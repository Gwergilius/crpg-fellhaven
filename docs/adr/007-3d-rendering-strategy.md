[ADR-001]: 001-engine-choice.md "Engine Choice"
[ADR-002]: 002-platform-strategy.md "Multi-Platform Development Strategy"
[ADR-003]: 003-graph-based-dungeon.md "World Data Model"
[divinity-os]: https://store.steampowered.com/app/435150/Divinity_Original_Sin_2/ "Divinity: Original Sin 2"
[bg3]: https://store.steampowered.com/app/1086940/Baldurs_Gate_3/ "Baldur's Gate 3"
[pillars]: https://store.steampowered.com/app/291650/Pillars_of_Eternity/ "Pillars of Eternity"

# ADR-007: 3D Rendering Strategy — Isometric/Overhead View

**Status**: Accepted

**Date**: 2026-05-15

## Context

Fellhaven is a turn-based fantasy CRPG with a grid-based dungeon model ([ADR-003]). The game
requires a 3D rendering approach that:

1. **Supports tactical combat**: Players need to see party formation, enemy positions, and terrain
2. **Shows spatial relationships**: Multiple party members, NPCs, and environmental features visible simultaneously
3. **Works on all platforms**: Desktop, Web (WASM), Mobile ([ADR-002])
4. **Fits the genre**: Classic CRPG feel with modern presentation
5. **Leverages Godot 4.6.2**: Uses engine capabilities efficiently ([ADR-001])

### View Options Considered

Three major rendering paradigms exist for grid-based dungeon games:

1. **First-Person View** (Dungeon Master, Eye of the Beholder, Might & Magic I-V)
   - Player sees through character's eyes
   - Single grid cell visible at a time
   - Requires step-by-step exploration
   - Good for immersion, claustrophobia, puzzle-solving

2. **Pure 2D Top-Down** (early Ultima, Rogue)
   - Overhead orthographic view
   - Entire map visible (or large portion)
   - Simple rendering, good for strategy
   - Less immersive, flat appearance

