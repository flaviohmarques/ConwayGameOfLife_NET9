using ConwayGameOfLife_NET9.Models;

namespace ConwayGameOfLife_NET9.Services
{
    public interface IGameOfLifeRules
    {
        Task<Board> ComputeNextGenerationAsync(Board currentBoard);
        Task<Board> ComputeMultipleGenerationsAsync(Board board, int generations);
        Task<(Board Board, bool IsCycleDetected, int? CycleStartGeneration)> ComputeFinalStateAsync(Board board, int maxGenerations);
    }
    public class GameOfLifeRules : IGameOfLifeRules
    {
        /// <summary>
        /// Computes the next generation of a Game of Life board.
        /// </summary>
        /// <param name="currentBoard">The current state of the board.</param>
        /// <returns>A new board representing the next generation.</returns>
        /// <remarks>
        /// This method applies Conway's Game of Life rules to each cell:
        /// 1. Live cell with fewer than 2 live neighbors dies (underpopulation)
        /// 2. Live cell with 2 or 3 live neighbors survives
        /// 3. Live cell with more than 3 live neighbors dies (overpopulation)
        /// 4. Dead cell with exactly 3 live neighbors becomes alive (reproduction)
        /// 
        /// The method creates a clone of the input board, increments its generation count,
        /// and updates the state of each cell based on these rules.
        /// </remarks>
        public async Task<Board> ComputeNextGenerationAsync(Board currentBoard)
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

        public async Task<Board> ComputeMultipleGenerationsAsync(Board board, int generations)
        {
            for (int i = 0; i < generations; i++)
            {
                board = await ComputeNextGenerationAsync(board);
            }

            return board;
        }


        /// <summary>
        /// Determines whether a board is empty (all cells are dead).
        /// </summary>
        /// <param name="board">The board to check.</param>
        /// <returns>True if all cells are dead; otherwise, false.</returns>
        /// <remarks>
        /// This method iterates through all cells in the board and returns false
        /// as soon as it encounters a live cell. Returns true only if all cells are dead.
        /// </remarks>
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

        /// <summary>
        /// Computes the final state of a board by simulating up to a maximum number of generations.
        /// Detects if a stable state or a repeating cycle is reached.
        /// </summary>
        /// <param name="board">The initial state of the board to simulate.</param>
        /// <param name="maxGenerations">The maximum number of generations to simulate.</param>
        /// <returns>
        /// A tuple containing:
        /// - The resulting board state,
        /// - A boolean indicating if a cycle was detected,
        /// - The generation at which the cycle started, or null if no cycle was detected.
        /// </returns>
        /// <exception cref="TimeoutException">
        /// Thrown if the board does not reach a stable or cyclic state within the maximum number of generations.
        /// </exception>
        public async Task<(Board Board, bool IsCycleDetected, int? CycleStartGeneration)> ComputeFinalStateAsync(Board board, int maxGenerations)
        {
            // Clone the input board to ensure the original state remains unchanged
            var currentBoard = await board.CloneAsync();

            // A dictionary to track previously seen board states and their corresponding generation numbers
            var previousStates = new Dictionary<string, int>();

            for (int i = 0; i < maxGenerations; i++)
            {
                // Serialize the current board state for easy comparison
                string serializedState = SerializeBoardState(currentBoard);

                // Check if this state has already been seen (cycle detection)
                if (previousStates.TryGetValue(serializedState, out int previousGeneration))
                {
                    // A cycle has been detected: return the current state, cycle flag, and the cycle's starting generation
                    return (currentBoard, true, previousGeneration);
                }

                // Record the current board state and generation
                previousStates[serializedState] = currentBoard.GenerationCount;

                // Check if the board has reached an empty (all dead cells) and thus stable state
                if (IsEmpty(currentBoard))
                {
                    // Return the stable state with no cycle
                    return (currentBoard, false, null);
                }

                // Advance to the next generation of the board
                currentBoard = await ComputeNextGenerationAsync(currentBoard);
            }

            // If no cycle or stable state is found within the max generations, throw a timeout exception
            throw new TimeoutException($"Board did not reach conclusion after {maxGenerations} generations");
        }

        /// <summary>
        /// Counts the number of live neighboring cells for a given cell position.
        /// </summary>
        /// <param name="board">The board to analyze.</param>
        /// <param name="x">The x-coordinate of the cell.</param>
        /// <param name="y">The y-coordinate of the cell.</param>
        /// <returns>The count of live neighboring cells (0-8).</returns>
        /// <remarks>
        /// This method examines the 8 adjacent cells surrounding the specified position,
        /// respecting the board boundaries. Only cells within the board's dimensions are considered.
        /// </remarks>
        private static int CountLiveNeighbors(Board board, int x, int y)
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

        /// <summary>
        /// Creates a string representation of a board's state for cycle detection.
        /// </summary>
        /// <param name="board">The board to serialize.</param>
        /// <returns>A string representation of the board's cell states.</returns>
        /// <remarks>
        /// This method creates a compact string representation of the board by mapping
        /// each cell to either "1" (alive) or "0" (dead). This serialization excludes
        /// metadata like board ID and generation count, focusing only on the cell states
        /// to detect recurring patterns or cycles.
        /// </remarks>
        private string SerializeBoardState(Board board)
        {
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
}
