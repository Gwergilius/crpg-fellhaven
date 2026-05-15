# Fellhaven Tests

Unit and integration tests for the Fellhaven CRPG project.

## Running Tests

### From Command Line

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test
dotnet test --filter "FullyQualifiedName~DirectionTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### From Visual Studio

1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All" or select specific tests
3. View results in Test Explorer window

## Test Structure

```
Fellhaven.Tests/
├─ Core/
│  ├─ DirectionTests.cs
│  ├─ LocationTests.cs
│  ├─ EdgeTests.cs
│  └─ GameStateTests.cs
├─ Systems/
│  ├─ LuaScriptEngineTests.cs
│  ├─ DungeonLoaderTests.cs
│  └─ LocalizationManagerTests.cs
└─ Integration/
   └─ DungeonGraphIntegrationTests.cs
```

## Writing Tests

### Example Test Class

```csharp
using NUnit.Framework;
using Fellhaven.Core;

namespace Fellhaven.Tests.Core;

[TestFixture]
public class LocationTests
{
    private Location _location;
    
    [SetUp]
    public void Setup()
    {
        _location = new Location("test_location");
    }
    
    [Test]
    public void Constructor_SetsId()
    {
        Assert.That(_location.Id, Is.EqualTo("test_location"));
    }
    
    [Test]
    public void GetAvailableEdge_WithNoEdges_ReturnsNull()
    {
        var gameState = new GameState();
        var edge = _location.GetAvailableEdge(Direction.North, gameState);
        
        Assert.That(edge, Is.Null);
    }
}
```

## Test Coverage Goals

- **Core Systems**: 90%+ coverage
- **Game Logic**: 80%+ coverage
- **UI Code**: 50%+ coverage (tested manually)

## Continuous Integration

Tests run automatically on:
- Every push to `main` or `develop`
- Every pull request
- See `.github/workflows/build.yml`

## Known Issues

- **Godot Node tests**: Cannot easily unit test Godot Node classes
  - Use integration tests or manual testing for Node-derived classes
- **Lua script tests**: Mock GameState for testing Lua integration

## Future Enhancements

- [ ] Add integration tests for full dungeon traversal
- [ ] Add performance benchmarks
- [ ] Add test fixtures for common scenarios
- [ ] Add mutation testing
