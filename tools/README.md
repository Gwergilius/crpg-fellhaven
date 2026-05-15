# Fellhaven Development Tools

This directory contains tools for Fellhaven development.

## Planned Tools

### 🗺️ Maze Editor
Visual editor for creating and editing dungeon graphs.

**Features**:
- Drag-and-drop location creation
- Visual edge connections
- Lua script editing with syntax highlighting
- Real-time validation
- Export to JSON

**Status**: 📋 Not yet implemented

---

### 🔍 Dungeon Validator
Command-line tool to validate dungeon JSON files.

**Features**:
- Check for broken references
- Validate Lua scripts
- Detect unreachable locations
- Report missing localization keys
- Graph connectivity analysis

**Status**: 📋 Not yet implemented

**Usage** (planned):
```bash
dotnet run --project tools/DungeonValidator -- validate path/to/dungeon.json
```

---

### 📊 Balance Calculator
Spreadsheet-based tool for balancing game systems.

**Features**:
- Combat math simulation
- Loot table generation
- Experience curve calculation
- Difficulty tuning

**Status**: 📋 Not yet implemented

---

### 🌐 Localization Helper
GUI tool for managing translation files.

**Features**:
- Side-by-side translation editing
- Missing key detection
- Export to/import from CSV
- Translation progress tracking

**Status**: 📋 Not yet implemented

---

## Contributing

To add a new tool:

1. Create a subfolder: `tools/ToolName/`
2. Add a README.md describing the tool
3. Implement as standalone .NET console app or Godot tool
4. Update this document

## Requirements

- .NET 8 SDK
- Godot 4.6.2 (for Godot-based tools)
