using ConwayGameOfLife_NET9.Contracts;
using ConwayGameOfLife_NET9.Models;

namespace ConwayGameOfLife_NET9.Mapper
{
    public static class BoardMapper
    {

        /// <summary>
        /// Maps a Board domain entity to a BoardResponse DTO.
        /// </summary>
        /// <param name="board">The board domain entity to map.</param>
        /// <returns>A BoardResponse DTO containing the board's data.</returns>
        /// <remarks>
        /// This private helper method converts the internal Board representation to a DTO
        /// suitable for API responses, including converting the board state to a binary array.
        /// </remarks>
        public static async Task<BoardResponse> MapToResponseAsync(IBoard board)
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

        public static async Task<BoardResponse[]> MapBatchToResponseAsync(IEnumerable<IBoard> boards)
        {
            if (boards == null)
                throw new ArgumentNullException(nameof(boards));

            var tasks = boards.Select(MapToResponseAsync);
            return await Task.WhenAll(tasks);
        }
    }
}
