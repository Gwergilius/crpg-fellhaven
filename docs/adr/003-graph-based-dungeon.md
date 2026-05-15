[adr-002]: 002-platform-strategy.md "Multi-Platform Development Strategy"
[adr-004]: 004-lua-scripting.md "Lua Scripting for Conditions and Actions"

# ADR-003: Graph-Based Dungeon Model

**Status**: Accepted

**Date**: 2026-05-14

## Status

Accepted

## Context

Traditional CRPG dungeon systems typically use one of these approaches:
1. **Grid/Tile-based**: Dungeons are 2D grids where each cell is a tile (e.g., Baldur's Gate, classic Ultima)
2. **Waypoint networks**: 3D spaces with predefined waypoints for navigation (e.g., modern action RPGs)
3. **Free-form 3D**: Full 3D movement without constraints (e.g., Skyrim)

For Fellhaven, we need a system that:
- Supports turn-based movement in distinct locations
- Allows complex conditional transitions between areas
- Enables easy authoring and modification of dungeons
- Works well with a first-person or isometric view
- Supports scripted events triggered by movement

## Decision

We will use a **directed graph model** for dungeon representation:

- **Nodes** represent discrete locations/rooms
- **Edges** represent possible transitions between locations
- Each edge has:
  - **Direction** (N, NE, E, SE, S, SW, W, NW, Up, Down)
  - **Priority** (for multiple edges in same direction)
  - **Condition script** (Lua) that determines if transition is available
  - **Action scripts** (Lua) that execute when transition is taken
  - **Visual data** (wall/door textures)

Navigation works as follows:
1. Player chooses a direction to move
2. System finds all edges from current node in that direction, ordered by priority
3. First edge whose condition evaluates to `true` is selected
4. Edge's actions are executed (if their conditions are met)
5. Player moves to destination node

## Consequences

### Positive Consequences

- **Flexibility**: Can represent any dungeon topology (linear, branching, loops, vertical)
- **Scriptability**: Lua conditions/actions enable complex behaviors without code changes
- **Authoring**: Graph structure is easily serializable (JSON) and editable
- **Abstraction**: Decouples logical structure from visual representation
- **Testing**: Graph can be validated independently of rendering
- **Reusability**: Same graph can be rendered in multiple visual styles (2D, 2.5D, isometric)

### Negative Consequences

- **Spatial reasoning**: Harder to visualize than a grid without tooling
- **Pathfinding**: Graph pathfinding more complex than grid A*
- **Learning curve**: Developers must understand graph concepts
- **Tool requirement**: Need a dedicated editor for efficient dungeon creation

### Neutral Consequences

- Graph edges can represent teleportation, one-way passages, secret doors naturally
- Movement is discrete (node-to-node) rather than continuous

## Alternatives Considered

### Alternative 1: Traditional Grid/Tilemap

**Description**: Use a 2D grid where each cell is a tile type (floor, wall, door, etc.)

**Pros**:
- Simple to implement and understand
- Natural spatial representation
- Many existing tools and libraries
- Pathfinding is straightforward (A*)

**Cons**:
- Limited to grid topology (no easy diagonal connections or teleports)
- Conditional passages require special tile types
- Less flexible for non-euclidean spaces
- Harder to script complex behaviors per-connection

**Why rejected**: Too restrictive for the types of dungeons we want to create. Handling conditional/scripted transitions would require special-case code.

### Alternative 2: Full 3D Scene Graph

**Description**: Use Godot's native 3D scene system with Area3D triggers

**Pros**:
- Leverage existing Godot tools
- Visual editing in Godot editor
- Natural 3D representation

**Cons**:
- Overkill for turn-based discrete movement
- Performance overhead of full 3D
- Harder to serialize and version control
- Less portable across platforms (esp. mobile)
- Turn-based logic would be bolted on

**Why rejected**: Too heavyweight for our needs. We want logical structure to be data-driven and independent of 3D rendering.

### Alternative 3: Scripted Movement System

**Description**: Define all movement logic purely in scripts without explicit graph structure

**Pros**:
- Maximum flexibility
- No constraints on movement

**Cons**:
- No clear data structure to query or visualize
- Very difficult to create tooling for
- Hard to validate or debug
- Maintenance nightmare as content grows
- No clear separation between data and logic

**Why rejected**: Unmaintainable at scale. Need clear data structures.

## Implementation Notes

### Core Classes

```csharp
public class Location
{
    string Id
    List<Edge> OutgoingEdges
    Dictionary<string, object> Properties
}

public class Edge
{
    Location Source
    Location Destination
    Direction Direction
    int Priority
    string ConditionScript
    List<EdgeAction> Actions
}

public class EdgeAction
{
    string ConditionScript
    string ActionScript
}
```

### File Format

Dungeons stored as JSON:

```json
{
  "locations": [ { "id": "room1", ... } ],
  "edges": [ { "source": "room1", "destination": "room2", ... } ]
}
```

### Lua API

```lua
-- Condition examples
return HasItem(gameState, "golden_key")
return GetVar(gameState, "quest_stage") >= 3

-- Action examples
SetFlag(gameState, "door_opened", true)
GiveItem(gameState, "treasure", 1)
```

## References

- Graph theory fundamentals: https://en.wikipedia.org/wiki/Graph_theory
- M.A.G.U.S. dungeon design inspiration
- Legend of Grimrock's grid-based system (similar but more constrained)

## Related Decisions

- [ADR-001][adr-001]: Engine Choice
- [ADR-002][adr-002]: Platform Strategy
- [ADR-004][adr-004]: Lua Scripting Integration
- [ADR-005][adr-005]: Localization System
