# Fellhaven - Coding Guidelines

[solid]: https://en.wikipedia.org/wiki/SOLID "SOLID Principles"
[fluent-results]: https://github.com/altmann/FluentResults "FluentResults Library"
[mediatr]: https://github.com/jbogard/MediatR "MediatR Library"
[result-pattern]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api#use-domain-objects-for-the-web-api "Result Pattern"
[csharp-conventions]: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions "C# Coding Conventions"
[agents]: AGENTS.md

This document outlines coding standards and architectural principles for the Fellhaven project.

## Core Principles

### SOLID Principles

We follow [SOLID][solid] design principles:

1. **Single Responsibility Principle (SRP)**
   - Each class should have one, and only one, reason to change
   - Example: `DungeonLoader` only loads dungeons, `LocalizationManager` only handles translations

2. **Open/Closed Principle (OCP)**
   - Classes should be open for extension, closed for modification
   - Use interfaces and abstract classes for extensibility
   - Example: Edge actions are extensible through Lua scripts without modifying C# code

3. **Liskov Substitution Principle (LSP)**
   - Derived classes must be substitutable for their base classes
   - Ensure proper inheritance hierarchies

4. **Interface Segregation Principle (ISP)**
   - Clients should not depend on interfaces they don't use
   - Keep interfaces small and focused

5. **Dependency Inversion Principle (DIP)**
   - Depend on abstractions, not concretions
   - Use dependency injection where appropriate

### Example: SOLID in Practice

```csharp
// ✅ Good: Single responsibility, depends on abstraction
public class DungeonValidator : IDungeonValidator
{
    private readonly ILogger _logger;
    
    public DungeonValidator(ILogger logger)
    {
        _logger = logger;
    }
    
    public Result Validate(Dungeon dungeon)
    {
        // Single responsibility: only validation
        return ValidateLocations(dungeon)
            .Bind(() => ValidateEdges(dungeon))
            .Bind(() => ValidateConnectivity(dungeon));
    }
}

// ❌ Bad: Multiple responsibilities, concrete dependencies
public class DungeonManager
{
    public void LoadAndValidateAndProcess(string path)
    {
        // Too many responsibilities!
        var json = File.ReadAllText(path);
        var dungeon = JsonSerializer.Deserialize<Dungeon>(json);
        if (dungeon.Locations.Count == 0)
            throw new Exception("Invalid!");
        // ... processing logic
    }
}
```

---

## Error Handling

### Use FluentResults Instead of Exceptions

We use [FluentResults][fluent-results] and the [Result Pattern][result-pattern] instead of throwing exceptions for expected failure scenarios.

**Install Package**:
```bash
dotnet add package FluentResults
```

### When to Use Result vs Exceptions

**Use Result&lt;T&gt;**:
- Business rule violations (door is locked, insufficient inventory)
- Validation failures (invalid dungeon structure)
- Expected failures that are part of normal flow
- Operations that can fail for legitimate reasons

**Use Exceptions**:
- Programming errors (null reference, index out of bounds)
- Truly exceptional circumstances (out of memory, disk full)
- Third-party library failures
- System-level errors

### Result Pattern Examples

```csharp
using FluentResults;

// ✅ Good: Returns Result for expected failures
public class InventoryService
{
    public Result<Item> RemoveItem(string itemId, int count)
    {
        if (!_items.ContainsKey(itemId))
            return Result.Fail<Item>($"Item '{itemId}' not found in inventory");
        
        if (_items[itemId].Count < count)
            return Result.Fail<Item>($"Insufficient quantity. Need {count}, have {_items[itemId].Count}");
        
        _items[itemId].Count -= count;
        return Result.Ok(_items[itemId]);
    }
}

// Usage with pattern matching (C# 14)
var result = inventoryService.RemoveItem("golden_key", 1);
if (result.IsSuccess)
{
    GD.Print($"Removed {result.Value.Name}");
}
else
{
    GD.PrintErr($"Failed: {result.Errors[0].Message}");
}

// Or with chaining
var outcome = inventoryService.RemoveItem("key", 1)
    .Bind(key => doorService.Unlock("throne_room", key))
    .Tap(() => GD.Print("Door unlocked!"));
```

### Complex Error Scenarios

```csharp
// Multiple validation errors
public Result ValidateDungeon(Dungeon dungeon)
{
    var result = Result.Ok();
    
    if (dungeon.Locations.Count == 0)
        result.WithError("Dungeon has no locations");
    
    if (dungeon.StartLocationId == null)
        result.WithError("No start location specified");
    
    var unreachableLocations = FindUnreachableLocations(dungeon);
    if (unreachableLocations.Any())
        result.WithError($"Unreachable locations: {string.Join(", ", unreachableLocations)}");
    
    return result;
}

// Error with metadata
public Result<Location> GetLocation(string locationId)
{
    if (!_locations.ContainsKey(locationId))
    {
        return Result.Fail<Location>(
            new Error($"Location '{locationId}' not found")
                .WithMetadata("LocationId", locationId)
                .WithMetadata("AvailableLocations", _locations.Keys)
        );
    }
    
    return Result.Ok(_locations[locationId]);
}
```

### Result in Godot Singletons

```csharp
public partial class GameState : Node
{
    public Result<bool> TryMove(Direction direction)
    {
        if (CurrentLocation == null)
            return Result.Fail<bool>("No current location set");
        
        var edgeResult = CurrentLocation.GetAvailableEdge(direction, this);
        if (edgeResult.IsFailed)
            return Result.Fail<bool>(edgeResult.Errors);
        
        var edge = edgeResult.Value;
        var transitionResult = TransitionTo(edge);
        
        return transitionResult.IsSuccess 
            ? Result.Ok(true)
            : Result.Fail<bool>(transitionResult.Errors);
    }
}
```

