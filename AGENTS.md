# Fellhaven - AI Agent Instructions

[adr-001]: docs/adr/001-engine-choice.md "Engine Choice"
[adr-002]: docs/adr/002-platform-strategy.md "Multi-Platform Development Strategy"
[adr-003]: docs/adr/003-graph-based-dungeon.md "Graph-Based Dungeon Model"
[adr-004]: docs/adr/004-condition-and-combat.md "Condition Vocabulary and Combat System"
[adr-005]: docs/adr/005-lua-scripting.md "Event-Action System with Lua Scripting"
[adr-006]: docs/adr/006-localization.md "Internationalization Strategy"
[location]: src/Fellhaven/scripts/core/Location.cs
[edge]: src/Fellhaven/scripts/core/Edge.cs
[gamestate]: src/Fellhaven/scripts/core/GameState.cs
[localizationmanager]: src/Fellhaven/scripts/systems/LocalizationManager.cs
[luascriptengine]: src/Fellhaven/scripts/core/LuaScriptEngine.cs
[dungeonloader]: src/Fellhaven/scripts/systems/DungeonLoader.cs
[direction]: src/Fellhaven/scripts/core/Direction.cs
[conventional-commits]: https://www.conventionalcommits.org/ "Conventional Commits Specification"
[tests-readme]: tests/README.md
[gdd]: docs/gdd/GDD.md "Game Design Document"
[adr-index]: docs/adr/
[lua-api]: docs/api/lua_api.md "Lua API Reference"
[graph-format]: docs/api/graph_format.md "Graph Format Specification"
[contributing]: CONTRIBUTING.md
[coding-guidelines]: CODING_GUIDELINES.md "Coding Guidelines"
[cursor-readme]: .cursor/README.md "Cursor IDE Configuration"

Fellhaven is a turn-based fantasy CRPG built with **Godot 4.6.2** and **C# (.NET 10)**, using a graph-based dungeon model with Lua scripting.

> **Note**: This file contains instructions for GitHub Copilot and Claude. If you're using Cursor IDE, see [.cursor/README.md][cursor-readme] - Cursor has its own `.cursorrules` file that references this document.

## Quick Start

### Build & Test
```powershell
# Build C# solution
dotnet build src/Fellhaven.slnx

# Run tests
cd tests
dotnet test
```

### Open in Godot
- Launch Godot 4.6.2 (.NET version)
- Import → `src/Fellhaven/project.godot`
- Click **Build** (top-right) to generate C# project

