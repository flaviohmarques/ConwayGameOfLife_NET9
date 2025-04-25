using ConwayGameOfLife_NET9.Models;
using ConwayGameOfLife_NET9.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Repositories;

public class BoardRepositoryTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly IBoardRepository _repository;

    public BoardRepositoryTests()
    {
        // Create temporary directory for test data
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"BoardData_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDirectory);

        // Create mock logger
        var mockLogger = new Mock<ILogger<BoardRepository>>();

        // Create field to access private _dataDirectory field
        var repositoryType = typeof(BoardRepository);
        var field = repositoryType.GetField("_dataDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Create repository instance
        _repository = new BoardRepository(mockLogger.Object);

        // Set _dataDirectory to the test directory
        field.SetValue(_repository, _testDataDirectory);
    }

    [Fact]
    public async Task SaveBoardAsync_NewBoard_SavesToDisk()
    {
        // Arrange
        var board = new Board(3, 3);
        board.Cells[0, 0].State = CellState.Alive;
        board.Cells[1, 1].State = CellState.Alive;
        board.Cells[2, 2].State = CellState.Alive;

        string filePath = Path.Combine(_testDataDirectory, $"{board.Id}.board");

        // Act
        await _repository.SaveBoardAsync(board);

        // Assert
        Assert.True(File.Exists(filePath));
        string content = await File.ReadAllTextAsync(filePath);
        Assert.Contains($"{board.Width},{board.Height},{board.GenerationCount}", content);
    }

    
    [Fact]
    public async Task GetBoardAsync_ExistingBoard_ReturnsBoard()
    {
        // Arrange
        var board = new Board(3, 3);
        string boardId = board.Id;
        board.Cells[0, 0].State = CellState.Alive;
        board.Cells[1, 1].State = CellState.Alive;
        board.Cells[2, 2].State = CellState.Alive;

        await _repository.SaveBoardAsync(board);

        // Act
        var retrievedBoard = await _repository.GetBoardAsync(boardId);

        // Assert
        Assert.NotNull(retrievedBoard);
        Assert.Equal(boardId, retrievedBoard.Id);
        Assert.Equal(board.Width, retrievedBoard.Width);
        Assert.Equal(board.Height, retrievedBoard.Height);
        Assert.Equal(CellState.Alive, retrievedBoard.Cells[0, 0].State);
        Assert.Equal(CellState.Alive, retrievedBoard.Cells[1, 1].State);
        Assert.Equal(CellState.Alive, retrievedBoard.Cells[2, 2].State);
    }

    [Fact]
    public async Task GetBoardAsync_NonExistentBoard_ReturnsNull()
    {
        // Arrange
        string nonExistentId = "non-existent-id";

        // Act
        var retrievedBoard = await _repository.GetBoardAsync(nonExistentId);

        // Assert
        Assert.Null(retrievedBoard);
    }

    [Fact]
    public async Task DeleteBoardAsync_ExistingBoard_RemovesFromDisk()
    {
        // Arrange
        var board = new Board(3, 3);
        string boardId = board.Id;
        await _repository.SaveBoardAsync(board);

        string filePath = Path.Combine(_testDataDirectory, $"{board.Id}.board");
        Assert.True(File.Exists(filePath));

        // Act
        bool result = await _repository.DeleteBoardAsync(boardId);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));

        var retrievedBoard = await _repository.GetBoardAsync(boardId);
        Assert.Null(retrievedBoard);
    }

    [Fact]
    public async Task DeleteBoardAsync_NonExistentBoard_ReturnsFalse()
    {
        // Arrange
        string nonExistentId = "non-existent-id";

        // Act
        bool result = await _repository.DeleteBoardAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        // Clean up the test directory
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }
}