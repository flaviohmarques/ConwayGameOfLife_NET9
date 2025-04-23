using ConwayGameOfLife_NET9.Models;
using ConwayGameOfLife_NET9.Repositories;
using ConwayGameOfLife_NET9.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ConwayGameOfLife_NET9.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IBoardRepository> _mockRepository;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _mockRepository = new Mock<IBoardRepository>();
        _gameService = new GameService(_mockRepository.Object, new Mock<ILogger<GameService>>().Object);
    }

    [Fact]
    public async Task CreateBoardAsync_ValidInput_ReturnsBoardId()
    {
        // Arrange
        int[][] initialState = new int[][]
        {
            new int[] { 0, 1, 0 },
            new int[] { 0, 1, 0 },
            new int[] { 0, 1, 0 }
        };

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        string boardId = await _gameService.CreateBoardAsync(initialState);

        // Assert
        Assert.NotNull(boardId);
        Assert.NotEmpty(boardId);
        _mockRepository.Verify(r => r.SaveBoardAsync(It.IsAny<Board>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBoardAsync_WithExistingId_ReturnsSuccessMessage()
    {
        // Arrange
        string boardId = "existing-board-id";
        _mockRepository
            .Setup(repo => repo.DeleteBoardAsync(boardId))
            .ReturnsAsync(true);

        // Act
        string result = await _gameService.DeleteBoardAsync(boardId);

        // Assert
        result.Should().Be($"Successfully deleted board {boardId}");
        _mockRepository.Verify(repo => repo.DeleteBoardAsync(boardId), Times.Once);
    }

    [Fact]
    public async Task DeleteBoardAsync_WithNonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        string boardId = "non-existing-board-id";
        _mockRepository
            .Setup(repo => repo.DeleteBoardAsync(boardId))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _gameService.DeleteBoardAsync(boardId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Board with ID {boardId} not found");
        _mockRepository.Verify(repo => repo.DeleteBoardAsync(boardId), Times.Once);
    }

   

    [Fact]
    public async Task DeleteBoardAsync_WithNullId_PassesNullToRepository()
    {
        // Arrange
        string boardId = null;
        _mockRepository
            .Setup(repo => repo.DeleteBoardAsync(boardId))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _gameService.DeleteBoardAsync(boardId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _mockRepository.Verify(repo => repo.DeleteBoardAsync(boardId), Times.Once);
    }

    [Fact]
    public async Task DeleteBoardAsync_WithEmptyId_PassesEmptyStringToRepository()
    {
        // Arrange
        string boardId = string.Empty;
        _mockRepository
            .Setup(repo => repo.DeleteBoardAsync(boardId))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _gameService.DeleteBoardAsync(boardId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _mockRepository.Verify(repo => repo.DeleteBoardAsync(boardId), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetNextStateAsync_ValidId_ReturnsNextGeneration(string boardId)
    {

        // Create a blinker pattern (vertical)
        var initialBoard = new Board(5, 5)
        {
            Id = boardId
        };
        initialBoard.Cells[1, 0].State = CellState.Alive;
        initialBoard.Cells[1, 1].State = CellState.Alive;
        initialBoard.Cells[1, 2].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        var nextBoard = await _gameService.GetNextStateAsync(boardId);

        // Asserts boards
        Assert.Equal(boardId, nextBoard.Id);
        Assert.Equal(1, nextBoard.GenerationCount);

        // Expecting
        Assert.True(nextBoard.Cells[0, 1].IsAlive);
        Assert.True(nextBoard.Cells[1, 1].IsAlive);
        Assert.True(nextBoard.Cells[2, 1].IsAlive);
        Assert.False(nextBoard.Cells[1, 0].IsAlive);
        Assert.False(nextBoard.Cells[1, 2].IsAlive);
    }


    [Theory, AutoMoqData]
    public async Task GetNextStateAsync_SmallBoard_ReturnsNextGeneration(string boardId)
    {
        // Create a blinker pattern (vertical)
        var initialBoard = new Board(3, 3)
        {
            Id = boardId
        };
        initialBoard.Cells[1, 0].State = CellState.Alive;
        initialBoard.Cells[1, 1].State = CellState.Alive;
        initialBoard.Cells[1, 2].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        var nextBoard = await _gameService.GetNextStateAsync(boardId);

        // Assert
        Assert.Equal(boardId, nextBoard.Id);
        Assert.Equal(1, nextBoard.GenerationCount);

        // Count the number of live cells
        int liveCount = 0;
        for (int x = 0; x < nextBoard.Width; x++)
        {
            for (int y = 0; y < nextBoard.Height; y++)
            {
                if (nextBoard.Cells[x, y].IsAlive)
                    liveCount++;
            }
        }

        // The number of live cells should remain 3
        Assert.Equal(3, liveCount);
    }


    [Theory, AutoMoqData]
    public async Task GetFinalStateAsync_StablePattern_ReturnsStableState(string boardId)
    {
        // Create a block pattern (stable)
        var initialBoard = new Board(4, 4);
        initialBoard.Id = boardId;
        initialBoard.Cells[1, 1].State = CellState.Alive;
        initialBoard.Cells[1, 2].State = CellState.Alive;
        initialBoard.Cells[2, 1].State = CellState.Alive;
        initialBoard.Cells[2, 2].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        var finalBoard = await _gameService.GetFinalStateAsync(boardId);

        // Assert
        Assert.Equal(boardId, finalBoard.Id);
        Assert.Equal(0, finalBoard.GenerationCount); // Should detect the stable pattern immediately

        // The block pattern should remain unchanged
        Assert.Equal(CellState.Alive, finalBoard.Cells[1, 1].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[1, 2].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[2, 1].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[1, 1].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[1, 2].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[2, 1].State);
        Assert.Equal(CellState.Alive, finalBoard.Cells[2, 2].State);

        _mockRepository.Verify(r => r.SaveBoardAsync(It.IsAny<Board>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetStateAfterGenerationsAsync_ValidInput_ReturnsCorrectState(string boardId)
    {
        // Arrange
        int generations = 2;

        // Create a blinker pattern (vertical)
        var initialBoard = new Board(5, 5);
        initialBoard.Id = boardId;
        initialBoard.Cells[2, 1].State = CellState.Alive;
        initialBoard.Cells[2, 2].State = CellState.Alive;
        initialBoard.Cells[2, 3].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultBoard = await _gameService.GetStateAfterGenerationsAsync(boardId, generations);

        // Assert
        Assert.Equal(boardId, resultBoard.Id);
        Assert.Equal(generations, resultBoard.GenerationCount);

        // After 2 generations, a blinker should return to vertical orientation
        Assert.Equal(CellState.Dead, resultBoard.Cells[1, 2].State);
        Assert.Equal(CellState.Alive, resultBoard.Cells[2, 1].State);
        Assert.Equal(CellState.Alive, resultBoard.Cells[2, 2].State);
        Assert.Equal(CellState.Alive, resultBoard.Cells[2, 3].State);
        Assert.Equal(CellState.Dead, resultBoard.Cells[3, 2].State);

        _mockRepository.Verify(r => r.SaveBoardAsync(It.IsAny<Board>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetFinalStateAsync_CyclicPattern_DetectsCycle(string boardId)
    {

        // Create a blinker pattern (vertical)
        var initialBoard = new Board(5, 5);
        initialBoard.Id = boardId;
        initialBoard.Cells[2, 1].State = CellState.Alive;
        initialBoard.Cells[2, 2].State = CellState.Alive;
        initialBoard.Cells[2, 3].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        _mockRepository.Setup(r => r.SaveBoardAsync(It.IsAny<Board>()))
            .Returns(Task.CompletedTask);

        // Act
        var finalBoard = await _gameService.GetFinalStateAsync(boardId);

        // Assert
        Assert.Equal(boardId, finalBoard.Id);
        Assert.Equal(0, finalBoard.GenerationCount); // Should detect the 2-cycle and return to gen 0

        _mockRepository.Verify(r => r.SaveBoardAsync(It.IsAny<Board>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetFinalStateAsync_MaxGenerationsExceeded_ThrowsTimeoutException(string boardId)
    {
        // Arrange
        int maxGenerations = 5;

        // Create a glider pattern (never stabilizes in a small grid)
        var initialBoard = new Board(20, 20);
        initialBoard.Id = boardId;
        initialBoard.Cells[1, 0].State = CellState.Alive;
        initialBoard.Cells[2, 1].State = CellState.Alive;
        initialBoard.Cells[0, 2].State = CellState.Alive;
        initialBoard.Cells[1, 2].State = CellState.Alive;
        initialBoard.Cells[2, 2].State = CellState.Alive;

        _mockRepository.Setup(r => r.GetBoardAsync(boardId))
            .ReturnsAsync(initialBoard);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
            await _gameService.GetFinalStateAsync(boardId, maxGenerations));
    }
}