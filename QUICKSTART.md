# Fellhaven - Quick Start Guide

[godot-download]: https://godotengine.org/download
[dotnet-download]: https://dotnet.microsoft.com/download
[vs-download]: https://visualstudio.microsoft.com/
[vscode-download]: https://code.visualstudio.com/
[git-download]: https://git-scm.com/
[gdd]: docs/gdd/GDD.md
[adr]: docs/adr/
[lua-api]: docs/api/lua_api.md
[graph-format]: docs/api/graph_format.md
[contributing]: CONTRIBUTING.md

Get up and running with Fellhaven development in 10 minutes!

## Prerequisites

✅ [Godot 4.4][godot-download] (.NET version)  
✅ [.NET 10 SDK][dotnet-download]  
✅ [Visual Studio 2026][vs-download] or [VS Code][vscode-download] with C# extension  
✅ [Git][git-download]

---

## Step 1: Clone the Repository

```bash
git clone https://github.com/Gwergilius/crpg-fellhaven.git
cd crpg-fellhaven
```

---

## Step 2: Open in Godot

1. Launch **Godot 4.4**
2. Click **"Import"**
3. Navigate to `crpg-fellhaven/src/Fellhaven/`
4. Select `project.godot`
5. Click **"Import & Edit"**

---

## Step 3: Build C# Project

In Godot Editor:
1. Click the **"Build"** button in the top-right corner
2. Wait for build to complete (MoonSharp will be downloaded automatically)
3. Check output panel for any errors

If build succeeds, you'll see: ✅ **Build succeeded**

---

## Step 4: Test the Core Systems

### A. Test Localization

Create a new script: `test_localization.gd`

```gdscript
extends Node

func _ready():
    var loc = LocalizationManager.Instance
    print(loc.Tr("ui.menu.new_game"))  # Should print "New Game"
    
    loc.SetLanguage("hu")
    print(loc.Tr("ui.menu.new_game"))  # Should print "Új Játék"
```

Attach to a Node and run the scene.

### B. Test Dungeon Loading

Create a new C# script: `TestDungeonLoader.cs`

```csharp
using Godot;
using Fellhaven.Core;
using Fellhaven.Systems;

public partial class TestDungeonLoader : Node
{
    public override void _Ready()
    {
        LuaScriptEngine.Initialize();
        
        var locations = DungeonLoader.LoadDungeon("res://data/dungeons/test_dungeon.json");
        
        if (locations != null)
        {
            GD.Print($"Loaded {locations.Count} locations");
            
            var entrance = locations["entrance"];
            GD.Print($"Entrance: {LocalizationManager.Instance.Tr(entrance.NameKey)}");
            
            var edge = entrance.GetAvailableEdge(Direction.North, GameState.Instance);
            if (edge != null)
            {
                GD.Print($"Can go North to: {edge.Destination.Id}");
            }
        }
    }
}
```

---

## Step 5: Create a Simple Test Scene

1. In Godot, create a new scene: **Scene > New Scene**
2. Add a **Control** node as root
3. Add a **Label** as child
4. Attach this script to the Label:

```csharp
using Godot;
using Fellhaven.Systems;

public partial class TestLabel : Label
{
    public override void _Ready()
    {
        Text = LocalizationManager.Instance.Tr("ui.menu.new_game");
    }
}
```

5. Run the scene (F5)

You should see **"New Game"** displayed!

---

## Step 6: Test Movement System

Create a test controller: `TestMovement.cs`

```csharp
using Godot;
using Fellhaven.Core;
using Fellhaven.Systems;

public partial class TestMovement : Node
{
    public override void _Ready()
    {
        // Initialize systems
        LuaScriptEngine.Initialize();
        
        // Load test dungeon
        var locations = DungeonLoader.LoadDungeon("res://data/dungeons/test_dungeon.json");
        if (locations != null)
        {
            GameState.Instance.CurrentLocation = locations["entrance"];
            PrintLocation();
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (GameState.Instance.CurrentLocation == null)
            return;
        
        if (@event.IsActionPressed("move_north"))
        {
            if (GameState.Instance.TryMove(Direction.North))
                PrintLocation();
        }
        else if (@event.IsActionPressed("move_south"))
        {
            if (GameState.Instance.TryMove(Direction.South))
                PrintLocation();
        }
    }
    
    private void PrintLocation()
    {
        var loc = GameState.Instance.CurrentLocation;
        if (loc != null)
        {
            GD.Print($"Current Location: {LocalizationManager.Instance.Tr(loc.NameKey)}");
            GD.Print($"  {LocalizationManager.Instance.Tr(loc.DescriptionKey)}");
        }
    }
}
```

Run this scene and press **W/S** to move North/South!

---

## Step 7: Test Locked Door Mechanic

Add this to your test controller:

```csharp
public override void _Input(InputEvent @event)
{
    // ... existing movement code ...
    
    // Press K to give golden key
    if (@event.IsActionPressed("ui_accept"))
    {
        GameState.Instance.Inventory.AddItem("golden_key", 1);
        GD.Print("🔑 You found a golden key!");
    }
}
```

Now:
1. Move North to corridor
2. Try to move North again (door is locked, message shown)
3. Press Enter to get the golden key
4. Try North again (door unlocks!)

---

## Common Issues

### Build Fails

**Problem**: `MSBuild error: project.csproj not found`  
**Solution**: Click "Project > Tools > C# > Create C# Solution"

**Problem**: `MoonSharp package not found`  
**Solution**: Restore NuGet packages:
```bash
cd src/Fellhaven
dotnet restore
```

### Lua Scripts Don't Work

**Problem**: Scripts don't execute or throw errors  
**Solution**: Call `LuaScriptEngine.Initialize()` in `_Ready()` before using scripts

### Localization Keys Show as [MISSING: ...]

**Problem**: Translation files not loading  
**Solution**: Check that `en.json` and `hu.json` exist in `res://localization/`

---

## Next Steps

✅ You've tested the core systems!

Now you can:

1. **Design your own dungeon** by editing `test_dungeon.json`
2. **Add new locations** and edges
3. **Write Lua scripts** for complex behaviors
4. **Create UI scenes** for a visual dungeon crawler
5. **Implement combat system** (see GDD)

---

## Resources

- 📖 [Game Design Document][gdd]
- 🏭️ [Architecture Decision Records][adr]
- 🌙 [Lua API Reference][lua-api]
- 📊 [Graph Format Spec][graph-format]
- 🐛 [Contributing Guide][contributing]

---

## Getting Help

- 💬 **GitHub Discussions**: Ask questions
- 🐛 **GitHub Issues**: Report bugs
- 📧 **Email**: [your email if you want]

---

**Happy Coding!** 🎮✨

May your dungeons be deep and your bugs be shallow!
