using ConwayGameOfLife_NET9.Models;
using ConwayGameOfLife_NET9.Repositories;

namespace ConwayGameOfLife_NET9.Services;

public interface IGameService
{
    Task<string> CreateBoardAsync(int[][] initialState);
    Task<Board> GetBoardAsync(string id);
    Task<string> DeleteBoardAsync(string id);
    Task<Board> GetNextStateAsync(string id);
    Task<Board> GetStateAfterGenerationsAsync(string id, int generations);
    Task<Board> GetFinalStateAsync(string id, int maxGenerations = 1000);
}

public class GameService(IBoardRepository boardRepository, ILogger<GameService> logger, IGameOfLifeRules gameOfLifeRules) : IGameService
{
    /// <summary>
    /// Creates a new Game of Life board from the provided initial state.
    /// </summary>
    /// <param name="initialState">A 2D array representing the initial state of the board, where 1 is alive and 0 is dead.</param>
    /// <returns>A string representing the unique identifier of the newly created board.</returns>
    /// <exception cref="Exception">Rethrows any exceptions that occur during board creation.</exception>
    /// <remarks>
    /// This method generates a new board with a unique ID, initializes it with the provided state,
    /// and persists it to the repository. The board's generation count starts at 0.
    /// </remarks>
    public async Task<string> CreateBoardAsync(int[][] initialState)
    {
        try
        {
            Board board = await Board.FromBinaryArrayAsync(initialState);
            await boardRepository.SaveBoardAsync(board);

            logger.LogInformation("Created new board with ID: {BoardId}", board.Id);
            return board.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating board");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a board by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the board to retrieve.</param>
    /// <returns>The requested Board object.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a board with the specified ID is not found.</exception>
    /// <remarks>
    /// This method attempts to retrieve a board from the repository and throws an exception
    /// if the board doesn't exist, rather than returning null.
    /// </remarks>
    public async Task<Board> GetBoardAsync(string id)
    {
        var board = await boardRepository.GetBoardAsync(id);

        if (board == null)
        {
            logger.LogWarning("Board with ID {Id} not found", id);
            throw new KeyNotFoundException($"Board with ID {id} not found");
        }

        return board;
    }

    /// <summary>
    /// Deletes a board by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the board to delete.</param>
    /// <returns>A confirmation message indicating successful deletion.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a board with the specified ID is not found.</exception>
    /// <remarks>
    /// This method attempts to delete a board from the repository and throws an exception
    /// if the board doesn't exist or the deletion operation fails.
    /// </remarks>
    public async Task<string> DeleteBoardAsync(string id)
    {
        if (await boardRepository.DeleteBoardAsync(id))
            return $"Successfully deleted board {id}";
        else
        {
            logger.LogWarning("Board with ID {Id} not found", id);
            throw new KeyNotFoundException($"Board with ID {id} not found");
        }
    }

    /// <summary>
    /// Advances a board by one generation according to Conway's Game of Life rules.
    /// </summary>
    /// <param name="id">The unique identifier of the board to advance.</param>
    /// <returns>The board in its next state after applying the rules.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a board with the specified ID is not found.</exception>
    /// <remarks>
    /// This method retrieves the current state of the board, computes the next generation,
    /// persists the new state to the repository, and returns the updated board.
    /// The generation count is incremented by one.
    /// </remarks>
    public async Task<Board> GetNextStateAsync(string id)
    {
        var board = await GetBoardAsync(id);
        var nextBoard = await gameOfLifeRules.ComputeNextGenerationAsync(board);

        // Save the new state
        await boardRepository.SaveBoardAsync(nextBoard);

        logger.LogInformation("Computed next state for board {Id}, generation {NextBoardGenerationCount}", id, nextBoard.GenerationCount);
        return nextBoard;
    }

    /// <summary>
    /// Advances a board by a specified number of generations.
    /// </summary>
    /// <param name="id">The unique identifier of the board to advance.</param>
    /// <param name="generations">The number of generations to advance the board.</param>
    /// <returns>The board after the specified number of generations have been applied.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of generations is negative.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a board with the specified ID is not found.</exception>
    /// <remarks>
    /// This method validates that the generation count is non-negative, retrieves the current state of the board,
    /// iteratively applies Conway's Game of Life rules for the specified number of generations,
    /// persists the final state to the repository, and returns the updated board.
    /// </remarks>
    public async Task<Board> GetStateAfterGenerationsAsync(string id, int generations)
    {
        if (generations < 0)
        {
            throw new ArgumentException("Number of generations must be non-negative");
        }

        var board = await GetBoardAsync(id);
        var resultBoard = await board.CloneAsync();

        resultBoard = await gameOfLifeRules.ComputeMultipleGenerationsAsync(resultBoard, generations);

        // Save the final state
        await boardRepository.SaveBoardAsync(resultBoard);

        logger.LogInformation($"Computed state after {generations} generations for board {id}");
        return resultBoard;
    }

    /// <summary>
    /// Attempts to find the final stable state or cycle of a board.
    /// </summary>
    /// <param name="id">The unique identifier of the board to analyze.</param>
    /// <param name="maxGenerations">The maximum number of generations to compute before timing out (default: 1000).</param>
    /// <returns>The board in its final stable state or at the start of a detected cycle.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a board with the specified ID is not found.</exception>
    /// <exception cref="TimeoutException">Thrown when a stable state or cycle is not found within the maximum number of generations.</exception>
    /// <remarks>
    /// This method tries to detect when a board:
    /// 1. Enters a repeating cycle of states (pattern detected using state hashing)
    /// 2. Reaches an empty state (all cells dead)
    /// 3. Exceeds the maximum generation count without finding stability
    /// 
    /// When a cycle is detected, the generation count is set to the first occurrence of that state.
    /// The final state is persisted to the repository before returning.
    /// </remarks>
    public async Task<Board> GetFinalStateAsync(string id, int maxGenerations = 1000)
    {
        var board = await GetBoardAsync(id);

        try
        {
            var (resultBoard, isCycleDetected, cycleStartGeneration) =
                await gameOfLifeRules.ComputeFinalStateAsync(board, maxGenerations);

            if (isCycleDetected && cycleStartGeneration.HasValue)
            {
                logger.LogInformation("Board {Id} stabilized into a cycle at generation {CurrentBoardGenerationCount}",
                    id, resultBoard.GenerationCount);
                resultBoard.GenerationCount = cycleStartGeneration.Value;
            }
            else
            {
                logger.LogInformation("Board {Id} reached empty state at generation {CurrentBoardGenerationCount}",
                    id, resultBoard.GenerationCount);
            }

            await boardRepository.SaveBoardAsync(resultBoard);
            return resultBoard;
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning("Board {Id} did not reach conclusion after {MaxGenerations} generations",
                id, maxGenerations);
            throw;
        }
    }

}