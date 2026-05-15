using System.Collections.Generic;

namespace Fellhaven.Core;

/// <summary>
/// Represents an edge (transition) between two locations in the dungeon graph.
/// </summary>
public class Edge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Source location (where the edge starts).
    /// </summary>
    public Location? Source { get; set; }
    
    /// <summary>
    /// Destination location (where the edge leads).
    /// </summary>
    public Location? Destination { get; set; }
    
    /// <summary>
    /// Direction from source to take this edge.
    /// </summary>
    public Direction Direction { get; set; }
    
    /// <summary>
    /// Priority of this edge (lower value = higher priority).
    /// When multiple edges exist in the same direction, the one with
    /// lowest priority value that passes its condition is used.
    /// </summary>
    public int Priority { get; set; } = 100;
    
    /// <summary>
    /// Path to wall/door texture for this edge.
    /// </summary>
    public string WallTexture { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional door texture (if this edge represents a door).
    /// </summary>
    public string DoorTexture { get; set; } = string.Empty;
    
    /// <summary>
    /// Lua script that determines if this edge can be traversed.
    /// Empty or null means always traversable.
    /// Script should return a boolean value.
    /// </summary>
    public string ConditionScript { get; set; } = string.Empty;
    
    /// <summary>
    /// Actions to execute when traversing this edge.
    /// </summary>
    public List<EdgeAction> Actions { get; set; } = new();
    
    /// <summary>
    /// Custom properties for this edge.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Evaluates whether this edge can be traversed given the current game state.
    /// </summary>
    /// <param name="gameState">Current game state.</param>
    /// <returns>True if edge is traversable, false otherwise.</returns>
    public bool EvaluateCondition(GameState gameState)
    {
        // Empty condition means always passable
        if (string.IsNullOrWhiteSpace(ConditionScript))
            return true;
        
        // Evaluate Lua condition script
        return LuaScriptEngine.EvaluateCondition(ConditionScript, gameState);
    }
    
    /// <summary>
    /// Executes all actions associated with this edge whose conditions are met.
    /// </summary>
    /// <param name="gameState">Current game state.</param>
    public void ExecuteActions(GameState gameState)
    {
        foreach (var action in Actions)
        {
            if (action.EvaluateCondition(gameState))
            {
                action.Execute(gameState);
            }
        }
    }
}

/// <summary>
/// Represents an action that can be triggered when traversing an edge.
/// </summary>
public class EdgeAction
{
    /// <summary>
    /// Unique identifier for this action.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional condition script that determines if this action should execute.
    /// Empty or null means always execute.
    /// </summary>
    public string ConditionScript { get; set; } = string.Empty;
    
    /// <summary>
    /// Lua script to execute when this action triggers.
    /// </summary>
    public string ActionScript { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this action does (for debugging/documentation).
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Evaluates whether this action should execute.
    /// </summary>
    public bool EvaluateCondition(GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(ConditionScript))
            return true;
        
        return LuaScriptEngine.EvaluateCondition(ConditionScript, gameState);
    }
    
    /// <summary>
    /// Executes this action's script.
    /// </summary>
    public void Execute(GameState gameState)
    {
        if (!string.IsNullOrWhiteSpace(ActionScript))
        {
            LuaScriptEngine.ExecuteAction(ActionScript, gameState);
        }
    }
}
