namespace ConwayGameOfLife_NET9.Contracts;
public class BoardResponse
{
    public required string Id { get; set; }
    public required int[][] State { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Generation { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response returned when a new board is successfully created
/// </summary>
public class BoardCreatedResponse
{
    /// <summary>
    /// The unique identifier of the newly created board
    /// </summary>
    /// <example>3f7b2a1c-d8e6-4f5g-9h0i-1j2k3l4m5n6o</example>
    public string Id { get; set; }
}