---

## Component Communication

### Use MediatR for Loose Coupling

We use [MediatR][mediatr] to implement the Mediator pattern for loose coupling between components.

**Install Package**:
```bash
dotnet add package MediatR
```

### Benefits

- **Decoupling**: Components don't need direct references to each other
- **Testability**: Easy to test handlers in isolation
- **CQRS**: Natural separation of commands and queries
- **Pipeline Behaviors**: Cross-cutting concerns (logging, validation)

### Request/Response Pattern

```csharp
using MediatR;

// Define request
public record GetLocationQuery(string LocationId) : IRequest<Result<Location>>;

// Define handler
public class GetLocationHandler : IRequestHandler<GetLocationQuery, Result<Location>>
{
    private readonly IDungeonRepository _repository;
    
    public GetLocationHandler(IDungeonRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Result<Location>> Handle(GetLocationQuery request, CancellationToken ct)
    {
        var location = await _repository.GetLocationAsync(request.LocationId);
        
        return location != null
            ? Result.Ok(location)
            : Result.Fail<Location>($"Location '{request.LocationId}' not found");
    }
}

// Usage
public class LocationController
{
    private readonly IMediator _mediator;
    
    public LocationController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<Result<Location>> GetLocation(string id)
    {
        return await _mediator.Send(new GetLocationQuery(id));
    }
}
```

### Notification Pattern (Events)

```csharp
// Define notification
public record LocationChangedNotification(string LocationId) : INotification;

// Define handlers (multiple can handle same notification)
public class UpdateUIHandler : INotificationHandler<LocationChangedNotification>
{
    public Task Handle(LocationChangedNotification notification, CancellationToken ct)
    {
        GD.Print($"UI: Updating display for location {notification.LocationId}");
        return Task.CompletedTask;
    }
}

public class PlayMusicHandler : INotificationHandler<LocationChangedNotification>
{
    public Task Handle(LocationChangedNotification notification, CancellationToken ct)
    {
        GD.Print($"Audio: Playing music for location {notification.LocationId}");
        return Task.CompletedTask;
    }
}

// Publish notification
await _mediator.Publish(new LocationChangedNotification("throne_room"));
```

### Command Pattern

```csharp
// Commands modify state, no return value (or Result<Unit>)
public record UnlockDoorCommand(string LocationId, string EdgeId) : IRequest<Result>;

public class UnlockDoorHandler : IRequestHandler<UnlockDoorCommand, Result>
{
    private readonly IGameState _gameState;
    
    public UnlockDoorHandler(IGameState gameState)
    {
        _gameState = gameState;
    }
    
    public async Task<Result> Handle(UnlockDoorCommand command, CancellationToken ct)
    {
        if (!_gameState.HasItem("key"))
            return Result.Fail("No key in inventory");
        
        _gameState.RemoveItem("key", 1);
        _gameState.SetFlag($"{command.EdgeId}_unlocked", true);
        
        return Result.Ok();
    }
}
```

### Pipeline Behaviors

```csharp
// Logging behavior for all requests
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        GD.Print($"Handling {typeof(TRequest).Name}");
        var response = await next();
        GD.Print($"Handled {typeof(TRequest).Name}");
        return response;
    }
}

// Validation behavior
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
        {
            // Return Result.Fail if TResponse is Result<T>
            return (TResponse)(object)Result.Fail(failures.Select(f => f.ErrorMessage));
        }
        
        return await next();
    }
}
```

---

## Integration with Godot

### Singleton Pattern (Godot Nodes)

```csharp
// Godot node singletons work with MediatR
public partial class GameStateManager : Node
{
    private static GameStateManager? _instance;
    public static GameStateManager Instance => _instance!;
    
    private IMediator _mediator = null!;
    
    public override void _Ready()
    {
        _instance = this;
        
        // Set up MediatR with dependency injection
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<IMediator>();
    }
    
    public async Task<Result<bool>> TryMovePlayer(Direction direction)
    {
        return await _mediator.Send(new MovePlayerCommand(direction));
    }
}
```

---

## C# 14 Features

Take advantage of modern C# features:

### Collection Expressions

```csharp
// ✅ Good: Collection expressions (C# 14)
List<Direction> validDirections = [Direction.North, Direction.East, Direction.South, Direction.West];

// Old way
var validDirections = new List<Direction> 
{ 
    Direction.North, Direction.East, Direction.South, Direction.West 
};
```

### Primary Constructors

```csharp
// ✅ Good: Primary constructor (C# 12+)
public class LocationService(IDungeonRepository repository, IMediator mediator)
{
    public async Task<Result<Location>> GetLocation(string id)
    {
        return await mediator.Send(new GetLocationQuery(id));
    }
}
```

### Pattern Matching

```csharp
// ✅ Good: Pattern matching with Result
var message = result switch
{
    { IsSuccess: true } => $"Success: {result.Value}",
    { IsFailed: true } when result.Errors.Count == 1 => $"Error: {result.Errors[0].Message}",
    { IsFailed: true } => $"Multiple errors: {string.Join(", ", result.Errors.Select(e => e.Message))}",
    _ => "Unknown state"
};
```

---

## Summary

- **SOLID Principles**: Foundation of our architecture
- **FluentResults**: Use Result&lt;T&gt; for expected failures, exceptions for exceptional cases
- **MediatR**: Decouple components with requests, commands, queries, and notifications
- **C# 14**: Leverage modern language features for cleaner code

See also:
- [AGENTS.md][agents] - General agent instructions and conventions
- [C# Coding Conventions][csharp-conventions] - Microsoft's official style guide
- [CONTRIBUTING.md][contributing] - How to contribute to the project
