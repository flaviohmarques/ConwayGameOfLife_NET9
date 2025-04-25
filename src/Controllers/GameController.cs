using ConwayGameOfLife_NET9.Contracts;
using ConwayGameOfLife_NET9.Mapper;
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
    [EndpointDescription("Creates a new Game of Life board from the provided initial state.")]
    [EndpointSummary("Creates board")]
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
            return CreatedAtAction(nameof(GetBoard), new { id = boardId }, new BoardCreatedResponse(boardId));

        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid board creation request");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a board by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the board to retrieve.</param>
    /// <returns>
    /// 200 OK with the board data if found;
    /// 404 Not Found if no board exists with the specified ID.
    /// </returns>
    /// <remarks>
    /// This endpoint retrieves a single Game of Life board with its current state and metadata.
    /// </remarks>
    [EndpointDescription("Get a board from the provided id.")]
    [EndpointSummary("Get board")]
    [HttpGet("boards/{id}")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoard(string id)
    {
        try
        {
            logger.LogInformation("Getting board {Id}", id);

            var board = await gameService.GetBoardAsync(id);
            return Ok(await BoardMapper.MapToResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
    }

    /// <summary>
    /// Deletes a board by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the board to delete.</param>
    /// <returns>
    /// 200 OK with a confirmation message if deleted successfully;
    /// 404 Not Found if no board exists with the specified ID.
    /// </returns>
    /// <remarks>
    /// This endpoint permanently removes a board from the system.
    /// </remarks>
    [EndpointDescription("Delete a board from the provided id.")]
    [EndpointSummary("Delete board")]
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


    /// <summary>
    /// Advances a board to its next generation state according to Game of Life rules.
    /// </summary>
    /// <param name="id">The unique identifier of the board to advance.</param>
    /// <returns>
    /// 200 OK with the next state of the board;
    /// 404 Not Found if no board exists with the specified ID.
    /// </returns>
    /// <remarks>
    /// This endpoint applies a single generation of Conway's Game of Life rules to the board
    /// and returns the updated state. The generation count is incremented by one.
    /// </remarks>
    [EndpointDescription("Request the next state of a board from the provided id.")]
    [EndpointSummary("Get board next state")]
    [HttpGet("boards/{id}/next")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextState(string id)
    {
        try
        {
            logger.LogInformation("Getting next state for board {Id}", id);

            var board = await gameService.GetNextStateAsync(id);
            return Ok(await BoardMapper.MapToResponseAsync(board));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Board with ID {id} not found" });
        }
    }


    /// <summary>
    /// Advances a board by a specified number of generations.
    /// </summary>
    /// <param name="id">The unique identifier of the board to advance.</param>
    /// <param name="generations">The number of generations to advance the board.</param>
    /// <returns>
    /// 200 OK with the state of the board after the specified number of generations;
    /// 400 Bad Request if the number of generations is negative or other validation errors occur;
    /// 404 Not Found if no board exists with the specified ID.
    /// </returns>
    /// <remarks>
    /// This endpoint efficiently computes multiple generations of Conway's Game of Life
    /// rules and returns the final state. The generation count is increased by the number
    /// of generations specified.
    /// </remarks>
    [EndpointDescription("Request the board state after a number of generations from the provided id.")]
    [EndpointSummary("Get board state")]
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
            return Ok(await BoardMapper.MapToResponseAsync(board));
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
    
    /// <summary>
    /// Attempts to find the final stable state of a board by advancing until stability is reached.
    /// </summary>
    /// <param name="id">The unique identifier of the board to advance.</param>
    /// <param name="maxGenerations">The maximum number of generations to compute before timing out (default: 1000).</param>
    /// <returns>
    /// 200 OK with the final stable state of the board;
    /// 404 Not Found if no board exists with the specified ID;
    /// 408 Request Timeout if a stable state is not reached within the maximum number of generations.
    /// </returns>
    /// <remarks>
    /// This endpoint attempts to find a stable state (no change, oscillator, or other pattern)
    /// by advancing the board until stability is detected or the maximum generation count is reached.
    /// </remarks>
    [EndpointDescription("Request the board final state after a large number of generations from the provided id.")]
    [EndpointSummary("Get board final state")]
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
            return Ok(await BoardMapper.MapToResponseAsync(board));
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
}