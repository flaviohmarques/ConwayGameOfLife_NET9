using ConwayGameOfLife_NET9.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace ConwayGameOfLife_NET9.Repositories;

public interface IBoardRepository
{
    Task<Board?> GetBoardAsync(string id);
    Task SaveBoardAsync(Board board);
    Task<bool> DeleteBoardAsync(string id);
}

public class BoardRepository : IBoardRepository
{
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<string, Board> _concurrentDictionary;
    private readonly ILogger<BoardRepository> _logger;

    public BoardRepository(ILogger<BoardRepository> logger)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoardData");
        _concurrentDictionary = new ConcurrentDictionary<string, Board>();

        // Ensure data directory exists and load from disk
        Directory.CreateDirectory(_dataDirectory);
        LoadExistingBoardsAsync().Wait();
    }

    /// <summary>
    /// Loads all existing board files from disk into memory at startup.
    /// </summary>
    /// <remarks>
    /// This method scans the configured data directory for files with the '.board' extension,
    /// deserializes each file into a Board object, and stores them in the in-memory concurrent dictionary.
    /// Any errors during the loading process are caught and logged, allowing the application to continue
    /// even if some boards fail to load.
    /// </remarks>
    private async Task LoadExistingBoardsAsync()
    {
        try
        {
            var files = Directory.GetFiles(_dataDirectory, "*.board");
            foreach (var file in files)
            {
                string id = Path.GetFileNameWithoutExtension(file);
                string serialized = await File.ReadAllTextAsync(file);
                Board board = await Board.DeserializeAsync(id, serialized);
                _concurrentDictionary[id] = board;
            }
            _logger.LogInformation("Loaded {ConcurrentDictionaryCount} boards from disk storage", _concurrentDictionary.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading boards from disk");
        }
    }

    /// <summary>
    /// Retrieves a board by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the board to retrieve.</param>
    /// <returns>
    /// The requested Board object if found; otherwise, null.
    /// </returns>
    /// <remarks>
    /// This method first checks the in-memory cache (_concurrentDictionary) for the board.
    /// If not found in memory, it attempts to load the board from disk, adds it to the 
    /// in-memory cache if successful, and then returns it.
    /// If the board doesn't exist or an error occurs during loading, null is returned.
    /// </remarks>
    public async Task<Board?> GetBoardAsync(string id)
    {
        if (_concurrentDictionary.TryGetValue(id, out Board? dictionaryBoard))
        {
            return dictionaryBoard;
        }
        string filePath = Path.Combine(_dataDirectory, $"{id}.board");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Board with ID {Id} not found", id);
            return null;
        }
        try
        {
            string serialized = await File.ReadAllTextAsync(filePath);
            Board board = await Board.DeserializeAsync(id, serialized);
            _concurrentDictionary[id] = board;
            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading board {Id} from disk", id);
            return null;
        }
    }

    /// <summary>
    /// Saves a board to both the in-memory cache and persistent storage.
    /// </summary>
    /// <param name="board">The board to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="Exception">Rethrows any exceptions that occur during the save operation.</exception>
    /// <remarks>
    /// This method updates the board in the in-memory cache (_concurrentDictionary) and 
    /// persists it to disk as a serialized file. The board's ID is used as the filename.
    /// Any exceptions during the save process are logged and rethrown to the caller.
    /// </remarks>
    public async Task SaveBoardAsync(Board board)
    {
        try
        {
            // Update cache  
            _concurrentDictionary[board.Id] = board;
            // Save to disk  
            string filePath = Path.Combine(_dataDirectory, $"{board.Id}.board");
            string serialized = await board.SerializeAsync();
            await File.WriteAllTextAsync(filePath, serialized);
            _logger.LogDebug("Saved board {BoardId} to disk", board.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving board {BoardId} to disk", board.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a board from both the in-memory cache and persistent storage.
    /// </summary>
    /// <param name="id">The unique identifier of the board to delete.</param>
    /// <returns>
    /// True if the board was successfully deleted or didn't exist; 
    /// False if an error occurred during deletion.
    /// </returns>
    /// <remarks>
    /// This method removes the board from the in-memory cache (_concurrentDictionary) and
    /// deletes the corresponding file from disk. If the file doesn't exist, the method
    /// still returns true as the end result is that the board doesn't exist.
    /// Any exceptions during the delete process are caught, logged, and false is returned.
    /// </remarks>
    public async Task<bool> DeleteBoardAsync(string id)
    {
        try
        {
            // Remove from cache  
            _concurrentDictionary.TryRemove(id, out _);
            // Remove from disk  
            string filePath = Path.Combine(_dataDirectory, $"{id}.board");
            if (!File.Exists(filePath)) return false;
            await Task.Run(() => File.Delete(filePath));
            _logger.LogInformation("Deleted board {Id} from disk", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting board {Id}", id);
            return false;
        }
    }
}