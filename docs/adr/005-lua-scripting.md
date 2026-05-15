[lua-api]: ../api/lua_api.md "Lua API Reference"
[adr-003]: 003-graph-based-dungeon.md "Graph-Based Dungeon Model"
[adr-004]: 004-condition-and-combat.md "Condition Vocabulary and Combat System"
[adr-006]: 006-localization.md "Internationalization Strategy"

# ADR-005: Lua Scripting for Conditions and Actions

**Status**: Accepted

**Date**: 2026-05-14

## Status

Accepted

## Context

The dungeon graph system needs a way to define:
- **Conditions**: When is an edge traversable? (e.g., player has key, quest completed)
- **Actions**: What happens when traversing an edge? (e.g., consume key, trigger event)

Options for implementing this logic:
1. **Hardcoded C# classes**: Create C# classes for each condition/action type
2. **DSL (Domain Specific Language)**: Design custom language for rules
3. **Existing scripting language**: Lua, JavaScript, Python, etc.
4. **Visual scripting**: Node-based editor (like Unreal Blueprints)

Requirements:
- Easy for designers to write and modify
- No recompilation needed for content changes
- Sandboxed (scripts can't crash the game or access file system)
- Good performance (evaluated frequently during gameplay)
- Debuggable
- Version controllable (text files)

## Decision

We will use **Lua** (via **MoonSharp** library) for scripting conditions and actions.

### Implementation Strategy

1. **MoonSharp Integration**: Use MoonSharp (pure .NET Lua 5.2 implementation)
2. **Built-in Function Library**: Provide C# functions callable from Lua:
   - `HasFlag(gameState, "flag_name")` → bool
   - `SetFlag(gameState, "flag_name", value)` → void
   - `GetVar(gameState, "var_name")` → int
   - `SetVar(gameState, "var_name", value)` → void
   - `HasItem(gameState, "item_id")` → bool
   - `GiveItem(gameState, "item_id", count)` → void
   - `RemoveItem(gameState, "item_id", count)` → void
   - Additional functions as needed

3. **Script Types**:
   - **Condition scripts**: Return boolean, evaluate game state
   - **Action scripts**: Modify game state, no return value

4. **Common Scripts Compilation**: Frequently-used scripts compiled into C# for performance

### Example Scripts

**Condition** (door requires key):
```lua
return HasItem(gameState, "golden_key")
```

**Action** (consume key and open door):
```lua
RemoveItem(gameState, "golden_key", 1)
SetFlag(gameState, "throne_room_door_open", true)
```

**Complex condition** (multi-stage quest check):
```lua
local questStage = GetVar(gameState, "main_quest_stage")
local hasAlliance = HasFlag(gameState, "northern_alliance")
return questStage >= 5 and hasAlliance
```

## Consequences

### Positive Consequences

- **No recompilation**: Content creators can iterate quickly
- **Easy to learn**: Lua is simple and widely known
- **Sandboxed**: MoonSharp prevents malicious scripts from harming the system
- **Debuggable**: Can log script execution, inspect values
- **Version control friendly**: Plain text files diff well
- **Extensible**: Easy to add new built-in functions as needs arise
- **Cross-platform**: MoonSharp is pure .NET, works everywhere
- **Performance**: MoonSharp is fast enough for our use case

### Negative Consequences

- **Runtime errors**: Scripts can have bugs not caught at compile time
- **Debugging**: Lua errors require different debugging approach than C#
- **Learning curve**: Team needs to learn Lua (though it's simple)
- **Another dependency**: MoonSharp NuGet package required
- **Type safety**: Lua is dynamically typed, can lead to runtime errors

### Neutral Consequences

- Scripts are evaluated at runtime, allowing for dynamic behavior
- Can reload scripts without restarting game (useful for debugging)

## Alternatives Considered

### Alternative 1: Hardcoded C# Classes

**Description**: Create inheritance hierarchy of Condition and Action classes in C#.

```csharp
public class HasItemCondition : ICondition
{
    public string ItemId { get; set; }
    public bool Evaluate(GameState state) => state.Inventory.HasItem(ItemId);
}
```

**Pros**:
- Type-safe
- Compile-time error checking
- IDE support (IntelliSense, refactoring)
- Best performance

**Cons**:
- Requires recompilation for new condition types
- Verbose (lots of boilerplate)
- Content creators must write C#
- Limited flexibility without code changes
- Harder to version control (C# classes vs. simple scripts)

**Why rejected**: Too rigid, slow iteration cycle, requires programming knowledge.

### Alternative 2: Custom DSL

**Description**: Design a custom text-based language for conditions/actions.

```
condition:
  has_item "golden_key"
  
action:
  remove_item "golden_key" 1
  set_flag "door_opened" true
```

**Pros**:
- Tailored exactly to our needs
- Could be simpler than Lua for specific cases
- More constrained (less room for errors)

**Cons**:
- Must implement parser, evaluator, debugger from scratch
- Significant development time
- No ecosystem or community support
- Team must learn proprietary language
- Limited by our implementation effort

**Why rejected**: Too much work for uncertain benefit. Lua is battle-tested and sufficient.

### Alternative 3: JavaScript (via Jint)

**Description**: Use JavaScript instead of Lua, integrated via Jint library.

**Pros**:
- More developers know JavaScript than Lua
- Jint is mature .NET JavaScript engine
- Richer standard library

**Cons**:
- JavaScript is more complex than Lua
- Jint is heavier than MoonSharp
- Async/Promises concepts unnecessary for our use case
- Lua is more common in game scripting

**Why rejected**: JavaScript overkill for simple conditions/actions. Lua is lighter and more game-oriented.

### Alternative 4: Visual Scripting

**Description**: Node-based editor (like Unreal Blueprints or Unity Visual Scripting).

**Pros**:
- No coding required (theoretically)
- Visual representation of logic
- Can be more intuitive for non-programmers

**Cons**:
- Requires significant tooling development
- Harder to version control (XML/JSON blob)
- Difficult to diff/merge changes
- Can become unwieldy for complex logic
- Limited by our tool implementation

**Why rejected**: Too much tooling overhead for a solo/small team project. Text-based scripts are simpler and more flexible.

## Implementation Notes

### MoonSharp Setup

1. Install NuGet package: `MoonSharp`
2. Create `LuaScriptEngine` static class
3. Register built-in functions:

```csharp
_luaEngine.Globals["HasFlag"] = (Func<GameState, string, bool>)
    ((state, flag) => state.HasFlag(flag));
```

4. Evaluate scripts:

```csharp
public static bool EvaluateCondition(string script, GameState state)
{
    _luaEngine.Globals["gameState"] = state;
    DynValue result = _luaEngine.DoString(script);
    return result.Boolean;
}
```

### Built-in Functions

See [lua_api.md][lua-api] for complete API reference.

### Performance Optimization

For very frequently-used scripts (e.g., "always passable"), cache results or use C# fast paths:

```csharp
if (string.IsNullOrEmpty(conditionScript))
    return true;  // Empty condition = always true
```

### Error Handling

Wrap script execution in try-catch:

```csharp
try
{
    return _luaEngine.DoString(script).Boolean;
}
catch (ScriptRuntimeException ex)
{
    GD.PrintErr($"Lua error: {ex.DecoratedMessage}");
    return false;  // Fail-safe: deny traversal on error
}
```

### Testing

Create unit tests for:
- Built-in function behavior
- Complex script evaluation
- Error handling (malformed scripts)

## References

- MoonSharp: http://www.moonsharp.org/
- Lua 5.2 Reference: https://www.lua.org/manual/5.2/
- "Programming in Lua" book: https://www.lua.org/pil/

## Related Decisions

- [ADR-003][adr-003]: Graph-Based Dungeon Model (uses Lua for edges)
- ADR-006: Common Script Library (precompiled frequently-used scripts) [TBD]
