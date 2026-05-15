# Dungeon Graph Format Specification

[lua-api]: lua_api.md "Lua API Reference"
[adr-003]: ../adr/003-graph-based-dungeon.md "Graph-Based Dungeon Model"

Version: 1.0  
Date: 2026-05-14

This document describes the JSON format for defining dungeon graphs in Fellhaven.

---

## File Structure

```json
{
  "dungeon_id": "string",
  "name_key": "string",
  "description": "string",
  "locations": [ /* array of Location objects */ ],
  "edges": [ /* array of Edge objects */ ]
}
```

## Top-Level Properties

### `dungeon_id` (string, required)
Unique identifier for this dungeon. Use lowercase with underscores (e.g., `"ancient_crypt"`).

### `name_key` (string, optional)
Localization key for the dungeon name. Used in UI and save files.

### `description` (string, optional)
Human-readable description of the dungeon (not shown to players, for documentation).

### `locations` (array, required)
Array of Location objects (nodes in the graph). Must have at least one location.

### `edges` (array, required)
Array of Edge objects (connections between locations). Can be empty.

---

## Location Object

Represents a discrete area/room in the dungeon.

```json
{
  "id": "entrance",
  "name_key": "location.dungeon.entrance.name",
  "description_key": "location.dungeon.entrance.desc",
  "background_texture": "res://assets/textures/entrance.png",
  "objects": [ /* optional array of LocationObject */ ],
  "properties": { /* optional custom properties */ }
}
```

### Properties

- **`id`** (string, required): Unique identifier for this location within the dungeon
- **`name_key`** (string, required): Localization key for location name
- **`description_key`** (string, required): Localization key for location description
- **`background_texture`** (string, optional): Path to background image/texture
- **`objects`** (array, optional): Objects/entities in this location (NPCs, items, etc.)
- **`properties`** (object, optional): Custom key-value properties

### LocationObject

```json
{
  "id": "treasure_chest",
  "type": "container",
  "name_key": "object.chest.name",
  "properties": { "locked": true }
}
```

- **`id`** (string, required): Unique identifier for this object
- **`type`** (string, required): Object type (e.g., "npc", "container", "decoration")
- **`name_key`** (string, required): Localization key for object name
- **`properties`** (object, optional): Custom properties

---

## Edge Object

Represents a transition between two locations.

```json
{
  "id": "entrance_to_hall",
  "source": "entrance",
  "destination": "great_hall",
  "direction": "North",
  "priority": 1,
  "wall_texture": "res://assets/walls/stone.png",
  "door_texture": "res://assets/doors/wooden.png",
  "condition_script": "return HasItem(gameState, 'key')",
  "actions": [ /* array of Action objects */ ]
}
```

### Properties

- **`id`** (string, required): Unique identifier for this edge
- **`source`** (string, required): ID of source location (where edge starts)
- **`destination`** (string, required): ID of destination location (where edge leads)
- **`direction`** (string, required): Direction from source (see Directions below)
- **`priority`** (number, optional, default: 100): Lower = higher priority
- **`wall_texture`** (string, optional): Texture for wall in this direction
- **`door_texture`** (string, optional): Texture for door (if applicable)
- **`condition_script`** (string, optional): Lua script determining if edge is traversable
- **`actions`** (array, optional): Actions to execute when traversing this edge

### Directions

Valid direction values (case-insensitive):
- `"North"`, `"N"`
- `"NorthEast"`, `"NE"`
- `"East"`, `"E"`
- `"SouthEast"`, `"SE"`
- `"South"`, `"S"`
- `"SouthWest"`, `"SW"`
- `"West"`, `"W"`
- `"NorthWest"`, `"NW"`
- `"Up"`, `"U"` (stairs/ladder up)
- `"Down"`, `"D"` (stairs/ladder down)

### Priority

When multiple edges exist in the same direction from a location, they are evaluated in priority order (lowest first). The first edge whose condition evaluates to `true` is used.

**Example use case**: Locked door with fallback
```json
[
  {
    "id": "door_unlocked",
    "direction": "North",
    "priority": 1,
    "destination": "next_room",
    "condition_script": "return HasFlag(gameState, 'door_open')"
  },
  {
    "id": "door_locked",
    "direction": "North",
    "priority": 2,
    "destination": "current_room",
    "condition_script": "",
    "actions": [{ "action_script": "ShowMessage(gameState, 'msg.locked')" }]
  }
]
```

---

