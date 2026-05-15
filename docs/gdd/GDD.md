# Fellhaven - Game Design Document

[adr-001]: ../adr/001-engine-choice.md
[adr-002]: ../adr/002-platform-strategy.md
[adr-003]: ../adr/003-graph-based-dungeon.md
[adr-004]: ../adr/004-condition-and-combat.md
[adr-005]: ../adr/005-lua-scripting.md
[adr-006]: ../adr/006-localization.md
[adr-index]: ../adr/

**Version**: 0.1.0-alpha  
**Last Updated**: 2026-05-14  
**Status**: Early Development

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Game Overview](#game-overview)
3. [Core Gameplay](#core-gameplay)
4. [Game Systems](#game-systems)
5. [Content](#content)
6. [Technical Design](#technical-design)
7. [Art and Audio](#art-and-audio)
8. [User Interface](#user-interface)
9. [Development Roadmap](#development-roadmap)

---

## 1. Executive Summary

### Vision Statement

Fellhaven is a turn-based fantasy CRPG inspired by classic tabletop role-playing games, specifically M.A.G.U.S. The game combines tactical turn-based combat with dungeon exploration and character progression.

### Target Audience

- Fans of classic CRPGs (Baldur's Gate, Pillars of Eternity)
- Tabletop RPG enthusiasts
- Turn-based strategy fans
- Solo players looking for deep, tactical gameplay

### Core Pillars

1. **Tactical Depth**: Every decision matters in combat and exploration
2. **Character Progression**: Meaningful choices that shape your character
3. **Exploration**: Discover secrets and solve environmental puzzles
4. **Narrative**: Engaging stories set in a rich fantasy world

### Platform & Scope

- **Platforms**: Windows (primary), Android, iOS
- **Scope**: Single-player, 10-20 hours for first complete story
- **Development**: Solo hobby project, iterative development

---

## 2. Game Overview

### Genre

Turn-Based Fantasy CRPG

### Setting

*[To be developed - fantasy world inspired by M.A.G.U.S.]*

The world of Fellhaven is a dark fantasy setting where ancient magic mingles with medieval technology. Players explore dungeons, ruins, and wilderness areas filled with monsters, traps, and treasure.

### Core Loop

```
Explore → Encounter → Combat/Puzzle → Loot → Character Growth → Explore
```

1. **Explore** dungeons and locations
2. **Encounter** enemies, NPCs, or obstacles
3. **Combat** (tactical turn-based) or solve puzzles
4. **Loot** treasure, items, and equipment
5. **Character Growth** through experience and equipment
6. **Return to step 1** with new capabilities

---

## 3. Core Gameplay

### Exploration

**Movement System**:
- Graph-based dungeon navigation (see [ADR-001][adr-001])
- Discrete locations connected by edges
- Directional movement (N, NE, E, SE, S, SW, W, NW, Up, Down)
- Conditional transitions (locked doors, hidden passages)

**Interactions**:
- Examine objects and environmental features
- Talk to NPCs
- Search for hidden items
- Trigger environmental events

**View Options** (TBD - prototype will test):
- 2D top-down
- 2.5D first-person (Dungeon Master style)
- Isometric 3/4 view (⭐ preferred)

### Combat System

**Turn-Based Tactical**:
- Initiative order determines turn sequence
- Each character gets one action per turn
- Movement and action economy

**Action Types**:
- **Attack**: Basic weapon attack
- **Defend**: Increase defense until next turn
- **Skill**: Use character ability
- **Item**: Consume item from inventory
- **Flee**: Attempt to escape combat

**Combat Math** (preliminary):
```
Attack Roll = d20 + AttackSkill + Modifiers
Defense Roll = d20 + DefenseSkill + Modifiers

If Attack > Defense:
    Damage = WeaponDamage + StrengthBonus - ArmorReduction
```

### Character System

**Attributes** (inspired by M.A.G.U.S.):
- **Strength** (STR): Physical power, melee damage
- **Dexterity** (DEX): Agility, ranged accuracy, dodge
- **Constitution** (CON): Health, stamina, resistance
- **Intelligence** (INT): Magic power, skill learning
- **Wisdom** (WIS): Perception, willpower, magic resistance
- **Charisma** (CHA): Social interactions, leadership

**Classes** (initial set):
1. **Warrior**: High HP, melee combat specialist
2. **Rogue**: Stealth, traps, critical hits
3. **Mage**: Spell casting, low HP, high damage
4. **Cleric**: Healing, buffs, moderate combat

**Progression**:
- Experience Points (XP) from combat and quests
- Level-up grants: stat increases, new skills, HP/MP
- Equipment: weapons, armor, accessories

---

## 4. Game Systems

### Inventory System

**Structure**:
- Limited inventory slots (e.g., 20-30 items)
- Item categories: Weapons, Armor, Consumables, Quest Items, Miscellaneous
- Equipment slots: Weapon, Armor, Accessory 1, Accessory 2

**Items**:
- **Weapons**: Swords, axes, bows, staves (affect attack and damage)
- **Armor**: Light, medium, heavy (affect defense and dodge)
- **Consumables**: Potions, scrolls, food
- **Quest Items**: Key items for story progression

### Magic System

**Mana Points (MP)**:
- Each spell costs MP to cast
- MP regenerates through rest or consumables

**Spell Schools** (preliminary):
- **Elemental**: Fire, ice, lightning damage
- **Healing**: Restore HP, cure ailments
- **Support**: Buffs, debuffs, utility
- **Summoning**: Conjure allies or effects

### Quest System

*[To be developed in detail]*

**Types**:
- **Main Quest**: Primary story arc
- **Side Quests**: Optional content
- **Exploration**: Discover locations

### Save System

- Manual save at any time (non-combat)
- Autosave at key points
- Multiple save slots

---

## 5. Content

### Story Outline

*[Placeholder - to be developed]*

**Act 1**: Tutorial and introduction to Fellhaven
**Act 2**: Main conflict emerges
**Act 3**: Climax and resolution

### Locations

**Prototype Dungeon**:
- Small 5-10 room dungeon for testing
- Demonstrates all core mechanics
- Includes combat, puzzles, loot

**Planned Locations** (future):
- Village/Town (safe area, vendors, NPCs)
- Forest Ruins
- Underground Crypt
- Mountain Fortress

### Characters & NPCs

*[To be developed]*

### Bestiary

**Prototype Enemies**:
- **Goblin**: Low HP, weak attack
- **Skeleton**: Medium HP, medium attack
- **Orc**: High HP, strong attack

**Enemy Traits**:
- Unique behaviors (aggressive, defensive, caster)
- Loot tables
- XP rewards

---

## 6. Technical Design

### Architecture

**Engine**: Godot 4.6.2  
**Language**: 
  - Preferred: C# (.NET 10)
  - Fallback: C# (.NET 8) if Godot doesn't support .NET 10  
**Scripting**: Lua (MoonSharp)

**Key Systems**:
- Graph-based dungeon model ([ADR-001][adr-001])
- Lua scripting for conditions/actions ([ADR-002][adr-002])
- Key-based localization ([ADR-003][adr-003])

See [Architecture Decision Records][adr-index] for detailed technical decisions.

### Data Format

**Source Files**: YAML (preferred) or JSON for world data, items, and localization  
**Runtime Persistence**: SQLite databases (`world.db` for static data, `save.db` for player progress)  
**Build Pipeline**: **MazeCompiler** transforms YAML/JSON sources → SQLite databases

### Performance Targets

- **60 FPS** on mid-range PC
- **30 FPS** on mobile devices
- **< 1s** load times between locations

---

## 7. Art and Audio

### Art Style

*[To be determined during prototype]*

**Options being considered**:
- Pixel art (retro aesthetic)
- 2D hand-drawn
- Low-poly 3D
- Stylized 2.5D

**Placeholder Assets**:
- Simple geometric shapes
- Free/CC0 assets for prototyping

### Audio

*[To be developed]*

**Music**:
- Atmospheric ambient for exploration
- Intense themes for combat

**SFX**:
- UI feedback
- Combat sounds
- Environmental ambience

---

## 8. User Interface

### UI Components

**Main Menu**:
- New Game
- Load Game
- Settings
- Quit

**In-Game HUD**:
- Character status (HP, MP)
- Minimap (optional)
- Action menu
- Message log

**Combat UI**:
- Turn order indicator
- Action buttons
- Enemy status
- Character panels

**Inventory Screen**:
- Item grid
- Equipment slots
- Item details panel
- Character stats

### Controls

**Keyboard & Mouse** (PC):
- WASD or Arrow keys for movement
- Click for actions
- Hotkeys for skills/items

**Touch** (Mobile):
- Tap to move/interact
- Swipe for menus
- On-screen buttons

---

## 9. Development Roadmap

### Phase 1: Prototype (Current)

**Goals**:
- ✅ Project structure setup
- ✅ Core architecture (graph model)
- 🔄 Basic movement and navigation
- 🔄 Simple combat system
- 🔄 UI style decision (2D/2.5D/isometric)

**Duration**: 2-4 weeks  
**Success Criteria**: One playable dungeon with combat

### Phase 2: Core Systems

**Goals**:
- Full combat system implementation
- Inventory and equipment
- Character progression (leveling)
- Save/load system

**Duration**: 6-8 weeks

### Phase 3: Content Creation

**Goals**:
- Expanded enemy variety
- Multiple dungeons
- Quest system
- NPC interactions

**Duration**: 8-12 weeks

### Phase 4: Polish & Release

**Goals**:
- Visual polish
- Audio implementation
- Balance and playtesting
- Mobile builds
- Public alpha release

**Duration**: 4-6 weeks

---

## Appendices

### Inspiration & References

- **M.A.G.U.S.** tabletop RPG (rule system inspiration)
- **Baldur's Gate** (tactical combat, party management)
- **Darkest Dungeon** (turn-based combat, progression)
- **Legend of Grimrock** (dungeon exploration)
- **Divinity: Original Sin 2** (modern CRPG design)

### Glossary

- **CRPG**: Computer Role-Playing Game
- **ADR**: Architecture Decision Record
- **GDD**: Game Design Document
- **HP**: Hit Points (health)
- **MP**: Mana Points (magic resource)
- **NPC**: Non-Player Character
- **XP**: Experience Points

---

**Document Status**: Living document, updated throughout development

**Next Review**: After prototype completion
