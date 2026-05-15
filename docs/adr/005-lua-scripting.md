[lua_api.md]: ../api/lua_api.md "Lua API Reference"
[ADR-003]: 003-graph-based-dungeon.md "World Data Model"
[ADR-004]: 004-condition-and-combat.md "Condition Vocabulary and Combat System"
[ADR-006]: 006-localization.md "Internationalization Strategy"

# ADR-005: Event-Action System with Lua Scripting

**Status**: Accepted

**Date**: 2026-05-15

## Context

The dungeon graph system ([ADR-003]) requires:
- **Conditions**: When is an edge traversable? When does a combat action execute?
- **Actions**: What happens when entering a node? Traversing an edge? Interacting with an object?

[ADR-004] defines a **closed condition vocabulary** for common cases (has_item, stat_check, etc.).
This ADR addresses the **action system** — what the game *does* in response to player choices,
world state changes, and narrative triggers.

### Requirements

The action system must be:
- **Data-driven**: actions defined in YAML source files, not hardcoded C#
- **Editor-friendly**: the MazeEditor (future) can present actions visually
- **Moddable**: community contributors can add custom logic without touching game code
- **Cross-platform**: must work on Desktop, Web (WASM), and Mobile (iOS AOT)
- **Safe**: sandboxed execution — scripts cannot access file system, network, or game internals
  beyond an explicit API surface

### Design Tension

