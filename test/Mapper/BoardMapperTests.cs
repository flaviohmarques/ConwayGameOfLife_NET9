using ConwayGameOfLife_NET9.Mapper;
using ConwayGameOfLife_NET9.Models;
using FluentAssertions;
using Moq;

namespace UnitTests.Mapper;

public class BoardMapperTests
{
    [Fact]
    public async Task MapToResponseAsync_ShouldMapAllProperties()
    {
        // Arrange
        var mockBoard = new Mock<IBoard>();

        var boardId = "test-board-123";
        var width = 10;
        var height = 8;
        var generationCount = 42;
        var createdAt = DateTime.UtcNow;
        var binaryArray = new int[][] {
            new int[] { 0, 1, 0 },
            new int[] { 0, 0, 1 },
            new int[] { 1, 1, 1 }
        };

        mockBoard.Setup(b => b.Id).Returns(boardId);
        mockBoard.Setup(b => b.Width).Returns(width);
        mockBoard.Setup(b => b.Height).Returns(height);
        mockBoard.Setup(b => b.GenerationCount).Returns(generationCount);
        mockBoard.Setup(b => b.CreatedAt).Returns(createdAt);
        mockBoard.Setup(b => b.ToBinaryArrayAsync()).ReturnsAsync(binaryArray);

        // Act
        var result = await BoardMapper.MapToResponseAsync(mockBoard.Object);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.Width.Should().Be(width);
        result.Height.Should().Be(height);
        result.Generation.Should().Be(generationCount);
        result.CreatedAt.Should().Be(createdAt);
        result.State.Should().BeEquivalentTo(binaryArray);
    }

    [Fact]
    public async Task MapBatchToResponseAsync_ShouldMapAllBoards()
    {
        // Arrange
        var mockBoard1 = CreateMockBoard("board-1", 5, 5, 1);
        var mockBoard2 = CreateMockBoard("board-2", 10, 10, 2);

        var boards = new[] { mockBoard1.Object, mockBoard2.Object };

        // Act
        var results = await BoardMapper.MapBatchToResponseAsync(boards);

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be("board-1");
        results[1].Id.Should().Be("board-2");
    }

    private Mock<IBoard> CreateMockBoard(string id, int width, int height, int generation)
    {
        var mockBoard = new Mock<IBoard>();
        mockBoard.Setup(b => b.Id).Returns(id);
        mockBoard.Setup(b => b.Width).Returns(width);
        mockBoard.Setup(b => b.Height).Returns(height);
        mockBoard.Setup(b => b.GenerationCount).Returns(generation);
        mockBoard.Setup(b => b.CreatedAt).Returns(DateTime.UtcNow);
        mockBoard.Setup(b => b.ToBinaryArrayAsync()).ReturnsAsync(new int[][] { new int[] { 0, 1 } });
        return mockBoard;
    }
}