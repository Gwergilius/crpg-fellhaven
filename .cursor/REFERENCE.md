# Cursor IDE File Reference

Quick reference for important project files that Cursor should consult.

## Core Documentation (Referenced in .cursorrules)

### Project Guidelines
- **`AGENTS.md`** - Complete AI agent instructions
  - Architecture overview
  - Core components and patterns
  - Coding conventions
  - File organization
  - Common workflows

- **`CODING_GUIDELINES.md`** - Coding standards
  - SOLID principles
  - FluentResults (Result pattern)
  - MediatR (Mediator pattern, CQRS)
  - C# 14 features
  - Error handling

- **`CONTRIBUTING.md`** - Contribution guide
  - Conventional Commits
  - Pull request process
  - Testing requirements

## Architecture Decision Records

- **`docs/adr/001-graph-based-dungeon.md`** - Graph model design
- **`docs/adr/002-lua-scripting.md`** - Lua integration
- **`docs/adr/003-localization.md`** - i18n system

## API Documentation

- **`docs/api/lua_api.md`** - Complete Lua API reference
  - Flag management
  - Variables
  - Inventory
  - Party system

- **`docs/api/graph_format.md`** - JSON dungeon format
  - Location structure
  - Edge format
  - Priority system
  - Action execution

## Key Implementation Files

### Core Systems
```
src/Fellhaven/scripts/core/
├── Location.cs           # Graph node, navigation
├── Edge.cs               # Graph edge, conditions
├── Direction.cs          # Enum + extensions
├── GameState.cs          # Singleton, flags/vars/inventory
└── LuaScriptEngine.cs    # MoonSharp integration
```

### Supporting Systems
```
src/Fellhaven/scripts/systems/
├── DungeonLoader.cs      # JSON → graph
└── LocalizationManager.cs # i18n singleton
```

## Example Dungeon Data

- **`src/Fellhaven/data/dungeons/test_dungeon.json`** - Example dungeon with locked door

## Localization

- **`src/Fellhaven/localization/en.json`** - English translations
- **`src/Fellhaven/localization/hu.json`** - Hungarian translations

## How Cursor Uses This

When you ask Cursor a question, it will:

1. **Consult `.cursorrules`** first for general guidance
2. **Reference documentation** mentioned above for specific details
3. **Apply patterns** from `CODING_GUIDELINES.md`
4. **Follow conventions** from `AGENTS.md`

Example flow:
```
You: "Add a new Lua function to check if player has a specific class"

Cursor:
1. Checks AGENTS.md → finds Lua API section
2. Checks docs/api/lua_api.md → sees existing party functions
3. Checks CODING_GUIDELINES.md → applies Result pattern
4. Generates code following project patterns
```

---

**Tip**: If Cursor's response doesn't match project standards, mention the specific guideline file:
- "Following CODING_GUIDELINES.md, use Result<T> instead of throwing..."
- "According to AGENTS.md, Lua functions should be registered in..."
