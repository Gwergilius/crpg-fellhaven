using System.Collections.Generic;

namespace Fellhaven.Core;

/// <summary>
/// Represents a location (node) in the dungeon graph.
/// A location is a discrete area that the player can be in.
/// </summary>
public class Location
{
    /// <summary>
    /// Unique identifier for this location.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Localization key for the location name.
    /// </summary>
    public string NameKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Localization key for the location description.
    /// </summary>
    public string DescriptionKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to background texture/image for this location.
    /// </summary>
    public string BackgroundTexture { get; set; } = string.Empty;
    
    /// <summary>
    /// List of outgoing edges from this location.
    /// </summary>
    public List<Edge> OutgoingEdges { get; set; } = new();
    
    /// <summary>
    /// Custom properties for this location (can store any data).
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Objects/entities in this location (NPCs, items, etc.).
    /// </summary>
    public List<LocationObject> Objects { get; set; } = new();
    
    /// <summary>
    /// Creates a new location with the specified ID.
    /// </summary>
    public Location(string id)
    {
        Id = id;
    }
    
    /// <summary>
    /// Gets the first available edge in the specified direction.
    /// Edges are checked in priority order, and the first one whose
    /// condition evaluates to true is returned.
    /// </summary>
    /// <param name="direction">Direction to search for edges.</param>
    /// <param name="gameState">Current game state for condition evaluation.</param>
    /// <returns>The first available edge, or null if none available.</returns>
    public Edge? GetAvailableEdge(Direction direction, GameState gameState)
    {
        // Get all edges in the specified direction, ordered by priority (lower = higher priority)
        var edgesInDirection = OutgoingEdges
            .Where(e => e.Direction == direction)
            .OrderBy(e => e.Priority);
        
        // Return first edge whose condition is satisfied
        foreach (var edge in edgesInDirection)
        {
            if (edge.EvaluateCondition(gameState))
            {
                return edge;
            }
        }
        
        return null; // No available edge in this direction
    }
    
    /// <summary>
    /// Gets all edges from this location (regardless of availability).
    /// </summary>
    public IEnumerable<Edge> GetAllEdges()
    {
        return OutgoingEdges;
    }
    
    /// <summary>
    /// Gets all edges in a specific direction (regardless of availability).
    /// </summary>
    public IEnumerable<Edge> GetEdgesInDirection(Direction direction)
    {
        return OutgoingEdges.Where(e => e.Direction == direction);
    }
}

/// <summary>
/// Represents an object/entity within a location.
/// </summary>
public class LocationObject
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "npc", "item", "decoration", etc.
    public string NameKey { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}
