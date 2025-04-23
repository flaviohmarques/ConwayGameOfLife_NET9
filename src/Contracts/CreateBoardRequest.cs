
using FluentValidation;
namespace ConwayGameOfLife_NET9.Contracts;

/// <summary>
/// Sample request model for the CreateBoard endpoint
/// </summary>
public class CreateBoardRequest
{
    /// <summary>
    /// 2D array representing the initial state of the Game of Life board.
    /// Each cell must be 0 (dead) or 1 (alive).
    /// </summary>
    /// <example>
    /// [
    ///   [0, 0, 0, 0, 0],
    ///   [0, 0, 1, 0, 0],
    ///   [0, 0, 0, 1, 0],
    ///   [0, 1, 1, 1, 0],
    ///   [0, 0, 0, 0, 0]
    /// ]
    /// </example>
    public required int[][] InitialState { get; set; }
}

/// <summary>
/// Validator for the CreateBoardRequest
/// </summary>
public class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardRequestValidator()
    {
        RuleFor(x => x.InitialState)
            .NotNull()
            .WithMessage("InitialState is required.")
            .Must(state => state.Length > 0 && state.All(row => row is { Length: > 0 }))
            .WithMessage("InitialState must not be empty and rows must contain at least one element.")
            .Must(state =>
            {
                var firstRowLength = state[0].Length;
                return state.All(row => row.Length == firstRowLength);
            })
            .WithMessage("All rows in InitialState must have the same number of columns.");

        RuleForEach(x => x.InitialState).ChildRules(row =>
        {
            row.RuleForEach(cell => cell)
                .Must(cell => cell is 0 or 1)
                .WithMessage("Each cell must be either 0 (dead) or 1 (alive).");
        });
    }
}