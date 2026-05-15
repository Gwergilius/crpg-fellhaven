# Cursor IDE Configuration

This project includes configuration for [Cursor IDE](https://cursor.sh/), an AI-powered code editor.

## Configuration Files

### `.cursorrules`
The main configuration file for Cursor IDE, located in the project root. This file instructs Cursor to:

1. **Read project guidelines** from existing documentation:
   - `AGENTS.md` - AI agent instructions, architecture, coding conventions
   - `CODING_GUIDELINES.md` - SOLID principles, FluentResults, MediatR patterns
   - `CONTRIBUTING.md` - Contribution guidelines and commit conventions

2. **Follow established patterns**:
   - Data-driven design (JSON + Lua for gameplay logic)
   - Graph-based dungeon model
   - Result pattern instead of exceptions
   - MediatR for component communication

3. **Apply naming conventions**:
   - C# code: PascalCase, camelCase, _camelCase patterns
   - Game data: snake_case for IDs and flags
   - Markdown: reference-style links only

## Why This Approach?

Instead of duplicating documentation, the `.cursorrules` file **references** existing project files. This ensures:

- ✅ **Single source of truth** - Documentation stays in sync
- ✅ **No duplication** - Rules defined once in `AGENTS.md` and `CODING_GUIDELINES.md`
- ✅ **Easy maintenance** - Update docs once, Cursor sees changes automatically
- ✅ **Consistency** - Claude, Cursor, and human developers follow the same guidelines

## Using Cursor with This Project

1. **Open the project** in Cursor IDE
2. Cursor automatically loads `.cursorrules` on startup
3. When asking Cursor for help, it will:
   - Reference the project's architecture (from `AGENTS.md`)
   - Follow coding guidelines (from `CODING_GUIDELINES.md`)
   - Use the correct naming conventions
   - Apply SOLID principles, Result pattern, and MediatR

## Example Prompts

```
"Add a new Lua API function for checking party skills"
→ Cursor will follow AGENTS.md for Lua API patterns

"Refactor DungeonLoader to use Result pattern"
→ Cursor will apply FluentResults from CODING_GUIDELINES.md

"Create a new MediatR command for unlocking doors"
→ Cursor will use MediatR patterns from CODING_GUIDELINES.md
```

## Key Documentation to Consult

When working with Cursor, you may want to manually reference:

- **Architecture**: `docs/adr/*.md` - Architecture Decision Records
- **Lua API**: `docs/api/lua_api.md` - Complete Lua function reference
- **Data Format**: `docs/api/graph_format.md` - JSON dungeon structure
- **Game Design**: `docs/gdd/GDD.md` - Overall game design

## Troubleshooting

If Cursor doesn't seem to follow the guidelines:
1. Ensure `.cursorrules` is in the project root
2. Restart Cursor IDE to reload configuration
3. Explicitly mention the guideline files in your prompt: "Following CODING_GUIDELINES.md, implement..."

---

**Note**: The `.cursorrules` file should be committed to version control so all team members using Cursor have the same AI assistant behavior.
