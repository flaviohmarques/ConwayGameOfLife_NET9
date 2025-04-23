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