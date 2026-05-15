# Contributing to Fellhaven

[conventional-commits]: https://www.conventionalcommits.org/ "Conventional Commits Specification"
[coding-guidelines]: CODING_GUIDELINES.md

Thank you for your interest in contributing to Fellhaven!

## Code of Conduct

- Be respectful and constructive
- Focus on what is best for the project and community
- Show empathy towards other contributors

## Development Tools

### Supported IDEs

- **Visual Studio 2026** - Full C# and Godot support
- **Visual Studio Code** - With C# extension and Godot tools
- **Cursor IDE** - AI-powered editor with project-specific configuration (`.cursorrules`)

### AI Assistant Configuration

This project includes configurations for AI coding assistants:

- **`.cursorrules`** - Cursor IDE configuration
- **`AGENTS.md`** - GitHub Copilot and Claude instructions
- **`CODING_GUIDELINES.md`** - Code patterns and architectural guidelines

All AI assistants are configured to follow the same standards. If you're using Cursor, see [.cursor/README.md](.cursor/README.md) for details.

## How to Contribute

### Reporting Bugs

Use GitHub Issues with the **Bug** label:
1. Use the bug report template
2. Describe the expected vs actual behavior
3. Include steps to reproduce
4. Add relevant logs or screenshots

### Suggesting Features

Use GitHub Issues with the **Feature** label:
1. Use the feature request template
2. Explain the use case and benefits
3. Consider implementation complexity
4. Discuss with maintainers before starting work

### Submitting Code

1. **Fork the repository** and create a feature branch
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow coding conventions**:
   - C# naming: PascalCase for types/methods, camelCase for locals
   - Use clear, descriptive names
   - Add XML documentation to public APIs
   - Keep methods focused and small
   - Write unit tests for new functionality
   - See [CODING_GUIDELINES.md][coding-guidelines] for SOLID, FluentResults, and MediatR usage

3. **Commit your changes**:
   ```bash
   git commit -m "feat: add new dungeon graph loader
   
   - Implemented JSON-based dungeon loading
   - Added validation for graph structure
   - Includes unit tests"
   ```

4. **Push to your fork** and submit a Pull Request

### Commit Message Convention

Follow [Conventional Commits][conventional-commits]:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Formatting, missing semicolons, etc.
- `refactor`: Code restructuring without behavior change
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples**:
```
feat(combat): add critical hit system
fix(navigation): correct edge condition evaluation
docs(api): update Lua API reference
refactor(core): simplify location graph traversal
```

## Coding Standards

See [CODING_GUIDELINES.md][coding-guidelines] for comprehensive coding standards including SOLID principles, Result pattern, and MediatR usage.

### C# Guidelines

```csharp
// ✅ Good
public class LocationGraph
{
    private readonly Dictionary<string, Location> _locations;
    
    /// <summary>
    /// Gets a location by its unique identifier.
    /// </summary>
    /// <param name="locationId">The unique location ID.</param>
    /// <returns>The location, or null if not found.</returns>
    public Location GetLocation(string locationId)
    {
        return _locations.TryGetValue(locationId, out var location) 
            ? location 
            : null;
    }
}

// ❌ Bad
public class locationgraph
{
    public Dictionary<string, Location> locations;
    
    public Location getlocation(string id)
    {
        return locations[id];  // Can throw!
    }
}
```

### GDScript Guidelines (if used)

```gdscript
# ✅ Good
extends Node
class_name LocationManager

## Manages location transitions and graph traversal.
## 
## This class handles the game's dungeon graph, tracking
## the current location and processing transitions.

var current_location: Location

func _ready() -> void:
    load_dungeon_graph()

## Moves to the specified direction if possible.
func move(direction: Direction) -> bool:
    var edge = current_location.get_available_edge(direction)
    if edge:
        transition_to(edge.destination)
        return true
    return false
```

### Lua Script Guidelines

```lua
-- ✅ Good: Clear, documented condition
-- Check if player has the golden key and hasn't used it yet
return HasItem(gameState, "golden_key") and 
       not HasFlag(gameState, "golden_key_used")

-- ❌ Bad: Unclear, no documentation
return HasItem(gameState, "gk") and not HasFlag(gameState, "gku")
```

## Project Structure

```
src/
├─ Fellhaven/
   ├─ scripts/
   │  ├─ core/              # Core game systems
   │  ├─ systems/           # Gameplay systems
   │  ├─ ui/                # UI controllers
   │  └─ utilities/         # Helper classes
   ├─ scenes/
   │  ├─ main/              # Main scenes
   │  ├─ ui/                # UI scenes
   │  └─ locations/         # Location templates
   ├─ data/
   │  ├─ dungeons/          # Dungeon graph JSON
   │  ├─ items/             # Item definitions
   │  └─ scripts/           # Lua scripts
   └─ assets/
      ├─ textures/
      ├─ audio/
      └─ fonts/
```

## Testing

### Writing Tests

```csharp
using NUnit.Framework;

[TestFixture]
public class LocationGraphTests
{
    private LocationGraph _graph;
    
    [SetUp]
    public void Setup()
    {
        _graph = new LocationGraph();
    }
    
    [Test]
    public void GetLocation_WithValidId_ReturnsLocation()
    {
        // Arrange
        var location = new Location("test_location");
        _graph.AddLocation(location);
        
        // Act
        var result = _graph.GetLocation("test_location");
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo("test_location"));
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~LocationGraphTests"
```

## Documentation

### Architecture Decision Records (ADR)

When making significant architectural decisions:

1. Create a new ADR in `docs/adr/`
2. Use template: `docs/adr/000-template.md`
3. Number sequentially: `001-decision-title.md`

### API Documentation

- Add XML documentation to all public APIs
- Update `docs/api/` when adding new systems
- Include code examples for complex APIs

## Pull Request Process

1. **Update documentation** for any changed functionality
2. **Add tests** for new features
3. **Ensure CI passes** (build, tests, linting)
4. **Request review** from maintainers
5. **Address feedback** promptly
6. **Squash commits** if requested before merging

## Questions?

- Open a GitHub Discussion for general questions
- Use GitHub Issues for bugs and features
- Check existing issues and discussions first

---

Thank you for contributing to Fellhaven! 🎮
