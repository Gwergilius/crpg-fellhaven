# Fellhaven Lua API Reference

[adr-004]: ../adr/004-condition-and-combat.md "Condition Vocabulary and Combat System"
[adr-005]: ../adr/005-lua-scripting.md "Lua Scripting for Conditions and Actions"
[graph-format]: graph_format.md "Dungeon Graph Format Specification"

Version: 0.1.0-alpha

This document describes the Lua API available for writing conditions and actions in dungeon graph scripts.

---

## Overview

Lua scripts in Fellhaven are used for:
- **Condition scripts**: Return boolean value, determine if edge is traversable
- **Action scripts**: Modify game state, no return value

All scripts have access to a `gameState` object containing current game state.

---

## Built-in Functions

### Flag Management

Flags are boolean values stored in game state (typically for quest progress, door states, etc.)

#### `HasFlag(gameState, flagName)`

Check if a flag is set to true.

**Parameters**:
- `gameState` (GameState): Current game state
- `flagName` (string): Name of the flag to check

**Returns**: `boolean` - true if flag exists and is true, false otherwise

**Example**:
```lua
-- Check if player opened the secret door
return HasFlag(gameState, "secret_door_opened")
```

#### `SetFlag(gameState, flagName, value)`

Set a flag to true or false.

**Parameters**:
- `gameState` (GameState): Current game state
- `flagName` (string): Name of the flag to set
- `value` (boolean): Value to set (true or false)

**Returns**: Nothing

**Example**:
```lua
-- Mark the dragon as defeated
SetFlag(gameState, "dragon_defeated", true)
```

---

### Variable Management

Variables store integer values (for counters, quest stages, etc.)

#### `GetVar(gameState, varName)`

Get the value of a variable.

**Parameters**:
- `gameState` (GameState): Current game state
- `varName` (string): Name of the variable

**Returns**: `number` - Variable value, or 0 if not set

**Example**:
```lua
-- Get current quest stage
local questStage = GetVar(gameState, "main_quest_stage")
return questStage >= 3
```

#### `SetVar(gameState, varName, value)`

Set a variable to a specific integer value.

**Parameters**:
- `gameState` (GameState): Current game state
- `varName` (string): Name of the variable
- `value` (number): Integer value to set

**Returns**: Nothing

**Example**:
```lua
-- Advance quest to stage 2
SetVar(gameState, "main_quest_stage", 2)
```

#### `AddVar(gameState, varName, amount)`

Add to a variable's value (useful for counters).

**Parameters**:
- `gameState` (GameState): Current game state
- `varName` (string): Name of the variable
- `amount` (number): Amount to add (can be negative to subtract)

**Returns**: Nothing

**Example**:
```lua
-- Increment kill counter
AddVar(gameState, "goblins_killed", 1)
```

---

### Inventory Management

#### `HasItem(gameState, itemId)`

Check if player has at least one of an item.

**Parameters**:
- `gameState` (GameState): Current game state
- `itemId` (string): Unique item identifier

**Returns**: `boolean` - true if player has item, false otherwise

**Example**:
```lua
-- Check if player has the golden key
return HasItem(gameState, "golden_key")
```

#### `HasItems(gameState, itemId, count)`

Check if player has at least a specific quantity of an item.

**Parameters**:
- `gameState` (GameState): Current game state
- `itemId` (string): Unique item identifier
- `count` (number): Minimum quantity required

**Returns**: `boolean` - true if player has enough items, false otherwise

**Example**:
```lua
-- Check if player has 3 health potions
return HasItems(gameState, "health_potion", 3)
```

#### `GetItemCount(gameState, itemId)`

Get the exact count of an item in inventory.

**Parameters**:
- `gameState` (GameState): Current game state
- `itemId` (string): Unique item identifier

**Returns**: `number` - Item count, or 0 if not in inventory

**Example**:
```lua
-- Check how many coins player has
local coinCount = GetItemCount(gameState, "gold_coin")
return coinCount >= 100
```

#### `GiveItem(gameState, itemId, count)`

Add items to player's inventory.

**Parameters**:
- `gameState` (GameState): Current game state
- `itemId` (string): Unique item identifier
- `count` (number): Quantity to add (default: 1)

**Returns**: Nothing

**Example**:
```lua
-- Give player a silver key
GiveItem(gameState, "silver_key", 1)
```

#### `RemoveItem(gameState, itemId, count)`

Remove items from player's inventory.

**Parameters**:
- `gameState` (GameState): Current game state
- `itemId` (string): Unique item identifier
- `count` (number): Quantity to remove (default: 1)

**Returns**: Nothing

**Example**:
```lua
-- Consume a key to open door
RemoveItem(gameState, "golden_key", 1)
SetFlag(gameState, "throne_room_unlocked", true)
```

---

### Party & Character

#### `HasClass(gameState, className)`

Check if any party member has a specific class.

**Parameters**:
- `gameState` (GameState): Current game state
- `className` (string): Class name (e.g., "Rogue", "Mage")

**Returns**: `boolean` - true if any party member has the class

**Example**:
```lua
-- Check if party has a rogue (can pick locks)
return HasClass(gameState, "Rogue")
```

#### `HasSkill(gameState, skillName, minLevel)`