## Action Object

Actions are executed when an edge is traversed (if their conditions are met).

```json
{
  "id": "consume_key",
  "condition_script": "return HasItem(gameState, 'golden_key')",
  "action_script": "RemoveItem(gameState, 'golden_key', 1)\nSetFlag(gameState, 'used_key', true)",
  "description": "Consumes the golden key when used"
}
```

### Properties

- **`id`** (string, required): Unique identifier for this action
- **`condition_script`** (string, optional): Lua condition; action only executes if true
- **`action_script`** (string, required): Lua script to execute
- **`description`** (string, optional): Human-readable description (for documentation)

---

## Lua Scripting

### Condition Scripts

Must return a **boolean** value.

**Examples**:
```lua
-- Simple flag check
return HasFlag(gameState, "quest_complete")

-- Item check
return HasItem(gameState, "silver_key")

-- Complex condition
return GetVar(gameState, "quest_stage") >= 3 and HasClass(gameState, "Mage")
```

### Action Scripts

Can modify game state, no return value.

**Examples**:
```lua
-- Set flag
SetFlag(gameState, "door_opened", true)

-- Give item
GiveItem(gameState, "treasure", 1)

-- Multi-line action
RemoveItem(gameState, "torch", 1)
SetVar(gameState, "torches_used", GetVar(gameState, "torches_used") + 1)
ShowMessage(gameState, "msg.torch_lit")
```

See [Lua API Reference][lua-api] for all available functions.

---

## Complete Example

```json
{
  "dungeon_id": "tutorial_crypt",
  "name_key": "dungeon.tutorial_crypt.name",
  "description": "Small tutorial dungeon teaching basic mechanics",
  "locations": [
    {
      "id": "entrance",
      "name_key": "location.crypt.entrance.name",
      "description_key": "location.crypt.entrance.desc",
      "background_texture": "res://assets/crypt_entrance.png"
    },
    {
      "id": "main_hall",
      "name_key": "location.crypt.hall.name",
      "description_key": "location.crypt.hall.desc",
      "background_texture": "res://assets/crypt_hall.png",
      "objects": [
        {
          "id": "skeleton_guard",
          "type": "enemy",
          "name_key": "enemy.skeleton.name"
        }
      ]
    }
  ],
  "edges": [
    {
      "id": "entrance_to_hall",
      "source": "entrance",
      "destination": "main_hall",
      "direction": "North",
      "priority": 1,
      "condition_script": "",
      "actions": [
        {
          "id": "first_visit",
          "condition_script": "return not HasFlag(gameState, 'visited_hall')",
          "action_script": "SetFlag(gameState, 'visited_hall', true)\nShowMessage(gameState, 'msg.hall_entered')",
          "description": "Mark first visit to hall"
        }
      ]
    },
    {
      "id": "hall_to_entrance",
      "source": "main_hall",
      "destination": "entrance",
      "direction": "South",
      "priority": 1,
      "condition_script": "",
      "actions": []
    }
  ]
}
```

---

## Validation Rules

1. **All location IDs must be unique** within a dungeon
2. **All edge IDs must be unique** within a dungeon
3. **Edge source and destination must reference existing locations**
4. **Direction must be a valid direction string**
5. **Lua scripts must be syntactically valid** (validated on load)
6. **Circular references are allowed** (location can have edge to itself)
7. **Disconnected locations are allowed** but should be avoided

---

## Best Practices

### Naming Conventions

- **IDs**: Use `snake_case` (e.g., `"treasure_room"`, `"locked_door_1"`)
- **Localization keys**: Use dot notation (e.g., `"location.crypt.entrance.name"`)
- **Directions**: Use full names for clarity (e.g., `"North"` not `"N"`)

### Organization

- Group related edges together in the file
- Add comments via `description` fields
- Use consistent priority values (1, 10, 100 for tiers)
- Name edges descriptively (e.g., `"corridor_to_locked_door_unlocked"`)

### Performance

- Keep Lua scripts **simple and fast**
- Avoid complex calculations in condition scripts
- Cache frequently-used values in game state
- Prefer flags over complex variable checks

### Testing

- Test all edges are reachable
- Verify condition scripts work as intended
- Check for unreachable locations
- Test edge priorities work correctly

---

## Version History

- **1.0** (2026-05-14): Initial specification

---

**See Also**:
- [Lua API Reference][lua-api]
- [ADR-001: Graph-Based Dungeon Model][adr-001]
