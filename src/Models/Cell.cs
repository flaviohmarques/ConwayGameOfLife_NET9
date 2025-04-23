namespace ConwayGameOfLife_NET9.Models;

public enum CellState
{
    Dead = 0,
    Alive = 1
}

public class Cell
{
    public CellState State { get; set; }

    public Cell(CellState state = CellState.Dead)
    {
        State = state;
    }

    public bool IsAlive => State == CellState.Alive;
}