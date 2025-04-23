using ConwayGameOfLife_NET9.Contracts;
using ConwayGameOfLife_NET9.Models;
using ConwayGameOfLife_NET9.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConwayGameOfLife_NET9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController(IGameService gameService, ILogger<GameController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new Game of Life board from the provided initial state.
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Game/boards
    ///     {
    ///        "initialState": [
    ///           [0, 0, 0, 0, 0],
    ///           [0, 0, 1, 0, 0],
    ///           [0, 0, 0, 1, 0],
    ///           [0, 1, 1, 1, 0],
    ///           [0, 0, 0, 0, 0]
    ///        ]
    ///     }
    ///
    /// Sample response:
    ///
    ///     HTTP/1.1 201 Created
    ///     {
    ///        "id": "3f7b2a1c-d8e6-4f5g-9h0i-1j2k3l4m5n6o"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">The new board configuration</param>
    /// <returns>The new id of the uploaded board</returns>
    [HttpPost("boards")]
    [ProducesResponseType(typeof(BoardCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBoard([FromBody] CreateBoardRequest request)
    {
        try
        {
            logger.LogInformation("Creating new board");

            var validator = new CreateBoardRequestValidator();
            var result = await validator.ValidateAsync(request);
            if (!result.IsValid) return BadRequest(result.Errors.Select(s => s.ErrorMessage));
                
            var boardId = await gameService.CreateBoardAsync(request.InitialState);
            return CreatedAtAction(nameof(GetBoard), new { id = boardId }, new BoardCreatedResponse { Id = boardId });

        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid board creation request");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("boards/{id}")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoard(string id)
    {
        try
        {
            logger.LogInformation("Getting board {Id}", id);

            var board = await gameService.GetBoardAsync(id);
            return Ok(await MapBoardResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
    }

    [HttpDelete("boards/{id}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBoard(string id)
    {
        try
        {
            logger.LogInformation("Deleting board {Id}", id);

            var message = await gameService.DeleteBoardAsync(id);
            return Ok(message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
    }

    [HttpGet("boards/{id}/next")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextState(string id)
    {
        try
        {
            logger.LogInformation("Getting next state for board {Id}", id);

            var board = await gameService.GetNextStateAsync(id);
            return Ok(await MapBoardResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
    }

    [HttpGet("boards/{id}/after/{generations}")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStateAfterGenerations(string id, int generations)
    {
        try
        {
            logger.LogInformation("Getting state after {Generations} generations for board {Id}", generations, id);

            if (generations < 0)
            {
                return BadRequest(new { message = "Number of generations must be non-negative" });
            }

            var board = await gameService.GetStateAfterGenerationsAsync(id, generations);
            return Ok(await MapBoardResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("boards/{id}/final")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    public async Task<IActionResult> GetFinalState(string id, [FromQuery] int maxGenerations = 1000)
    {
        try
        {
            logger.LogInformation("Getting final state for board {Id} (max {MaxGenerations} generations)", id, maxGenerations);

            var board = await gameService.GetFinalStateAsync(id, maxGenerations);
            return Ok(await MapBoardResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, new { message = ex.Message });
        }
    }

    private static async Task<BoardResponse> MapBoardResponseAsync(Board board)
    {
        return new BoardResponse
        {
            Id = board.Id,
            State = await board.ToBinaryArrayAsync(),
            Width = board.Width,
            Height = board.Height,
            Generation = board.GenerationCount,
            CreatedAt = board.CreatedAt
        };
    }
}