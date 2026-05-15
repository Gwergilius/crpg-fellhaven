[adr-001]: 001-engine-choice.md "Engine Choice"
[adr-002]: 002-platform-strategy.md "Multi-Platform Development Strategy"
[adr-004]: 004-lua-scripting.md "Lua Scripting for Conditions and Actions"
[adr-005]: 005-localization.md "Internationalization Strategy"

# ADR-003: World Data Model — Directed Graph / Finite State Machine

**Status**: Accepted

**Date**: 2026-05-15

## Table of Contents

- [Context](#context)
- [Decision](#decision)
  - [Core concepts](#core-concepts)
  - [Composite scene assembly](#composite-scene-assembly)
  - [Relationship to the FSM model](#relationship-to-the-fsm-model)
  - [Partitioning and the grid](#partitioning-and-the-grid)
- [Rationale](#rationale)
- [Schemas](#schemas)
- [Consequences](#consequences)
- [Save System](#save-system)
- [Alternatives Considered](#alternatives-considered)
- [Related ADRs](#related-adrs)

## Context

Traditional CRPG dungeon systems typically use one of these approaches:
1. **Grid/Tile-based**: Dungeons are 2D grids where each cell is a tile (e.g., Baldur's Gate, classic Ultima)
2. **Waypoint networks**: 3D spaces with predefined waypoints for navigation (e.g., modern action RPGs)
3. **Free-form 3D**: Full 3D movement without constraints (e.g., Skyrim)

The world is not fundamentally a grid. The grid is merely a spatial encoding convenience.
What the world actually is:

- A collection of **nodes** (locations) that the player can be in
- A collection of **transitions** (edges) between those nodes, each of which may be
  traversable or not depending on game state
- A set of **terminal states**: the game starts somewhere, ends somewhere (victory or
  death), and some states are unreachable unless certain conditions are met

This is the definition of a **directed graph**, and when the terminal and conditional
states are included, it becomes a **finite state machine (FSM)**.

For Fellhaven, we need a system that:
- Supports turn-based movement in distinct locations
- Allows complex conditional transitions between areas
- Enables easy authoring and modification of dungeons
- Works well with a first-person or isometric view
- Supports scripted events triggered by movement
- Models non-spatial game states (death, victory, character creation)
- Enables static analysis (reachability, softlock detection)

## Decision

Model the game world as a **directed graph of nodes and edges**, partitioned into
**regions** for rendering and data-management purposes.

### Core concepts

#### Node

A node represents a discrete location the player can occupy. In the common case this
is a dungeon cell, but the model is not limited to spatial cells.

| Property | Type | Description |
|---|---|---|
| `id` | `string` | Globally unique identifier, e.g. `"dungeon_entrance:3:5"` |
| `region` | `string` | Region this node belongs to |
| `x`, `y` | `int` | Grid coordinates within the region (spatial nodes only) |
| `type` | `NodeType` | `cell` \| `terminal_start` \| `terminal_death` \| `terminal_victory` \| `meta` |
| `visual` | `NodeVisual?` | Texture set, ceiling/floor overrides (spatial nodes only) |
| `flags` | `NodeFlags` | `nonMagic`, `dark`, `dangerous`, `noRecall`, `outdoor` |
| `on_enter` | `ScriptRef?` | Script executed when the player enters this node |
| `items` | `ItemSpawn[]` | Items that may be found here (initial spawns) |
| `encounters` | `EncounterRef?` | Random encounter table for this node |

#### Edge (Transition)

An edge represents a possible transition from one node to another. In the spatial case
an edge is a step through a wall face (N/E/S/W), but edges may also represent non-spatial
transitions such as stairs, portals, or game-state transitions (death, victory).

| Property | Type | Description |
|---|---|---|
| `id` | `string` | Unique within the source node, e.g. `"dungeon_entrance:3:5:north"` |
| `from` | `NodeId` | Source node |
| `to` | `NodeId` | Destination node. **For solid walls, `to == from` (self-loop)** |
| `direction` | `Direction?` | `north` \| `east` \| `south` \| `west` \| `up` \| `down` \| `null` (non-spatial) |
| `state` | `EdgeState` | `open` \| `closed` \| `locked` \| `secret` \| `one_way` \| `solid` |
| `wall_visuals` | `object?` | State-keyed texture map for this wall face |
| `condition` | `Condition?` | Gate check — blocks traversal entirely if it fails |
| `on_traverse` | `TraverseEntry[]?` | Ordered list of (condition, action) pairs; first match fires |
| `on_interact` | `TraverseEntry[]?` | Fired when player interacts with wall face without crossing |
| `flags` | `EdgeFlags` | `one_way`, `no_spell_pass`, etc. |

The **condition** and **on_traverse** are separate concerns:
- `condition` is a **gate**: evaluated first. If it fails, traversal is blocked.
- `on_traverse` is a **conditional action chain**: ordered list of `{ condition, action }` entries. First matching entry fires.

Navigation works as follows:
1. Player chooses a direction to move
2. System finds all edges from current node in that direction
3. For each edge (in definition order):
   - Evaluate edge's `condition` gate — if false, skip to next edge
   - If condition passes, execute first matching `on_traverse` entry
   - Move player to `to` node
4. If no edge condition passes, movement fails

#### Region

A region is a named grouping of nodes for rendering and data management purposes.

| Property | Type | Description |
|---|---|---|
| `id` | `string` | Unique identifier, e.g. `"sorpigal_town"` |
| `name_key` | `string` | i18n key for the display name ([ADR-005][adr-005]) |
| `type` | `RegionType` | `town` \| `dungeon` \| `outdoor` \| `special` |
| `width`, `height` | `int` | Grid dimensions (typically ≤ 16×16) |
| `texture_set` | `string` | Default wall/floor/ceiling texture set |
| `default_wall_type` | `WallType` | Wall type used when direction has no explicit edge |
| `ambient_light` | `bool` | Whether ambient light is present (false = torch required) |
| `default_encounter_table` | `string?` | Fallback encounter table if node has none |
| `music_track` | `string?` | Background music asset identifier |

#### NPC entity

An NPC is an independent entity whose current location is part of its own state,
not part of any node. This is the **Entity–Location separation** principle.

| Property | Type | Description |
|---|---|---|
| `id` | `string` | Globally unique identifier, e.g. `"innkeeper_sorpigal"` |
| `home_node_id` | `NodeId` | Default location (initial state) |
| `schedule` | `ScheduleEntry[]?` | Time-of-day location overrides (optional) |
| `on_tick` | `ScriptRef?` | Lua script controlling movement/behavior each tick (optional) |
| `on_interact` | `ScriptRef?` | Lua script executed when player interacts with NPC |
| `flags` | `NpcFlags` | `hostile`, `essential`, `unique`, etc. |

NPC location is resolved at runtime using a **priority override stack**:

```
if on_tick script exists  → script decides (full control)
else if schedule exists   → look up current time_of_day in schedule table
else                      → home_node_id (static default)
```

#### Special nodes and the `__game__` meta-region

The three reserved terminal nodes live in a dedicated **meta-region** called
`__game__`. This region has no spatial grid and is never rendered.

| Node id | Type | Description |
|---|---|---|
| `__start__` | `terminal_start` | Where a new party enters the world |
| `__death__` | `terminal_death` | The party has been killed |
| `__victory__` | `terminal_victory` | The game has been won |

These are regular nodes; they differ only in `type`. The game engine applies
platform-specific behaviour when the player reaches a terminal node.

### Composite scene assembly

The game never renders a node directly. Instead, the engine assembles a **composite
scene** on each player move:

```
CompositeScene = f(
    current_node,          -- topology, visual, flags
    outgoing_edges,        -- traversable directions, wall visuals
    party,                 -- characters, HP/SP, inventory
    npcs_at_node,          -- all NPCs whose resolved location == current_node
    time_of_day,           -- affects NPC schedules, lighting, ambient sound
    active_flags           -- open doors, defeated bosses, etc.
)
```

The composite scene is ephemeral — it is assembled on demand and never persisted.
Only its inputs (node graph, NPC states, party state, flags, game clock) are saved.

### Relationship to the FSM model

The directed graph becomes a **finite state machine** when the following rule is applied:

> The player's position in the world is always exactly one node. Moving is transitioning
> along an edge. Every edge transition may be gated by a condition. Some nodes are terminal.

This framing makes several previously implicit behaviours explicit and verifiable:
- **Reachability analysis**: can a node be reached from `__start__`?
- **Softlock detection**: does any reachable non-terminal node have zero traversable outgoing edges?
- **Completion path verification**: is `__victory__` reachable at all?
- **Flag dependency graph**: which flags must be set to open a given path?

### Partitioning and the grid

The grid is a **rendering and locality constraint**, not a semantic one. Spatial nodes
carry `(x, y)` coordinates within their region for rendering purposes. Non-spatial nodes
(terminal states, meta nodes) have no coordinates.

An edge may connect nodes in **different regions** (e.g., a staircase leads from
`dungeon_b1:8:0` to `town_entrance:5:7`). There is no requirement that edges stay within a region.

## Rationale

### Why directed graph?

The grid is a special case of a directed graph where every node has up to four edges
aligned to cardinal directions. Modelling it as a graph does not lose any information
from the grid model; it only makes implicit structure explicit.

The graph model is more general: it naturally handles non-spatial transitions (death,
portals, cutscene triggers) without special-casing them.

### Why FSM framing?

Terminal and start states are already present in RPGs; making them explicit nodes enables:
- Static analysis of the game graph (CI tool: detect softlocks before shipping)
- Clear engine contract: the game loop is "move along edges until terminal node"
- Cleaner separation of game logic from engine rendering logic

#### The hardcoded start problem

In many games, the player's starting position is **hardcoded in the engine**. Changing where
the party spawns requires modifying and recompiling the engine itself.

The `__start__` abstract node solves this by making the starting point **data-driven**.
The engine only knows: *"find the `__start__` node and follow its outgoing edge."*
The actual destination is declared in the world data, not in engine code.

### Why keep a grid convention?

Using regions of limited size (e.g., 16×16):
- Gives a **natural answer to view distance**: everything in the current region that is
  not occluded by a wall is visible. No fog-of-distance formula.
- Keeps the renderer simple: it only renders the current region
- Limits memory footprint: only the current region's nodes need to be fully loaded

The constraint is a convention, not a hard limit. Content can use regions of any size.

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

The constraint is a convention, not a hard limit. Content can use regions of any size.

## Schemas

### Solid wall convention

> **Absent edge = solid self-loop using the region's `default_wall_type`, no behaviour.**
> The renderer falls back to the region's `default_wall_type` (typically `wall_0`) for
> any direction that has no explicit edge. This covers the vast majority of walls.

Any direction that deviates from the default **must** have an explicit edge.

#### Solid walls as self-loops

A solid wall face is modelled as an edge where **`to == from`**: the edge starts and
ends at the same node. The `state` field controls the player experience:

| `state` on self-loop | Player experience |
|---|---|---|
| `solid` | Permanent structural wall. Renderer draws texture. No interaction. |
| `closed` | Wall that gives feedback. Shows `fail_message_key` ("You hit a solid wall."). |
| `open` | Silent no-op. Player stays, no error message. Useful for invisible triggers. |

### JSON source schema

One JSON file per region, stored under `assets/data/dungeons/<region_id>.json`.

```json
{
  "id": "sorpigal_town",
  "name_key": "region.sorpigal.name",
  "type": "town",
  "width": 16,
  "height": 16,
  "texture_set": "town_stone",
  "ambient_light": true,
  "default_encounter_table": null,
  "music_track": "town_theme",

  "nodes": [
    {
      "id": "sorpigal:8:3",
      "x": 8,
      "y": 3,
      "type": "cell",
      "flags": {},
      "on_enter": null,
      "items": [],
      "encounters": null,
      "edges": [
        {
          "id": "sorpigal:8:3:north",
          "to": "sorpigal:8:2",
          "direction": "north",
          "state": "open",
          "wall_visuals": { "default": "none" },
          "condition": null,
          "on_traverse": null,
          "flags": {}
        }
      ]
    }
  ],

  "npcs": [
    {
      "id": "innkeeper_sorpigal",
      "name_key": "npc.innkeeper_sorpigal.name",
      "home_node_id": "sorpigal:8:3",
      "schedule": null,
      "on_tick": null,
      "on_interact": { "type": "script", "package": "npcs/innkeeper" },
      "flags": { "essential": true }
    }
  ]
}
```

### Compiled SQLite schema

```sql
-- Regions
CREATE TABLE regions (
    id                      TEXT PRIMARY KEY,
    name_key                TEXT NOT NULL,
    type                    TEXT NOT NULL,
    width                   INTEGER NOT NULL,
    height                  INTEGER NOT NULL,
    texture_set             TEXT NOT NULL,
    ambient_light           INTEGER NOT NULL,
    default_encounter_table TEXT,
    music_track             TEXT
);

-- Nodes
CREATE TABLE nodes (
    id              TEXT PRIMARY KEY,
    region_id       TEXT NOT NULL REFERENCES regions(id),
    x               INTEGER,
    y               INTEGER,
    type            TEXT NOT NULL,
    flags           TEXT NOT NULL DEFAULT '{}',
    on_enter        TEXT,
    encounter_table TEXT
);

-- Edges
CREATE TABLE edges (
    id              TEXT PRIMARY KEY,
    from_node       TEXT NOT NULL REFERENCES nodes(id),
    to_node         TEXT NOT NULL REFERENCES nodes(id),
    direction       TEXT,
    state           TEXT NOT NULL,
    wall_visuals    TEXT NOT NULL DEFAULT '{"default":"wall_0"}',
    condition       TEXT,
    on_traverse     TEXT,
    on_interact     TEXT,
    flags           TEXT NOT NULL DEFAULT '{}'
);

-- NPCs
CREATE TABLE npcs (
    id              TEXT PRIMARY KEY,
    name_key        TEXT NOT NULL,
    home_node_id    TEXT NOT NULL REFERENCES nodes(id),
    on_tick         TEXT,
    on_interact     TEXT,
    flags           TEXT NOT NULL DEFAULT '{}'
);

CREATE TABLE npc_schedules (
    npc_id          TEXT NOT NULL REFERENCES npcs(id),
    time_of_day     TEXT NOT NULL,
    node_id         TEXT REFERENCES nodes(id),
    PRIMARY KEY (npc_id, time_of_day)
);
```

## Consequences

### Positive

- Event triggers on cells map naturally to `on_enter` scripts on nodes
- Event triggers on wall faces map naturally to `on_traverse` scripts on edges
- **All teleportation becomes ordinary edges** — any transition is an edge with a `to` node id
- Cross-region edges (stairs, portals) are structurally identical to same-region edges
- Static analysis tools can verify world integrity (reachability, softlocks)
- Modding is cleaner: add/remove nodes and edges, not cell arrays
- Non-spatial game states (death, victory) integrate into the same model
- **Flexibility**: Can represent any topology (linear, branching, loops, vertical)
- **Scriptability**: Lua conditions/actions enable complex behaviors without code changes
- **Authoring**: Graph structure is easily serializable (JSON) and editable
- **Abstraction**: Decouples logical structure from visual representation
- **Testing**: Graph can be validated independently of rendering

### Negative

- More verbose source files than a flat cell grid: each edge is an explicit object
- **Spatial reasoning**: Harder to visualize than a grid without tooling
- **Pathfinding**: Graph pathfinding more complex than grid A*
- **Learning curve**: Developers must understand graph concepts
- **Tool requirement**: Need a dedicated editor for efficient dungeon creation
- Tooling for static graph analysis must be built

### Neutral

- Graph edges can represent teleportation, one-way passages, secret doors naturally
- Movement is discrete (node-to-node) rather than continuous

## Save System

### Static vs. mutable data

Every property in `world.db` is classified as either **static** (never changes) or
**mutable** (may be changed by scripts or player actions).

| Category | Classification |
|---|---|
| Node topology, visual, flags | Static |
| Edge topology, wall visuals | Static |
| Edge state | **Mutable** |
| Global and map-local flags | **Mutable** |
| Item spawns (presence) | **Mutable** |
| NPC state | **Mutable** |
| Party | **Mutable** |

### Save file structure (`save.db`)

A save file is a **delta snapshot**: it stores only values that differ from the defaults
defined in `world.db`, plus the full party state.

```
save.db
  meta              — save_slot, timestamp, playtime, game_version
  party             — full party snapshot (characters, inventory, gold, HP/SP)
  position          — current_node_id, facing_direction
  edge_overrides    — (edge_id, current_state)  where state ≠ initial_state
  flag_overrides    — (flag_id, current_value)  where value ≠ initial_value
  consumed_spawns   — (node_id, item_id)         items already picked up
  visited_nodes     — (node_id)                  for minimap reveal
  npc_states        — (npc_id, current_node_id, alive, flags_json)
```

Only NPCs whose state differs from their `world.db` defaults are written to `npc_states`.

## Implementation Details

### Flags Representation

The `flags` field on Node, Edge, and NPC entities uses a specialized data structure:

**Type**: `Dictionary<string, bool>` with special semantics:
- **Getter**: Returns `false` for missing keys (default value)
- **Setter**: Deletes keys when set to `false` (sparse storage)
- **JSON serialization**: Includes only flags set to `true`

**Rationale**:
- Most flags are `false` most of the time — storing only `true` values saves space
- The sparse representation makes JSON diffs cleaner (no noise from unchanged flags)
- Getters defaulting to `false` means code can check flags without null checks

**Example**:
```json
// This JSON:
{ "flags": { "nonMagic": true } }

// Represents:
{
  "nonMagic": true,
  "dark": false,
  "dangerous": false,
  "noRecall": false,
  "outdoor": false
}

// Empty flags object means all flags are false:
{ "flags": {} }
```

**Implementation note**: The underlying storage is effectively a `HashSet<string>` of flag names,
not a dictionary of bool values. The `Dictionary<string, bool>` type signature is a public API
convenience that maps cleanly to JSON objects.

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
- Does not model non-spatial states

**Why rejected**: Too restrictive. Handling conditional/scripted transitions would require special-case code. Grid is a special case of a graph — modelling as graph is strictly more expressive.

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

**Why rejected**: Too heavyweight. We want logical structure to be data-driven and independent of 3D rendering.

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

### Alternative 4: Keep the flat cell grid

Simple and directly mirrors traditional formats. Does not model non-spatial states, makes cross-map transitions a special case, and prevents static reachability analysis. Rejected because the graph model is strictly more expressive.

### Alternative 5: Full FSM without grid constraint

Model every possible game state as a node, with no region/grid structure at all. Theoretically elegant but impractical: the renderer needs a spatial grid. Rejected in favour of the hybrid approach (spatial nodes carry coordinates; non-spatial nodes do not).

## References

- Graph theory fundamentals: https://en.wikipedia.org/wiki/Graph_theory
- M.A.G.U.S. dungeon design inspiration
- Legend of Grimrock's grid-based system (similar but more constrained)
- Inspired by MM1-Remaster ADR-010 World Data Model

## Related ADRs

- [ADR-001][adr-001]: Engine Choice
- [ADR-002][adr-002]: Platform Strategy
- [ADR-004][adr-004]: Lua Scripting Integration (conditions/actions on edges)
- [ADR-005][adr-005]: Localization System (name_key fields)

---

**Owner**: Fellhaven Team  
**Last Updated**: 2026-05-15