### Prerequisites
- Visual Studio 2026 or VS Code with C# extension
- .NET 10 SDK (fallback: .NET 8 if Godot doesn't support .NET 10)
- C# 14 (fallback: C# 12)

## Architecture Overview

### Core Design Patterns

**Graph-Based Dungeon Model** (see [ADR-003][adr-003]):
- **Nodes** = [Location][location] objects (rooms/areas)
- **Edges** = [Edge][edge] objects (transitions with conditions)
- Navigation: Player chooses direction → system finds edges in that direction ordered by `Priority` (lower = higher precedence) → first edge whose Lua condition evaluates `true` is taken

**Condition and Combat System** (see [ADR-004][adr-004]):
- Unified condition vocabulary for world and combat contexts
- Dice formula syntax with stat references: `2d10+{DEX}+1`
- Subject/quantifier system: `party`, `party.any`, `party.all`, `ally`, `enemy`, etc.
- Logical connectives: `and`, `or`, `not` for composing conditions

**Event-Action System** (see [ADR-005][adr-005]):
- **Tier 1**: Finite action vocabulary (C# handlers) for common events
- **Tier 2**: Lua escape hatch (`execute_script`) for unique puzzles and mods
- **MoonSharp** (Lua 5.2) for sandboxed scripting
- **Script-first pattern**: Prototype in Lua, promote to C# if common
- **No recompilation needed** for content changes—edit YAML/JSON source files only
- **Fail-safe**: Lua errors logged but don't crash the game

**Localization** (see [ADR-006][adr-006]):
- **Template-based rendering**: `{placeholder}` syntax for dynamic text composition
- **No string concatenation**: Only templates + placeholder substitution allowed
- **Two-tier system**:
  - Tier 1: JSON files for static UI text (`localization/en.json`, `localization/hu.json`)
  - Tier 2: YAML `.i18` files bundled with Lua script packages
- Keys use dot notation: `location.entrance.name`, `msg.item_found`, `ui.menu.new_game`
- Fallback chain: current language → English → `[MISSING: key]`
- Parameter substitution: `Tr("msg.item_found", ("item", "Key"), ("location", "Crypt"))`

### Key Components

| Class | Pattern | Responsibilities |
|-------|---------|------------------|
| [GameState][gamestate] | Godot Singleton | Global flags, variables, inventory, party management |
| [LocalizationManager][localizationmanager] | Godot Singleton | Template translation via `Tr(key, params)`, placeholder substitution, runtime language switching |
| [LuaScriptEngine][luascriptengine] | Static Utility | Lua condition/action evaluation via MoonSharp |
| [DungeonLoader][dungeonloader] | Static Utility | YAML/JSON deserialization into Location/Edge graph |
| [Direction][direction] | Enum + Extensions | 8 compass directions + Up/Down, with `GetOpposite()`, `ToShortString()` |

## Coding Conventions

### Naming
| Type | Convention | Examples |
|------|-----------|----------|
| **C# classes/methods/properties** | `PascalCase` | `GameState`, `TryMove()`, `CurrentLocation` |
| **C# local variables** | `camelCase` | `edgesInDirection`, `isLocked` |
| **C# private fields** | `_camelCase` | `_instance`, `_items`, `_currentLanguage` |
| **Game IDs** (dungeons, locations, edges) | `snake_case` | `test_dungeon`, `entrance`, `corridor_to_locked_room` |
| **Localization keys** | `dot.separated.snake_case` | `location.entrance.name`, `ui.menu.new_game` |
| **GameState flags/variables** | `snake_case` | `treasure_room_unlocked`, `main_quest_stage` |

### File Organization
```
scripts/
├─ core/        # Game logic: Location, Edge, Direction, GameState, LuaScriptEngine
└─ systems/     # Data loading & services: DungeonLoader, LocalizationManager
```

### Patterns in Use

**Singletons** (2 variants):
```csharp
// Godot Node Singleton (requires scene tree)
public partial class GameState : Node
{
    private static GameState? _instance;
    public static GameState Instance => _instance!;
    
    public override void _Ready() => _instance = this;
}

// Static utility class (no scene tree needed)
public static class LuaScriptEngine
{
    public static void Initialize() { ... }
}
```

**Extension Methods**:
```csharp
public static class DirectionExtensions
{
    public static Direction GetOpposite(this Direction d) { ... }
    public static string ToShortString(this Direction d) { ... }
}
```

**Error Handling**:
- Use `GD.Print()` for info, `GD.PrintErr()` for errors
- Use **FluentResults** `Result<T>` for expected failures (business logic, validation)
- Use exceptions only for exceptional circumstances (programming errors, system failures)
- Prefer `?.` operator and early returns over deep nesting

**Component Communication**:
- Use **MediatR** for loose coupling between components
- Implement CQRS pattern: Commands (modify state), Queries (read data), Notifications (events)
- Define handlers separately from business logic

**SOLID Principles**:
- Follow SOLID design principles (see [CODING_GUIDELINES.md][coding-guidelines])
- Single Responsibility: One class, one reason to change
- Dependency Inversion: Depend on abstractions, not concretions

### XML Documentation
Add XML comments to all public APIs:
```csharp
/// <summary>
/// Evaluates a Lua condition script against the game state.
/// </summary>
/// <param name="script">Lua script that returns boolean.</param>
/// <param name="gameState">Current game state.</param>
/// <returns>True if condition passes, false on error or failure.</returns>
public static bool EvaluateCondition(string script, GameState gameState)
```

### Markdown Documentation
**NEVER use inline links** in Markdown documentation. Always use **reference-style links**.

**Preferred**: Use **shorthand reference** when the link text matches a suitable reference ID:
```markdown
See [ADR-001] for details.
Check [CODING_GUIDELINES] for code standards.

[ADR-001]: docs/adr/001-engine-choice.md
[CODING_GUIDELINES]: CODING_GUIDELINES.md
```

**Alternative**: Use separate reference IDs when link text differs from reference:
```markdown
See the [graph-based dungeon model][adr-003] for details.
Check out [this guide][lua-api].

[adr-001]: docs/adr/001-graph-based-dungeon.md "Graph-Based Dungeon Model"
[lua-api]: docs/api/lua_api.md
```

**❌ Never use inline links**:
```markdown
See the [ADR-001](docs/adr/001-engine-choice.md) for details.
Check out [this guide](docs/api/lua_api.md).
```

**Link definitions** must be placed at the **top of the file** (after title/intro). The tooltip part (`"tooltip text"`) is optional but recommended for clarity.

**Images** follow the same rules:
```markdown
![Architecture diagram][arch-diagram]

[arch-diagram]: docs/images/architecture.png "System architecture overview"
```

## Important Implementation Details

### Edge Priority System
Multiple edges from same location in same direction are checked **by priority** (lower = higher precedence):
```csharp
var edges = location.OutgoingEdges
    .Where(e => e.Direction == direction)
    .OrderBy(e => e.Priority);  // Priority 1 checked before Priority 2
```

Use cases:
- **Locked doors**: Priority 1 edge shows "door locked" message, Priority 2 edge (higher priority) actually moves if unlocked
- **Conditional passages**: Check special condition first, fall back to default

### Lua Built-In Functions
Available in all Lua scripts (conditions/actions):
```lua
-- Flags
HasFlag(gameState, "flag_name")
SetFlag(gameState, "flag_name", true)

-- Variables
GetVar(gameState, "var_name")  -- returns 0 if not set
SetVar(gameState, "var_name", 5)
AddVar(gameState, "counter", 1)

-- Inventory
HasItem(gameState, "item_id")
HasItems(gameState, "item_id", 5)  -- check count >= 5
GiveItem(gameState, "item_id", 1)
RemoveItem(gameState, "item_id", 1)

-- Party
HasClass(gameState, "warrior")
HasSkill(gameState, "spell_name", 1)  -- at least level 1
GetPartyLevel(gameState)
```

### Action Execution
All actions on an edge execute **sequentially**, each with optional condition:
```yaml
actions:
  - condition_script: 'return HasItem(gameState, "key")'
    action_script: |
      RemoveItem(gameState, "key", 1)
      SetFlag(gameState, "door_unlocked", true)
  - action_script: 'GiveItem(gameState, "xp", 10)'
```

## Git Workflow

### Commit Messages
Use [Conventional Commits][conventional-commits]:
```
<type>(<scope>): <subject>

<body>
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

**Examples**:
```
feat(combat): add critical hit system
fix(navigation): correct edge priority ordering
docs(api): update Lua API reference with new functions
refactor(core): simplify location graph traversal
test(dungeon): add edge condition evaluation tests
```

## Testing Strategy

- Unit tests in `tests/Fellhaven.Tests/`
- Test core logic independently of Godot (Location, Edge, Direction, GameState)
- Use `dotnet test` to run all tests
- See [tests/README.md][tests-readme] for guidelines

## Documentation

- **Game Design Document**: [docs/gdd/GDD.md][gdd]
- **Architecture Decision Records**: [docs/adr/][adr-index]
- **Lua API Reference**: [docs/api/lua_api.md][lua-api]
- **Graph Format Spec**: [docs/api/graph_format.md][graph-format]
- **Coding Guidelines**: [CODING_GUIDELINES.md][coding-guidelines]
- **Contributing Guide**: [CONTRIBUTING.md][contributing]

## Common Tasks

### Adding a New Location
1. Edit dungeon YAML/JSON in `data/dungeons/`
2. Add location translations to `localization/en.json`, `localization/hu.json`
3. Define edges with `direction`, `priority`, `condition_script`, `action_script`
4. Test with `dotnet test` (if unit testable) or in-game

### Adding a New Lua Function
1. Add method to `LuaScriptEngine.RegisterGameStateFunctions()`
2. Update [docs/api/lua_api.md][lua-api]
3. Add unit tests in `tests/Fellhaven.Tests/Core/`

### Adding Localization Keys
1. Add to `localization/en.json` (primary)
2. Add to `localization/hu.json`
3. Use key naming: `category.subcategory.identifier` (e.g., `location.dungeon_name.room_id.name`)

## Solution File Format

This project uses **`.slnx`** (Visual Studio 2026+ XML solution format) instead of `.sln`. Both VS 2026 and VS Code support it.

## Dependencies

- **Godot 4.6.2** (.NET version)
- **.NET 10 SDK** (fallback: .NET 8)
- **C# 14** (fallback: C# 12)
- **MoonSharp** (Lua 5.2 implementation for .NET)
- **FluentResults** (Result pattern for error handling)
- **MediatR** (Mediator pattern for loose coupling)
- **System.Text.Json** (JSON deserialization)

---

**Key Principle**: The dungeon graph is **data-driven**—gameplay logic lives in YAML/JSON + Lua, not hardcoded C#. When adding features, prefer extending Lua API over C# classes.