3. **Isometric/3/4 Overhead View** (Divinity: Original Sin, Baldur's Gate 3, Pillars of Eternity)
   - Angled overhead camera showing depth
   - Multiple grid cells visible (region/room scale)
   - Party, enemies, and environment visible together
   - Tactical overview while maintaining 3D depth

**Game Design Document states**: "Isometric 3/4 view (⭐ preferred)" — see `docs/gdd/GDD.md`

## Decision

We will use **Godot's 3D engine with an isometric/overhead camera perspective** for dungeon rendering.

### Camera Configuration

- **Camera3D** positioned above and behind the party
- Angled downward at approximately 45-60° (adjustable)
- Orthogonal projection (optional) or perspective with high FOV for tactical clarity
- Smooth camera following via `Camera3D.position` Tween or spring-arm interpolation
- Camera centered on party leader or party centroid

### Scene Architecture

The dungeon world is a **3D scene** composed of:

1. **Grid cells as 3D geometry**:
   - Floor tiles: `MeshInstance3D` with ground textures
   - Walls: Vertical quads/meshes with wall textures
   - Ceilings (if indoors): Optional geometry or transparent for overhead view

2. **Dynamic entities**:
   - Party members: `CharacterBody3D` or `Node3D` with sprite/model
   - NPCs: Same as party members
   - Items, doors, furniture: `MeshInstance3D` or sprite billboards

3. **Lighting and atmosphere**:
   - `DirectionalLight3D` for ambient dungeon lighting
   - `OmniLight3D` for torches, magical lights
   - `WorldEnvironment` with sky (outdoor) or dark ambient (indoor)

4. **Navigation grid**:
   - Grid positions are 3D coordinates (x, z plane)
   - Node graph from [ADR-003] maps to 3D positions
   - Edges determine valid movement paths

### Movement Model

- **Turn-based movement**: Player selects destination cell
- **Smooth interpolation**: Party moves from cell to cell with Tween animation
- **Camera follows**: Camera adjusts position to keep party in view
- **Region visibility**: Entire room/region visible (not just one cell)

## Rationale

### Why Isometric/Overhead?

| Criterion | First-Person | Pure 2D Top-Down | Isometric 3/4 (Chosen) |
|---|---|---|---|
| **Tactical overview** | ❌ One cell at a time | ✅ Full map | ✅ Region-scale view |
| **Party formation visible** | ❌ No | ✅ Yes | ✅ Yes |
| **3D depth perception** | ✅ Strong | ❌ None | ✅ Good |
| **Environmental detail** | ✅ Immersive | ❌ Flat | ✅ Detailed |
| **Combat clarity** | ❌ Hard to judge distances | ✅ Clear grid | ✅ Clear + depth |
| **Modern CRPG standard** | ❌ Retro niche | ❌ Retro | ✅ Genre standard |
| **Mobile touch usability** | ⚠️ Awkward | ✅ Simple tap | ✅ Tap + drag |

Isometric/overhead view is the **de facto standard** for modern party-based CRPGs:
- [Divinity: Original Sin 2][divinity-os] (2017)
- [Baldur's Gate 3][bg3] (2023)
- [Pillars of Eternity][pillars] (2015)
- Pathfinder: Kingmaker/Wrath of the Righteous (2018/2021)

Players expect this perspective for tactical party-based gameplay.

### Why Godot 3D (not 2D or raycaster)?

| Feature | Custom Raycaster | Godot 2D | Godot 3D (Chosen) |
|---|---|---|---|
| **Depth perception** | Manual ray math | Sprite scaling | Built-in camera |
| **Floor & ceiling** | Complex custom code | Sprite layers | Free geometry |
| **Lighting & shadows** | Not supported | Limited | Full 3D lighting |
| **Multi-level dungeons** | Not supported | Layer hacks | Native 3D support |
| **Sky/environment** | Not supported | Background sprite | WorldEnvironment |
| **Godot Editor integration** | Custom tools needed | Scene editor | Full 3D scene editor |
| **Mobile/Web export** | Must implement | Built-in | Built-in |

**Godot 3D gives us everything we need** without custom rendering code:
- Camera system handles perspective automatically
- Lighting, shadows, and environment are built-in
- Scene graph is inspectable and editable in Godot Editor
- Multi-level dungeons are trivial (just use Y-axis)
- Occlusion culling and frustum culling are automatic

The grid-based movement model from [ADR-003] is a **game design constraint**, not a rendering
limitation — it works perfectly with Godot 3D by positioning entities on discrete grid coordinates.

## Consequences

### Positive

- **Tactical gameplay**: Players see party formation, enemy positions, terrain at a glance
- **Genre-appropriate**: Matches player expectations for modern CRPGs
- **Mobile-friendly**: Touch controls for tap-to-move and camera drag are natural
- **Leverages Godot strengths**: Built-in 3D rendering, lighting, scene editor
- **Future-proof**: Can add vertical levels, dynamic lighting, weather effects without architecture changes
- **Modding support**: 3D scenes are editable in Godot Editor, moddable without code

### Negative

- **Higher memory footprint** than 2D: 3D geometry and textures use more RAM (mitigated by LOD and culling)
- **Camera control complexity**: Isometric camera movement and zoom require careful tuning
- **Performance on low-end devices**: 3D rendering more demanding than 2D (mitigated by Godot's mobile optimization)
- **Art asset requirements**: Need 3D models or billboarded sprites for characters and environment

### Neutral

- **Not first-person**: Loses immersive "dungeon crawler" feel of classic games (trade-off for tactical clarity)
- **Grid constraint**: 16×16 cell grid from [ADR-003] preserved, but larger regions visible at once
- **Camera angle**: Adjustable in prototype phase (45°, 60°, or user-configurable)

## Alternatives Considered

### Alternative 1: First-Person View (Dungeon Master style)

**Description**: Camera at player eye level, one cell visible at a time.

**Pros**:
- Immersive dungeon crawler feel
- Tension and claustrophobia
- Strong visual focus on immediate environment

**Cons**:
- **Poor for party tactics**: Can't see formation or multiple combatants
- **Mobile unfriendly**: Touch controls awkward for first-person navigation
- **Limited spatial awareness**: Hard to judge distances and positioning in combat
- **Not genre-standard**: Modern party CRPGs use overhead view

**Why rejected**: Fellhaven is a **party-based tactical CRPG**, not a solo dungeon crawler.
Players need to see all party members and tactical positioning. First-person view sacrifices
this for immersion, which is the wrong trade-off for this genre.

### Alternative 2: Pure 2D Top-Down

**Description**: Orthographic overhead view with 2D sprites.

**Pros**:
- Simplest rendering approach
- Lowest performance cost
- Clear tactical view

**Cons**:
- **Flat appearance**: No depth perception or atmosphere
- **Less immersive**: Feels like a board game
- **Limited visual appeal**: Hard to create engaging environment visuals
- **Not genre-standard**: Modern CRPGs use 3D for atmosphere

**Why rejected**: Pure 2D sacrifices too much visual appeal and atmosphere. Godot 3D provides
depth and lighting at acceptable performance cost. Isometric 3D offers the best balance.

### Alternative 3: Custom 2.5D Raycaster

**Description**: Wolfenstein-style raycasting with manual projection math.

**Pros**:
- Retro appeal
- Lower memory footprint than full 3D

**Cons**:
- **Only supports first-person view**: No isometric/overhead
- **Significant implementation effort**: Custom rendering pipeline
- **Limited features**: No sky, floor, ceiling, lighting without extensive custom work
- **Not justified**: Godot 3D already provides everything a raycaster would implement

**Why rejected**: Raycasters are only relevant for first-person view (Alternative 1), which we
already rejected. For isometric view, Godot 3D is the obvious choice.

### Alternative 4: Hybrid 2D/3D (Sprites on 3D Grid)

**Description**: 3D environment geometry with 2D billboarded sprites for characters.

**Pros**:
- Lower memory for character art (2D sprites vs 3D models)
- Retro aesthetic (sprite-based characters)

**Cons**:
- **Mixing paradigms**: 2D characters on 3D environment can look inconsistent
- **Not a fundamental difference**: Still uses Godot 3D, just sprite billboards instead of models

**Why not rejected**: This is actually a **valid hybrid approach** we may use. 3D environment
with billboarded character sprites is common (Divinity: Original Sin uses this). We can decide
this during asset creation phase — it's an art direction choice, not an architecture decision.

## Implementation Notes

### Camera Setup

```csharp
public partial class DungeonCamera : Camera3D
{
    [Export] public float CameraAngle { get; set; } = 50.0f;  // degrees from horizontal
    [Export] public float CameraDistance { get; set; } = 10.0f;
    [Export] public bool UseOrthogonal { get; set; } = false;
    
    public override void _Ready()
    {
        if (UseOrthogonal)
            Projection = ProjectionType.Orthogonal;
        
        // Position camera above and behind party
        Position = new Vector3(0, CameraDistance, -CameraDistance);
        LookAt(Vector3.Zero, Vector3.Up);
    }
}
```

### Scene Generation from Graph Model

```csharp
public static void GenerateRegionScene(Region region, Node3D parent)
{
    foreach (var node in region.Nodes)
    {
        // Create floor tile
        var floor = CreateFloorTile(node.X, node.Y, node.Visual);
        parent.AddChild(floor);
        
        // Create walls for each edge direction
        foreach (var edge in node.OutgoingEdges)
        {
            if (edge.State != EdgeState.Open)
            {
                var wall = CreateWall(node.X, node.Y, edge.Direction, edge.WallVisuals);
                parent.AddChild(wall);
            }
        }
    }
}
```

### Party Movement

```csharp
public void MovePartyToNode(Location destination)
{
    var targetPos = GridToWorld(destination.X, destination.Y);
    var tween = CreateTween();
    tween.TweenProperty(PartyNode, "position", targetPos, 0.5f)
         .SetTrans(Tween.TransitionType.Cubic);
}

private Vector3 GridToWorld(int x, int y)
{
    return new Vector3(x * GridCellSize, 0, y * GridCellSize);
}
```

## Validation

Prototype phase will test:
- Camera angle preferences (45°, 50°, 60°, user-adjustable)
- Orthogonal vs perspective projection
- Grid cell size and visibility range
- Character sprite/model art direction
- Performance on target platforms

## References

- [Divinity: Original Sin 2][divinity-os] — Isometric party-based CRPG (2017)
- [Baldur's Gate 3][bg3] — Isometric D&D CRPG (2023)
- [Pillars of Eternity][pillars] — Isometric party-based CRPG (2015)
- Godot 3D Camera documentation: https://docs.godotengine.org/en/stable/classes/class_camera3d.html
- Godot 3D Scene documentation: https://docs.godotengine.org/en/stable/tutorials/3d/index.html

## Related ADRs

- [ADR-001]: Engine Choice — Godot 4.6.2 provides robust 3D engine
- [ADR-002]: Multi-Platform Strategy — 3D rendering works on Desktop, Web, Mobile
- [ADR-003]: Graph-Based Dungeon Model — node/edge graph maps to 3D scene

---

**Owner**: Fellhaven Team  
**Last Updated**: 2026-05-15
