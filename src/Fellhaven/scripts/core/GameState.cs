namespace Fellhaven.Core;

/// <summary>
/// Represents the current state of the game.
/// This includes player progress, flags, variables, inventory, and current location.
/// </summary>
public partial class GameState : Node
{
    private static GameState? _instance;

    /// <summary>
    /// Singleton instance of GameState.
    /// </summary>
    public static GameState Instance => _instance!;

    /// <summary>
    /// Global boolean flags (e.g., "door_opened", "quest_completed").
    /// </summary>
    public Dictionary<string, bool> GlobalFlags { get; set; } = new();

    /// <summary>
    /// Global integer variables (e.g., quest stages, counters).
    /// </summary>
    public Dictionary<string, int> Variables { get; set; } = new();

    /// <summary>
    /// String variables (for more complex data).
    /// </summary>
    public Dictionary<string, string> StringVariables { get; set; } = new();

    /// <summary>
    /// Player's current location in the dungeon.
    /// </summary>
    public Location? CurrentLocation { get; set; }

    /// <summary>
    /// Player's inventory.
    /// </summary>
    public Inventory Inventory { get; set; } = new();

    /// <summary>
    /// Player's party (characters).
    /// </summary>
    public Party PlayerParty { get; set; } = new();

    public override void _Ready()
    {
        _instance = this;
    }

    /// <summary>
    /// Checks if a flag is set to true.
    /// </summary>
    public bool HasFlag(string flagName)
    {
        return GlobalFlags.TryGetValue(flagName, out var value) && value;
    }

    /// <summary>
    /// Sets a flag to the specified value.
    /// </summary>
    public void SetFlag(string flagName, bool value)
    {
        GlobalFlags[flagName] = value;
        GD.Print($"[GameState] Flag '{flagName}' set to {value}");
    }

    /// <summary>
    /// Gets an integer variable's value (returns 0 if not set).
    /// </summary>
    public int GetVariable(string varName)
    {
        return Variables.GetValueOrDefault(varName, 0);
    }

    /// <summary>
    /// Sets an integer variable to the specified value.
    /// </summary>
    public void SetVariable(string varName, int value)
    {
        Variables[varName] = value;
        GD.Print($"[GameState] Variable '{varName}' set to {value}");
    }

    /// <summary>
    /// Adds to an integer variable (creates it if it doesn't exist).
    /// </summary>
    public void AddVariable(string varName, int amount)
    {
        Variables[varName] = GetVariable(varName) + amount;
        GD.Print($"[GameState] Variable '{varName}' incremented by {amount} to {Variables[varName]}");
    }

    /// <summary>
    /// Gets a string variable's value (returns empty string if not set).
    /// </summary>
    public string GetStringVariable(string varName)
    {
        return StringVariables.GetValueOrDefault(varName, string.Empty);
    }

    /// <summary>
    /// Sets a string variable to the specified value.
    /// </summary>
    public void SetStringVariable(string varName, string value)
    {
        StringVariables[varName] = value;
        GD.Print($"[GameState] String variable '{varName}' set to '{value}'");
    }

    /// <summary>
    /// Moves the player to a new location via an edge.
    /// </summary>
    public void TransitionTo(Edge edge)
    {
        if (edge.Destination == null)
        {
            GD.PrintErr("[GameState] Cannot transition: edge has no destination");
            return;
        }

        GD.Print($"[GameState] Transitioning from '{CurrentLocation?.Id}' to '{edge.Destination.Id}' via {edge.Direction}");

        // Execute edge actions
        edge.ExecuteActions(this);

        // Move to new location
        CurrentLocation = edge.Destination;

        // Emit signal for UI update
        EmitSignal(SignalName.LocationChanged, edge.Destination.Id);
    }

    /// <summary>
    /// Attempts to move in the specified direction.
    /// </summary>
    /// <returns>True if movement succeeded, false otherwise.</returns>
    public bool TryMove(Direction direction)
    {
        if (CurrentLocation == null)
        {
            GD.PrintErr("[GameState] Cannot move: no current location");
            return false;
        }

        var edge = CurrentLocation.GetAvailableEdge(direction, this);

        if (edge == null)
        {
            GD.Print($"[GameState] No available edge in direction {direction}");
            return false;
        }

        TransitionTo(edge);
        return true;
    }

