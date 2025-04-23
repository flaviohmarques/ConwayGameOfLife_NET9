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

public class GameService : IGameService
{
    private readonly IBoardRepository _boardRepository;
    private readonly ILogger<GameService> _logger;

    public GameService(IBoardRepository boardRepository, ILogger<GameService> logger)
    {
        _boardRepository = boardRepository;
        _logger = logger;
    }

    public async Task<string> CreateBoardAsync(int[][] initialState)
    {
        try
        {
            Board board = await Board.FromBinaryArrayAsync(initialState);
            await _boardRepository.SaveBoardAsync(board);

            _logger.LogInformation("Created new board with ID: {BoardId}", board.Id);
            return board.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating board");
            throw;
        }
    }

    public async Task<Board> GetBoardAsync(string id)
    {
        var board = await _boardRepository.GetBoardAsync(id);

        if (board == null)
        {
            _logger.LogWarning("Board with ID {Id} not found", id);
            throw new KeyNotFoundException($"Board with ID {id} not found");
        }

        return board;
    }

    public async Task<string> DeleteBoardAsync(string id)
    {
        if (await _boardRepository.DeleteBoardAsync(id))
            return $"Successfully deleted board {id}";
        else
        {
            _logger.LogWarning("Board with ID {Id} not found", id);
            throw new KeyNotFoundException($"Board with ID {id} not found");
        }
    }

    public async Task<Board> GetNextStateAsync(string id)
    {
        var board = await GetBoardAsync(id);
        var nextBoard = await ComputeNextGenerationAsync(board);

        // Save the new state
        await _boardRepository.SaveBoardAsync(nextBoard);

        _logger.LogInformation("Computed next state for board {Id}, generation {NextBoardGenerationCount}", id, nextBoard.GenerationCount);
        return nextBoard;
    }

    public async Task<Board> GetStateAfterGenerationsAsync(string id, int generations)
    {
        if (generations < 0)
        {
            throw new ArgumentException("Number of generations must be non-negative");
        }

        var board = await GetBoardAsync(id);
        var resultBoard = await board.CloneAsync();

        for (int i = 0; i < generations; i++)
        {
            resultBoard = await ComputeNextGenerationAsync(resultBoard);
        }

        // Save the final state
        await _boardRepository.SaveBoardAsync(resultBoard);

        _logger.LogInformation($"Computed state after {generations} generations for board {id}");
        return resultBoard;
    }

    public async Task<Board> GetFinalStateAsync(string id, int maxGenerations = 1000)
    {
        var board = await GetBoardAsync(id);
        var currentBoard = await board.CloneAsync();

        // Dictionary to store previous states for cycle detection
        var previousStates = new Dictionary<string, int>();

        for (int i = 0; i < maxGenerations; i++)
        {
            // Store the current state for cycle detection
            string serializedState = SerializeBoardState(currentBoard);

            if (previousStates.TryGetValue(serializedState, out int previousGeneration))
            {
                // We've found a cycle
                _logger.LogInformation("Board {Id} stabilized into a cycle at generation {CurrentBoardGenerationCount}", id, currentBoard.GenerationCount);
                currentBoard.GenerationCount = previousGeneration;
                await _boardRepository.SaveBoardAsync(currentBoard);
                return currentBoard;
            }

            previousStates[serializedState] = currentBoard.GenerationCount;

            // If board is empty (all cells dead), we've reached a stable state
            if (IsEmpty(currentBoard))
            {
                _logger.LogInformation("Board {Id} reached empty state at generation {CurrentBoardGenerationCount}", id, currentBoard.GenerationCount);
                await _boardRepository.SaveBoardAsync(currentBoard);
                return currentBoard;
            }

            // Compute next generation
            currentBoard = await ComputeNextGenerationAsync(currentBoard);
        }

        _logger.LogWarning($"Board {id} did not reach conclusion after {maxGenerations} generations");
        throw new TimeoutException($"Board did not reach conclusion after {maxGenerations} generations");
    }

    private async Task<Board> ComputeNextGenerationAsync(Board currentBoard)
    {
        Board nextBoard = await currentBoard.CloneAsync();
        nextBoard.GenerationCount++;

        for (int x = 0; x < currentBoard.Width; x++)
        {
            for (int y = 0; y < currentBoard.Height; y++)
            {
                int liveNeighbors = CountLiveNeighbors(currentBoard, x, y);
                Cell currentCell = currentBoard.Cells[x, y];

                // Apply Conway's Game of Life rules
                if (currentCell.IsAlive)
                {
                    // Live cell with fewer than 2 live neighbors dies (underpopulation)
                    // Live cell with more than 3 live neighbors dies (overpopulation)
                    if (liveNeighbors < 2 || liveNeighbors > 3)
                    {
                        nextBoard.Cells[x, y].State = CellState.Dead;
                    }
                    // Otherwise, the live cell stays alive
                }
                else
                {
                    // Dead cell with exactly 3 live neighbors becomes alive (reproduction)
                    if (liveNeighbors == 3)
                    {
                        nextBoard.Cells[x, y].State = CellState.Alive;
                    }
                    // Otherwise, the dead cell stays dead
                }
            }
        }

        return nextBoard;
    }

    private int CountLiveNeighbors(Board board, int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Skip the cell itself
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                // Check if neighbor is within bounds
                if (nx >= 0 && nx < board.Width && ny >= 0 && ny < board.Height)
                {
                    if (board.Cells[nx, ny].IsAlive)
                        count++;
                }
            }
        }

        return count;
    }

    private bool IsEmpty(Board board)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (board.Cells[x, y].IsAlive)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private string SerializeBoardState(Board board)
    {
        // Simple state serialization for cycle detection
        // Only serializes the cell states, not the board ID or generation count
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                sb.Append(board.Cells[x, y].IsAlive ? "1" : "0");
            }
        }

        return sb.ToString();
    }
}