namespace Fellhaven.Core;

/// <summary>
/// Represents a direction of movement in the dungeon graph.
/// </summary>
public enum Direction
{
    /// <summary>North direction.</summary>
    North,
    
    /// <summary>Northeast direction.</summary>
    NorthEast,
    
    /// <summary>East direction.</summary>
    East,
    
    /// <summary>Southeast direction.</summary>
    SouthEast,
    
    /// <summary>South direction.</summary>
    South,
    
    /// <summary>Southwest direction.</summary>
    SouthWest,
    
    /// <summary>West direction.</summary>
    West,
    
    /// <summary>Northwest direction.</summary>
    NorthWest,
    
    /// <summary>Upward direction (stairs, ladder up).</summary>
    Up,
    
    /// <summary>Downward direction (stairs, ladder down).</summary>
    Down
}

/// <summary>
/// Extension methods for Direction enum.
/// </summary>
public static class DirectionExtensions
{
    /// <summary>
    /// Gets the opposite direction.
    /// </summary>
    public static Direction GetOpposite(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.NorthEast => Direction.SouthWest,
            Direction.East => Direction.West,
            Direction.SouthEast => Direction.NorthWest,
            Direction.South => Direction.North,
            Direction.SouthWest => Direction.NorthEast,
            Direction.West => Direction.East,
            Direction.NorthWest => Direction.SouthEast,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => direction
        };
    }
    
    /// <summary>
    /// Converts direction to a short string representation (N, E, S, W, etc.)
    /// </summary>
    public static string ToShortString(this Direction direction)
    {
        return direction switch
        {
            Direction.North => "N",
            Direction.NorthEast => "NE",
            Direction.East => "E",
            Direction.SouthEast => "SE",
            Direction.South => "S",
            Direction.SouthWest => "SW",
            Direction.West => "W",
            Direction.NorthWest => "NW",
            Direction.Up => "U",
            Direction.Down => "D",
            _ => "?"
        };
    }
}
