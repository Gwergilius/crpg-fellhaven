# Fellhaven

[godot-download]: https://godotengine.org/download
[dotnet-download]: https://dotnet.microsoft.com/download
[csharp-conventions]: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
[contributing]: CONTRIBUTING.md
[adr]: docs/adr/
[gdd]: docs/gdd/GDD.md
[lua-api]: docs/api/lua_api.md
[dev-guides]: docs/guides/
[coding-guidelines]: CODING_GUIDELINES.md
[license]: LICENSE

A turn-based fantasy CRPG built with Godot 4.6.2 and C#.

## Project Structure

```
Fellhaven/
├─ .github/          # GitHub Actions and issue templates
├─ docs/             # Documentation (ADR, GDD, API docs)
├─ src/              # Source code (Godot project)
├─ tests/            # Unit and integration tests
├─ tools/            # Development tools (Maze Editor, etc.)
└─ README.md         # This file
```

## Tech Stack

- **Engine**: Godot 4.6.2
- **Language**: 
  - Preferred: .NET 10 + C# 14
  - Fallback: .NET 8.0 + C# 12 (if Godot doesn't support .NET 10)
- **Scripting**: Lua (via MoonSharp)
- **Platforms**: Windows, Android, iOS

## Getting Started

### Prerequisites

- [Godot 4.6.2 (.NET version)][godot-download]
- [.NET 10 SDK][dotnet-download]
- Visual Studio 2026 or Visual Studio Code with C# extension
- **Optional**: [Cursor IDE](https://cursor.sh/) - AI-powered editor with project-specific configuration

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/Gwergilius/crpg-fellhaven.git
   cd crpg-fellhaven
   ```

2. Open the Godot project:
   - Launch Godot 4.6.2
   - Click "Import"
   - Navigate to `src/Fellhaven/`
   - Select `project.godot`

3. Build the C# solution:
   - In Godot, click "Build" in the top-right corner
   - Or use command line: `dotnet build src/Fellhaven.slnx`

### Running Tests

```bash
cd tests
dotnet test
```

## Development

### Coding Conventions

- Follow [C# Coding Conventions][csharp-conventions]- Follow [Coding Guidelines][coding-guidelines] (SOLID, FluentResults, MediatR)- Use `.slnx` solution format (Visual Studio 2026+)
- All comments, documentation, and commit messages in English
- See [CONTRIBUTING.md][contributing] for detailed guidelines

### Architecture

The project uses a graph-based dungeon model where:
- **Nodes** represent locations/rooms
- **Edges** represent transitions between locations
- **Lua scripts** define conditions and actions

See [Architecture Decision Records][adr] for detailed design decisions.

### AI Assistant Support

This project includes configuration for AI-powered development tools:

- **Cursor IDE**: Pre-configured with `.cursorrules` - see [.cursor/README.md](.cursor/README.md)
- **GitHub Copilot / Claude**: Project guidelines in `AGENTS.md` and `CODING_GUIDELINES.md`

All AI assistants are configured to follow the same coding standards, architectural patterns, and documentation conventions.

## Documentation

- [Game Design Document][gdd]
- [Architecture Decision Records][adr]
- [Lua API Reference][lua-api]
- [Developer Guides][dev-guides]

## License

[MIT License][license]

## Contributing

See [CONTRIBUTING.md][contributing] for guidelines.

## Status

🚧 **In Development** - Early prototype phase

---

**Fellhaven** - *Where shadows gather and heroes rise*
