using System.Text;
namespace ConwayGameOfLife_NET9.Models;
public class Board
{
    public string Id { get; set; }
    public Cell[,] Cells { get; private set; }
    public int Width => Cells.GetLength(0);
    public int Height => Cells.GetLength(1);
    public DateTime CreatedAt { get; set; }
    public int GenerationCount { get; set; }

    public Board(int width, int height)
    {
        Id = Guid.NewGuid().ToString();
        Cells = new Cell[width, height];
        CreatedAt = DateTime.UtcNow;
        GenerationCount = 0;

        // Initialize with dead cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cells[x, y] = new Cell();
            }
        }
    }

    public Board(Cell[,] cells)
    {
        Id = Guid.NewGuid().ToString();
        Cells = cells;
        CreatedAt = DateTime.UtcNow;
        GenerationCount = 0;
    }

    // Create a deep copy of the board asynchronously
    public async Task<Board> CloneAsync()
    {
        return await Task.Run(() =>
        {
            Cell[,] newCells = new Cell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    newCells[x, y] = new Cell(Cells[x, y].State);
                }
            }

            Board newBoard = new Board(newCells)
            {
                Id = Id,
                CreatedAt = CreatedAt,
                GenerationCount = GenerationCount
            };

            return newBoard;
        });
    }

    public async Task<string> SerializeAsync()
    {
        return await Task.Run(() =>
        {
            StringBuilder sb = new StringBuilder();

            // First line: Width,Height,GenerationCount
            sb.AppendLine($"{Width},{Height},{GenerationCount}");

            // Remaining lines: Cell states (1 for alive, 0 for dead)
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append((int)Cells[x, y].State);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        });
    }


    // Create board from string representation asynchronously
    public static async Task<Board> DeserializeAsync(string id, string serialized)
    {
        return await Task.Run(() =>
        {
            string[] lines = serialized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse first line for dimensions and generation count
            string[] dimensions = lines[0].Split(',');
            int width = int.Parse(dimensions[0]);
            int height = int.Parse(dimensions[1]);
            int generationCount = int.Parse(dimensions[2]);

            // Create cells from remaining lines
            Cell[,] cells = new Cell[width, height];

            for (int y = 0; y < height; y++)
            {
                string line = lines[y + 1];

                for (int x = 0; x < width; x++)
                {
                    CellState state = line[x] == '1' ? CellState.Alive : CellState.Dead;
                    cells[x, y] = new Cell(state);
                }
            }

            Board board = new Board(cells)
            {
                Id = id,
                GenerationCount = generationCount
            };

            return board;
        });
    }

    // Convert to binary representation for API responses asynchronously
    public async Task<int[][]> ToBinaryArrayAsync()
    {
        return await Task.Run(() =>
        {
            int[][] result = new int[Height][];

            for (int y = 0; y < Height; y++)
            {
                result[y] = new int[Width];
                for (int x = 0; x < Width; x++)
                {
                    result[y][x] = (int)Cells[x, y].State;
                }
            }

            return result;
        });
    }

    // Create board from binary input asynchronously
    public static async Task<Board> FromBinaryArrayAsync(int[][] array)
    {
        return await Task.Run(() =>
        {
            if (array == null || array.Length == 0 || array[0].Length == 0)
            {
                throw new ArgumentException("Invalid board input");
            }

            int height = array.Length;
            int width = array[0].Length;

            Cell[,] cells = new Cell[width, height];

            for (int y = 0; y < height; y++)
            {
                if (array[y].Length != width)
                {
                    throw new ArgumentException("All rows must have the same length");
                }

                for (int x = 0; x < width; x++)
                {
                    if (array[y][x] != 0 && array[y][x] != 1)
                    {
                        throw new ArgumentException("Board must only contain 0s and 1s");
                    }

                    cells[x, y] = new Cell(array[y][x] == 1 ? CellState.Alive : CellState.Dead);
                }
            }

            return new Board(cells);
        });
    }
}