    /// <summary>
    /// Resets the game state to initial values.
    /// </summary>
    public void Reset()
    {
        GlobalFlags.Clear();
        Variables.Clear();
        StringVariables.Clear();
        CurrentLocation = null;
        Inventory.Clear();
        PlayerParty = new Party();

        GD.Print("[GameState] Game state reset");
    }

    [Signal]
    public delegate void LocationChangedEventHandler(string locationId);

    [Signal]
    public delegate void FlagChangedEventHandler(string flagName, bool value);

    [Signal]
    public delegate void VariableChangedEventHandler(string varName, int value);
}

/// <summary>
/// Represents the player's inventory.
/// </summary>
public class Inventory
{
    private readonly Dictionary<string, int> _items = new();

    /// <summary>
    /// Checks if the inventory contains at least one of an item.
    /// </summary>
    public bool HasItem(string itemId)
    {
        return _items.GetValueOrDefault(itemId, 0) > 0;
    }

    /// <summary>
    /// Checks if the inventory contains at least the specified count of an item.
    /// </summary>
    public bool HasItems(string itemId, int count)
    {
        return _items.GetValueOrDefault(itemId, 0) >= count;
    }

    /// <summary>
    /// Gets the count of a specific item.
    /// </summary>
    public int GetItemCount(string itemId)
    {
        return _items.GetValueOrDefault(itemId, 0);
    }

    /// <summary>
    /// Adds items to the inventory.
    /// </summary>
    public void AddItem(string itemId, int count = 1)
    {
        _items[itemId] = GetItemCount(itemId) + count;
        GD.Print($"[Inventory] Added {count}x '{itemId}' (total: {_items[itemId]})");
    }

    /// <summary>
    /// Removes items from the inventory.
    /// </summary>
    public bool RemoveItem(string itemId, int count = 1)
    {
        var currentCount = GetItemCount(itemId);

        if (currentCount < count)
        {
            GD.PrintErr($"[Inventory] Cannot remove {count}x '{itemId}' (only have {currentCount})");
            return false;
        }

        _items[itemId] = currentCount - count;

        if (_items[itemId] <= 0)
            _items.Remove(itemId);

        GD.Print($"[Inventory] Removed {count}x '{itemId}' (remaining: {_items.GetValueOrDefault(itemId, 0)})");
        return true;
    }

    /// <summary>
    /// Gets all items in the inventory.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetAllItems()
    {
        return _items;
    }

    /// <summary>
    /// Clears all items from the inventory.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        GD.Print("[Inventory] Cleared all items");
    }
}

/// <summary>
/// Represents the player's party (characters).
/// </summary>
public class Party
{
    public List<Character> Members { get; set; } = new();

    /// <summary>
    /// Checks if any party member has the specified class.
    /// </summary>
    public bool HasClass(string className)
    {
        return Members.Any(m => m.ClassName.Equals(className, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if any party member has the specified skill at minimum level.
    /// </summary>
    public bool HasSkill(string skillName, int minLevel = 1)
    {
        return Members.Any(m => m.GetSkillLevel(skillName) >= minLevel);
    }

    /// <summary>
    /// Gets the average level of the party.
    /// </summary>
    public int GetAverageLevel()
    {
        if (Members.Count == 0)
            return 0;

        return (int)Members.Average(m => m.Level);
    }
}

/// <summary>
/// Represents a character in the party.
/// </summary>
public class Character
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int CurrentHP { get; set; } = 100;
    public int MaxHP { get; set; } = 100;
    public int CurrentMP { get; set; } = 50;
    public int MaxMP { get; set; } = 50;

    public Dictionary<string, int> Skills { get; set; } = new();
    public Dictionary<string, int> Attributes { get; set; } = new();

    public int GetSkillLevel(string skillName)
    {
        return Skills.GetValueOrDefault(skillName, 0);
    }
}