A purely declarative action vocabulary (like [ADR-004]'s condition vocabulary) would limit
expressiveness. Complex multi-step interactions — unique puzzles, branching NPC dialogues,
conditional item trades — require imperative logic that a fixed schema cannot anticipate.

Yet a purely scripted system (everything in Lua) sacrifices editor tooling, static analysis,
and performance for common cases.

The solution is a **two-tier architecture**.

## Decision

Implement a **two-tier event-action system**:

### Tier 1 — Finite Action Vocabulary

A closed, pre-compiled set of action types covering the most common Fellhaven events.
Each action type is a named C# handler class registered in `EventDispatcher.cs`.
Actions receive parameters from YAML/JSON via a dictionary.

All text parameters are **string keys** resolved via the i18n system ([ADR-006]) —
no inline text in action parameters.

### Tier 2 — Lua Escape Hatch (`execute_script`)

A single additional action type, `execute_script`, that runs a Lua script package via
**MoonSharp** (pure C# Lua 5.2 interpreter). Used for:
- Edge cases not covered by Tier 1
- Unique puzzles and complex branching logic
- Bugfixes and balance adjustments without recompilation
- Community mods

The Lua environment is **sandboxed**: scripts can only call an explicit, limited API
surface exposed by the game. They cannot access the file system, network, or game internals
beyond what is intentionally exposed.

## Action Vocabulary (Tier 1)

Common action types implemented as C# handlers:

| Action type | Parameters | Description |
|---|---|---|
| `show_message` | `text_key` | Display a message (sign, inscription, NPC line) |
| `enter_location` | `location_ref` | Enter a named location (inn, shop, temple, guild) |
| `trigger_trap` | `type`, `damage_formula` | Trigger a trap (pit, poison gas, blade). Uses ADR-004 formula syntax. |
| `set_edge_state` | `edge_id`, `state` | Open/close/lock a secret door, drawbridge, or gate. States: `open`, `closed`, `locked`. |
| `force_encounter` | `monster_group` | Initiate unavoidable combat |
| `use_shrine` | `deity`, `effect` | Shrine interaction (stat buff, HP restore, blessing) |
| `give_item` | `item_id`, `quantity` | Place item(s) into party inventory |
| `remove_item` | `item_id`, `quantity` | Remove item(s) from party inventory |
| `require_item` | `item_id`, `on_success`, `on_fail` | Gate: proceed only if party has item |
| `trade_item` | `npc_text_key`, `give`, `receive`, `success_key`, `fail_key` | Exchange items/gold with NPC |
| `set_darkness_zone` | `radius` | Mark area as requiring a light source |
| `set_flag` | `flag_id`, `value` | Set a global or dungeon-local boolean flag |
| `check_flag` | `flag_id`, `on_true`, `on_false` | Branch on a flag value (executes nested action chains) |
| `play_sound` | `sound_id` | Play a sound effect or music track |
| `heal_party` | `amount` | Restore HP to all party members (total, distributed) |
| `damage_party` | `amount`, `type` | Damage all party members (trap, environmental) |
| `execute_script` | `package` | Run a Lua script package (Tier 2 — see below) |

> **Note**: Cross-region transitions (map changes, teleporters) are **not actions** — they are
> edges in the world graph (see [ADR-003]). An edge with `direction: null` represents
> a teleporter pad; edges with `to` in a different region are cross-region transitions.

New action types can be added to the vocabulary in future phases without breaking existing
data files (unrecognised types are logged and skipped).

### Example: trade_item action

```yaml
- type: trade_item
  params:
    npc_text_key: fellhaven.trader.rope_for_key.prompt
    give:
      item_id: rope
      quantity: 3
    receive:
      item_id: magic_key
      quantity: 1
    success_key: fellhaven.trader.rope_for_key.success
    fail_key: fellhaven.trader.rope_for_key.fail
```

All `*_key` parameters resolve via the i18n system — no inline text in action definitions.

## Lua Scripting (Tier 2)

### Script Packages

Each Lua script is distributed as a **ZIP package** containing the logic file and all
language files. See [ADR-006] for the full package format specification.

```
assets/scripts/events/trader_rope_key.zip
  trader_rope_key.lua
  trader_rope_key.en.i18
  trader_rope_key.hu.i18
```

The i18n files use the same key-based format as the global localization system, but scoped
to the script. Example:

```yaml
# trader_rope_key.en.i18
greeting: "Hello traveler, what do you seek?"
trade:
  prompt: "Give 3 ROPE for 1 MAGIC KEY?"
  success: "The trader hands you the key."
  fail: "You don't have enough rope."
```

### MoonSharp Integration

**MoonSharp** is a pure .NET Lua 5.2 interpreter — no native code, no JIT compilation,
fully compatible with .NET AOT and WebAssembly builds.

Implementation (simplified):

```csharp
// EventDispatcher.cs
case "execute_script":
    var package = ScriptPackage.Load(action.Parameters["package"]);
    var i18n    = new ScriptI18n(package, currentLanguage);
    var script  = new MoonSharp.Interpreter.Script();
    
    // Register sandboxed API
    script.Globals["party"] = new PartyScriptApi(party);
    script.Globals["world"] = new WorldScriptApi(currentDungeon);
    script.Globals["game"]  = new GameScriptApi(gameManager, i18n);
    script.Globals["i18n"]  = i18n;
    
    // Execute as coroutine (for game.ask() / game.choose())
    script.LoadString(package.LuaSource).Coroutine();
    break;
```

### Sandboxed API Surface

All text-producing functions take **string keys** resolved via the script's `.i18` file.

```lua
-- party API
party.hasItem(item_id)              -- bool
party.hasItem(item_id, quantity)    -- bool
party.removeItem(item_id, quantity) -- void
party.addItem(item_id, quantity)    -- void
party.heal(amount)                  -- void
party.damage(amount, type)          -- void
party.gold()                        -- int
party.addGold(amount)               -- void
party.removeGold(amount)            -- void

-- world API (current dungeon/region)
world.setEdgeState(edge_id, state)  -- void   (state: "open"|"closed"|"locked")
world.setFlag(flag_id, value)       -- void
world.getFlag(flag_id)              -- bool
world.showMessage(text_key)         -- void   (key resolved via script .i18)

-- game API (coroutine-based — suspends script until player responds)
game.ask(text_key)                  -- bool   (Y/N prompt)
game.choose(text_key, option_keys)  -- int    (multiple choice, 1-based index)
game.startEncounter(monster_group)  -- void
game.playSound(sound_id)            -- void
game.navigateTo(node_id)            -- void   (forced navigation)
```

Nothing outside this surface is accessible. The Lua environment has **no I/O, no OS access,
no reflection, no access to C# internals**.

### Example Lua Script

```lua
-- trader_rope_key.lua
-- No inline text — all keys resolved from trader_rope_key.en.i18 / .hu.i18

world.showMessage("greeting")

local answer = game.ask("trade.prompt")
if answer then
    if party.hasItem("rope", 3) then
        party.removeItem("rope", 3)
        party.addItem("magic_key", 1)
        world.showMessage("trade.success")
    else
        world.showMessage("trade.fail")
    end
end
```

### Player Input via Coroutines

`game.ask()` and `game.choose()` **suspend the Lua coroutine** and hand control back to
the game loop. When the player responds, the EventDispatcher **resumes the coroutine**
with the answer. This requires no polling and fits naturally into the Godot frame loop.

### Error Handling

Wrap script execution in try-catch with fail-safe behavior:

```csharp
try
{
    script.LoadString(package.LuaSource).Coroutine();
}
catch (ScriptRuntimeException ex)
{
    GD.PrintErr($"Lua script error in {package.Name}: {ex.DecoratedMessage}");
    // Log but don't crash — the event fails gracefully
}
```

## Platform Compatibility

| Platform | Tier 1 (C# handlers) | Tier 2 (MoonSharp Lua) |
|---|---|---|
| Desktop (Win/Mac/Linux) | ✅ Yes | ✅ Yes |
| Web (Godot WASM export) | ✅ Yes | ✅ Yes — pure C#, no JIT |
| Mobile Android | ✅ Yes | ✅ Yes |
| Mobile iOS | ✅ Yes | ✅ Yes — interpreter, AOT-compatible |

Alternatives evaluated and **rejected** for cross-platform reasons:

- **Roslyn C# scripting**: requires JIT, incompatible with iOS and WASM
- **IronPython**: heavier runtime, limited WASM support
- **JavaScript (Jint)**: heavier than MoonSharp, unnecessary async complexity
- **GDScript**: Godot-internal only, not callable as data-driven scripts from C# without
  significant coupling

## Development Workflow — Script-First Pattern

New action types follow a **script-first promotion cycle**:

```
1. Prototype — Implement the new action as a Lua script package (execute_script)
               Fast iteration, no recompile, testable immediately in-game

2. Stabilise — Iterate on the Lua script until the behaviour is accepted as final
               The script serves as a runnable specification

3. Promote   — Translate the finalised Lua script to a named C# action class
               AI-assisted translation is reliable: the Lua script is small,
               isolated, and unambiguous — ideal input for code generation

4. Register  — Add the new type to EventDispatcher and MazeEditor dropdown
               Replace execute_script references in YAML with the new type
```

The Lua layer is not only an escape hatch and modding surface, but also the **primary
prototyping environment** for the C# action vocabulary itself. The vocabulary grows
organically: only battle-tested, accepted behaviours are promoted.

## Modding Implications

The two-tier system creates a clear modding surface:

- **Tier 1 mods**: new dungeons using existing action types — YAML data files only, no code
- **Tier 2 mods**: custom logic in Lua script packages — no C# compilation required

A mod is a ZIP of dungeon YAML files + script packages (`.lua` + `.i18` files).
No compiled binary components. Language translations ship inside the script packages.

This is a first-class modding API, not an afterthought.

## Consequences

### Positive

- **Data-driven content**: Actions defined in YAML, not hardcoded C#
- **Two-tier flexibility**: Common cases use fast C# handlers, edge cases use Lua
- **Script-first development**: Lua prototypes become C# actions — fast iteration
- **Safe modding surface**: Sandboxed Lua API, no file system or network access
- **No recompilation**: Content changes don't require rebuilding the game
- **Cross-platform**: MoonSharp is pure .NET, works on Desktop, Web (WASM), Mobile (iOS AOT)
- **Coroutine-based player input**: `game.ask()` / `game.choose()` enable interactive NPC dialogues
- **Editor-friendly**: Tier 1 actions can be presented in visual tools (MazeEditor)
- **Fail-safe**: Lua errors are logged but don't crash the game

### Negative

- **Lua learning curve**: Team and modders need to learn Lua (though it's simple)
- **Runtime errors**: Lua scripts can have bugs not caught at compile time
- **API surface maintenance**: Adding new Lua capabilities requires deliberate C# API expansion
- **MoonSharp dependency**: Adds ~250 KB NuGet package
- **Coroutine lifecycle**: Requires careful management in EventDispatcher

### Neutral

- Lua scripts are slightly slower than C# handlers (acceptable: events run once per player
  action, not per frame)
- The action vocabulary can grow over time; existing YAML files are forward-compatible

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
- Visual representation of logic
- Can be more intuitive for designers familiar with node-based tools

**Cons**:
- Requires significant tooling development
- Harder to version control (XML/JSON blob)
- Difficult to diff/merge changes
- Can become unwieldy for complex logic
- Limited by our tool implementation

**Why rejected**: Too much tooling overhead for a solo/small team project. Text-based scripts are simpler and more flexible.

### Alternative 5: Single-Tier System (Lua Only)

**Description**: Everything is a Lua script — no Tier 1 C# action vocabulary.

**Pros**:
- Maximum flexibility
- No "should this be Tier 1 or Tier 2" decisions
- Simplest architecture

**Cons**:
- Performance overhead for every action, even trivial ones
- No static analysis or visual editing for common cases
- Lua must handle 100% of game actions, including those that could be simpler as data

**Why rejected**: The two-tier system gives us the best of both worlds — fast, editor-friendly
data-driven actions for common cases, and full scripting power for edge cases. The promotion
cycle (Lua prototype → C# action) means the C# vocabulary grows organically based on actual
usage patterns.

## Implementation Notes

### EventDispatcher Architecture

```csharp
public class EventDispatcher
{
    private readonly Dictionary<string, IActionHandler> _handlers = new();
    
    public void RegisterHandler(string actionType, IActionHandler handler)
    {
        _handlers[actionType] = handler;
    }
    
    public void Execute(ActionData action, GameContext context)
    {
        if (_handlers.TryGetValue(action.Type, out var handler))
        {
            handler.Execute(action.Parameters, context);
        }
        else
        {
            GD.PrintErr($"Unknown action type: {action.Type}");
        }
    }
}
```

Each Tier 1 action implements `IActionHandler`. The `execute_script` action is just another
handler that loads and runs Lua.

### MoonSharp Setup

1. Install NuGet package: `MoonSharp`
2. Create `LuaScriptEngine` static class
3. Register sandboxed API:

```csharp
var script = new Script();
script.Globals["party"] = UserData.Create(new PartyScriptApi(party));
script.Globals["world"] = UserData.Create(new WorldScriptApi(dungeon));
script.Globals["game"]  = UserData.Create(new GameScriptApi(gameManager));
```

4. Execute as coroutine (for player input):

```csharp
DynValue coroutine = script.LoadString(luaCode).Coroutine();
DynValue result = coroutine.Coroutine.Resume();
// Handle yields from game.ask() / game.choose()
```

### Sandboxed API Implementation

See [lua_api.md] for complete API reference. Each API namespace (`party`, `world`,
`game`) is a separate C# class exposing only the intended methods via MoonSharp's `UserData`
registration.

### Performance Optimization

For very frequently-used conditions/actions, fast paths:

```csharp
if (string.IsNullOrEmpty(conditionScript))
    return true;  // Empty condition = always true
```

Tier 1 actions are always faster than Tier 2 scripts — this is by design.

### Testing

Create unit tests for:
- Each Tier 1 action handler
- Lua API surface (party, world, game)
- Script package loading and i18n resolution
- Coroutine suspend/resume for player input
- Error handling (malformed scripts, missing parameters)

## References

- MoonSharp: http://www.moonsharp.org/
- Lua 5.2 Reference: https://www.lua.org/manual/5.2/
- "Programming in Lua" book: https://www.lua.org/pil/

## Related ADRs

- [ADR-003]: World Data Model — defines where actions appear (node `on_enter`,
  edge `on_traverse`, etc.)
- [ADR-004]: Condition Vocabulary and Combat System — closed condition types
  complement this ADR's closed action types
- [ADR-006]: Localization System — script packages use `.i18` files for text

---

**Owner**: Fellhaven Team  
**Last Updated**: 2026-05-15