Check if any party member has a skill at minimum level.

**Parameters**:
- `gameState` (GameState): Current game state
- `skillName` (string): Skill name (e.g., "Lockpicking", "Diplomacy")
- `minLevel` (number): Minimum skill level required (optional, default: 1)

**Returns**: `boolean` - true if requirement met

**Example**:
```lua
-- Check if party can pick advanced locks
return HasSkill(gameState, "Lockpicking", 3)
```

#### `GetPartyLevel(gameState)`

Get the average level of the party.

**Parameters**:
- `gameState` (GameState): Current game state

**Returns**: `number` - Average party level

**Example**:
```lua
-- Only allow high-level parties through
return GetPartyLevel(gameState) >= 5
```

---

### UI & Messaging

#### `ShowMessage(gameState, messageKey)`

Display a localized message to the player.

**Parameters**:
- `gameState` (GameState): Current game state
- `messageKey` (string): Localization key for message

**Returns**: Nothing

**Example**:
```lua
-- Show "door is locked" message
if not HasItem(gameState, "golden_key") then
    ShowMessage(gameState, "msg.door_locked")
end
```

#### `ShowDialog(gameState, dialogId)`

Trigger a dialog sequence.

**Parameters**:
- `gameState` (GameState): Current game state
- `dialogId` (string): Dialog sequence identifier

**Returns**: Nothing

**Example**:
```lua
-- Start conversation with guard
ShowDialog(gameState, "dialog.guard_checkpoint")
```

---

### Combat & Encounter

#### `StartCombat(gameState, encounterId)`

Initiate a combat encounter.

**Parameters**:
- `gameState` (GameState): Current game state
- `encounterId` (string): Encounter definition ID

**Returns**: Nothing

**Example**:
```lua
-- Trigger goblin ambush
if not HasFlag(gameState, "goblins_defeated") then
    StartCombat(gameState, "encounter.goblin_ambush")
end
```

#### `IsQuestComplete(gameState, questId)`

Check if a quest is completed.

**Parameters**:
- `gameState` (GameState): Current game state
- `questId` (string): Quest identifier

**Returns**: `boolean` - true if quest completed

**Example**:
```lua
-- Only allow passage if quest is done
return IsQuestComplete(gameState, "quest.rescue_villagers")
```

---

## Common Patterns

### Locked Door (requires key)

**Condition**:
```lua
return HasItem(gameState, "dungeon_key")
```

**Action**:
```lua
RemoveItem(gameState, "dungeon_key", 1)
SetFlag(gameState, "dungeon_door_open", true)
ShowMessage(gameState, "msg.door_unlocked")
```

### One-Way Passage

**Condition**:
```lua
return not HasFlag(gameState, "trapdoor_fallen")
```

**Action**:
```lua
SetFlag(gameState, "trapdoor_fallen", true)
ShowMessage(gameState, "msg.trapdoor_falls")
```

### Quest Stage Check

**Condition**:
```lua
local stage = GetVar(gameState, "main_quest_stage")
return stage >= 3 and stage < 5
```

### Skill Check (e.g., Lockpicking)

**Condition**:
```lua
-- Either have the key OR can pick the lock
return HasItem(gameState, "master_key") or HasSkill(gameState, "Lockpicking", 5)
```

**Action** (if picked):
```lua
if not HasItem(gameState, "master_key") then
    ShowMessage(gameState, "msg.lock_picked")
end
SetFlag(gameState, "vault_unlocked", true)
```

### Counter-Based Event

**Condition**:
```lua
-- Trigger after killing 10 goblins
return GetVar(gameState, "goblins_killed") >= 10
```

**Action**:
```lua
ShowMessage(gameState, "msg.goblin_chief_appears")
StartCombat(gameState, "encounter.goblin_chief")
SetFlag(gameState, "chief_encountered", true)
```

### Complex Multi-Condition

**Condition**:
```lua
-- Require: completed quest, has item, and correct class
local hasQuest = IsQuestComplete(gameState, "quest.join_guild")
local hasToken = HasItem(gameState, "guild_token")
local hasClass = HasClass(gameState, "Mage")

return hasQuest and hasToken and hasClass
```

---

## Debugging Tips

### Print Variable Values

```lua
-- Debug: print quest stage
local stage = GetVar(gameState, "main_quest_stage")
print("Quest stage: " .. stage)
return stage >= 3
```

### Test Multiple Conditions

```lua
-- Break down complex condition for debugging
local hasKey = HasItem(gameState, "golden_key")
local questDone = IsQuestComplete(gameState, "quest.find_key")

print("Has key: " .. tostring(hasKey))
print("Quest done: " .. tostring(questDone))

return hasKey or questDone
```

---

## Error Handling

If a Lua script throws an error:
- **Conditions**: Return `false` (deny passage)
- **Actions**: Silently fail (no action taken)
- Error logged to console

**Best Practices**:
- Keep scripts simple
- Test with debug prints
- Use clear variable names
- Add comments for complex logic

---

## Version History

- **0.1.0**: Initial API (2026-05-14)

---

**See Also**:
- [ADR-002: Lua Scripting][adr-002]
- [Dungeon Graph Format][graph-format]
