[ADR-003]: 003-graph-based-dungeon.md "World Data Model"
[ADR-005]: 005-lua-scripting.md "Event-Action System with Lua Scripting"

# ADR-006: Internationalization (i18n) Strategy

**Status**: Accepted

**Date**: 2026-05-15

## Context

Fellhaven should support multiple languages from day one:
- **Primary**: English, Hungarian
- **Future**: Potentially more languages

Content needing localization:
- UI text (menus, buttons, tooltips)
- Location names and descriptions
- Item names and descriptions
- Dialog and narrative text (including dynamic runtime messages)
- System messages and errors
- Script package content (mods, custom puzzles)

Requirements:
- Easy to add new languages
- Text changes don't require code recompilation
- Runtime language switching
- **Template-based rendering**: Dynamic text composed from templates with placeholders
- **No string concatenation**: Prevents word order hardcoding from source language
- Support for plural forms and gender (future)
- Support for script packages with bundled translations
- Version control friendly
- Simple for translators to work with

## Decision

We will implement a **template-based localization system** with a two-tier architecture:

### Two-Tier Architecture

**Tier 1 — Static Translations (JSON)**: System UI, menus, and fixed game text stored as
JSON key-value files. Fast lookup, preloaded at startup.

**Tier 2 — Script Translations (`.i18` YAML)**: Dynamic text for Lua script packages
(see [ADR-005]). Each script package includes its own `.i18` translation files bundled
in the script ZIP. Loaded on-demand when the script executes.

### Core Principles

1. **Template-Based Rendering**: All composited text uses templates with named placeholders
   - Placeholder syntax: `{identifier}`
   - Example: `"You found {item_name} in {location_name}"`
   - Localized templates control word order, not code logic

2. **No String Concatenation**: Game code must never build localized text by concatenating
   language fragments. Only full templates + placeholder substitution are allowed.
   ```csharp
   // ❌ WRONG: string concatenation
   string msg = Tr("you_found") + " " + itemName + " " + Tr("in") + " " + locationName;
   
   // ✅ CORRECT: template with placeholders
   string msg = Tr("msg.item_found", ("item", itemName), ("location", locationName));
   ```

3. **Localization Keys**: All text uses hierarchical string keys (e.g., `"ui.menu.new_game"`)

4. **Translation Files**: One JSON file per language in `localization/` folder

5. **LocalizationManager**: Singleton that loads translations and provides `Tr(key, params)` method

6. **Localized UI Components**: Custom UI controls that auto-update on language change

7. **Fallback Chain**: Missing translations fall back to English

### File Structure

```
localization/
├─ en.json          # English (primary)
├─ hu.json          # Hungarian
└─ keys.txt         # Documentation of all keys
```

### Tier 1: JSON Format (Static Translations)

```json
{
  "ui.menu.new_game": "New Game",
  "ui.menu.load_game": "Load Game",
  "location.dungeon_entrance.name": "Dungeon Entrance",
  "location.dungeon_entrance.desc": "A dark stone archway leading underground.",
  "msg.item_found": "You found {item} in {location}",
  "msg.gold_gained": "You gained {amount} gold",
  "msg.level_up": "{character} reached level {level}!",
  "combat.damage_dealt": "{attacker} deals {damage} damage to {target}"
}
```

### Tier 2: `.i18` Format (Script Package Translations)

Script packages (Lua mods) include their own translations as `.i18` YAML files:

```yaml
# scripts/puzzle_ancient_lock.en.i18
en:
  puzzle.lock.inspect: "An ancient lock with {count} tumblers."
  puzzle.lock.success: "The lock clicks open!"
  puzzle.lock.failure: "The lock resists your attempt."
  puzzle.lock.hint: "Try aligning the {color} tumbler first."
```

```yaml
# scripts/puzzle_ancient_lock.hu.i18
hu:
  puzzle.lock.inspect: "Egy ősi zár {count} retesszel."
  puzzle.lock.success: "A zár kattanva kinyílik!"
  puzzle.lock.failure: "A zár ellenáll a kísérletednek."
  puzzle.lock.hint: "Próbáld először a {color} reteszt beállítani."
```

Script packages are distributed as ZIP files:
```
puzzle_ancient_lock.zip
├─ puzzle.lua
├─ puzzle.en.i18
└─ puzzle.hu.i18
```

### Usage in Code

```csharp
// Simple translation (no parameters)
string text = LocalizationManager.Instance.Tr("ui.menu.new_game");
// Result: "New Game"

// Template with placeholders
string msg = LocalizationManager.Instance.Tr(
    "msg.item_found",
    ("item", "Silver Key"),
    ("location", "Ancient Crypt")
);
// Result: "You found Silver Key in Ancient Crypt"

// Combat message example
string combatMsg = LocalizationManager.Instance.Tr(
    "combat.damage_dealt",
    ("attacker", warrior.Name),
    ("damage", damageAmount.ToString()),
    ("target", orc.Name)
);
// Result: "Thorin deals 15 damage to Orc Warrior"

// In Godot scenes
[Export] public string LocalizationKey { get; set; }
Text = LocalizationManager.Instance.Tr(LocalizationKey);
```

### Usage in Lua Scripts

```lua
-- Load script package translations
local i18 = LoadScriptTranslations("puzzle_ancient_lock")

-- Simple lookup
local msg = i18.Tr("puzzle.lock.inspect", { count = 3 })
-- Result: "An ancient lock with 3 tumblers."

-- With multiple parameters
local hint = i18.Tr("puzzle.lock.hint", { color = "red" })
-- Result: "Try aligning the red tumbler first."
```

### Localized Components

```csharp
public partial class LocalizedLabel : Label
{
    [Export] public string LocalizationKey { get; set; }
    
    public void UpdateLocalization()
    {
        Text = LocalizationManager.Instance.Tr(LocalizationKey);
    }
}
```

## Consequences

### Positive Consequences

- **Template-based text composition**: Natural word order per language, no hardcoded concatenation
- **Parameter substitution**: Dynamic values inserted into localized templates
- **Two-tier architecture**: Static JSON for performance, dynamic `.i18` for mods and scripts
- **No string concatenation**: Enforced architectural rule prevents untranslatable text
- **Simple Tier 1 implementation**: Straightforward key-value lookup for UI and system text
- **Script package localization**: Mods include their own translations, no global namespace pollution
- **Easy to add languages**: Just create new JSON/`.i18` files
- **Runtime switching**: Change language without restart
- **Version control friendly**: JSON/YAML files diff well, translators can work independently
- **No external dependencies**: Pure C# implementation (for now)
- **Migration path**: Can upgrade to advanced i18n engine without breaking existing code
- **Designer-friendly**: Content creators work with keys and templates, not language strings

### Negative Consequences

- **Key management**: Need discipline to use consistent naming convention
- **Placeholder discipline**: Developers must use placeholders correctly
- **Missing keys**: Typos in keys only caught at runtime (no compile-time safety)
- **Template validation**: Must ensure placeholder names match between template and code
- **Manual sync**: Must ensure all language files have same keys and placeholders
- **Limited features**: No built-in plural forms, gender agreement (yet) — must be added later

### Neutral Consequences

- Localization keys are visible in code/data files
- Translators need basic JSON/YAML understanding (or we provide a tool)
- Template syntax `{placeholder}` is part of the public contract

## Alternatives Considered

### Alternative 1: Godot's Built-in Localization (CSV-based)

**Description**: Use Godot's native localization system with CSV files.

**Pros**:
- Built into Godot, no custom code needed
- CSV editor support
- Handles plurals, context

**Cons**:
- CSV format awkward for version control
- Tied to Godot, can't easily use in standalone tools
- Less flexible for dynamic content
- Harder to parse/validate programmatically

**Why rejected**: Want more control and flexibility. JSON easier to work with in code and tooling.

### Alternative 2: GetText (.po files)

**Description**: Use industry-standard GetText system with .po files.

**Pros**:
- Industry standard
- Excellent tooling (Poedit, etc.)
- Supports context, plurals, comments for translators
- Large ecosystem

**Cons**:
- .po file format complex to parse
- Requires additional library
- Overkill for initial needs
- More setup complexity

**Why rejected**: Too heavyweight for current needs. Can migrate later if needed.

### Alternative 3: Database-backed Localization

**Description**: Store translations in SQLite database.

**Pros**:
- Easy to query and manage
- Can add metadata (translator notes, timestamps)
- Can have translation UI that edits database

**Cons**:
- Adds dependency (SQLite)
- Harder to version control
- More complex than needed
- Difficult for non-technical translators

**Why rejected**: Over-engineered. JSON files are sufficient and simpler for base game content.
Note: The two-tier system uses database-backed lookups for compiled world data (future),
but translations themselves remain in JSON/YAML source files.

### Alternative 4: Single-Tier System (JSON or YAML only)

**Description**: Use only one format for all translations (either JSON or YAML).

**Pros**:
- Simpler architecture (only one parser)
- Consistent format throughout
- Easier to understand for new contributors

**Cons**:
- JSON lacks comments, difficult for lengthy narrative text
- YAML is slower to parse for frequent UI lookups
- Script packages would need to reference global translation pool (namespace pollution)
- Doesn't separate static (preloaded) from dynamic (on-demand) content

**Why rejected**: Two-tier system gives best of both worlds — fast JSON for UI, readable
YAML for scripts. Script packages are self-contained with their own translations.

### Alternative 5: Inline String Tables (C#)

**Description**: Define translations directly in C# as dictionaries.

```csharp
var translations = new Dictionary<string, string>
{
    { "ui.menu.new_game", "New Game" },
    // ...
};
```

**Pros**:
- Type-safe
- Compile-time checked
- No file I/O

**Cons**:
- Requires recompilation for any text change
- Verbose
- Not translator-friendly
- Can't add languages without code change

**Why rejected**: Defeats the purpose of data-driven localization.

## Implementation Notes

### LocalizationManager Class

```csharp
public partial class LocalizationManager : Node
{
    private static LocalizationManager _instance;
    private Dictionary<string, Dictionary<string, string>> _translations;
    private string _currentLanguage = "en";
    
    public void SetLanguage(string langCode)
    {
        _currentLanguage = langCode;
        GetTree().CallGroup("localized_ui", "UpdateLocalization");
    }
    
    // Simple translation (no parameters)
    public string Tr(string key)
    {
        return Tr(key, Array.Empty<(string, string)>());
    }
    
    // Template translation with named placeholders
    public string Tr(string key, params (string name, string value)[] parameters)
    {
        // Try current language
        string template;
        if (_translations[_currentLanguage].TryGetValue(key, out template))
        {
            return SubstitutePlaceholders(template, parameters);
        }
        
        // Fallback to English
        if (_translations["en"].TryGetValue(key, out template))
        {
            return SubstitutePlaceholders(template, parameters);
        }
        
        // Key not found
        return $"[MISSING: {key}]";
    }
    
    private string SubstitutePlaceholders(string template, (string name, string value)[] parameters)
    {
        string result = template;
        foreach (var (name, value) in parameters)
        {
            result = result.Replace($"{{{name}}}", value);
        }
        return result;
    }
}
```

### Key Naming Convention

Use hierarchical dot-notation:

```
{category}.{subcategory}.{identifier}

Examples:
ui.menu.new_game
ui.menu.load_game
ui.combat.attack
ui.combat.defend
location.entrance.name
location.entrance.desc
item.sword.name
item.sword.desc
dialog.npc_guard.greeting
error.save_failed
```

### Advanced Features

#### Plural Forms (Future Enhancement)

Different languages have different plural rules (English: 1 vs. many; Russian: 1, 2-4, 5+).

```json
"ui.items_found": {
  "zero": "No items found",
  "one": "Found 1 item",
  "few": "Found {count} items",
  "many": "Found {count} items",
  "other": "Found {count} items"
}
```

Usage:
```csharp
string msg = LocalizationManager.Instance.TrPlural(
    "ui.items_found",
    itemCount,
    ("count", itemCount.ToString())
);
```

#### Context and Translator Notes (Future Enhancement)

Extended JSON format with metadata:

```json
{
  "key": "ui.action.block",
  "translations": {
    "en": "Block",
    "hu": "Védekezés"
  },
  "context": "Combat action: defend with shield",
  "notes": "Keep it short (max 10 chars for UI button)",
  "max_length": 10
}
```

#### Gender Agreement (Future Enhancement)

For languages with grammatical gender:

```json
"msg.character_died": {
  "male": "{name} fell in battle.",
  "female": "{name} fell in battle.",
  "neutral": "{name} fell in battle."
}
```

Usage:
```csharp
string msg = LocalizationManager.Instance.TrGender(
    "msg.character_died",
    character.Gender,
    ("name", character.Name)
);
```

### Translation Workflow

1. Developer adds new text with key and template
2. Template added to `en.json` with placeholders (if needed)
3. Key documented in `keys.txt` with context and placeholder meanings
4. Translators update their language files
   - Preserve placeholder names: `{item}`, `{location}`, etc.
   - Reorder placeholders as needed for natural word order
5. CI validates all language files:
   - Same keys present
   - Same placeholder names in templates
   - No missing or extra placeholders
6. Missing translations or placeholder mismatches flagged in PR

### Validation Tool

Create a validator tool that checks both keys and placeholders:

```csharp
public static class TranslationValidator
{
    public static void ValidateTranslations()
    {
        var enData = LoadTranslations("en.json");
        var huData = LoadTranslations("hu.json");
        
        // Check for missing/extra keys
        var enKeys = enData.Keys.ToHashSet();
        var huKeys = huData.Keys.ToHashSet();
        
        var missing = enKeys.Except(huKeys);
        var extra = huKeys.Except(enKeys);
        
        if (missing.Any())
            GD.PrintErr($"Missing translations in hu.json: {string.Join(", ", missing)}");
        if (extra.Any())
            GD.PrintErr($"Extra keys in hu.json: {string.Join(", ", extra)}");
        
        // Check placeholder consistency
        foreach (var key in enKeys.Intersect(huKeys))
        {
            var enPlaceholders = ExtractPlaceholders(enData[key]);
            var huPlaceholders = ExtractPlaceholders(huData[key]);
            
            if (!enPlaceholders.SetEquals(huPlaceholders))
            {
                GD.PrintErr($"Placeholder mismatch in key '{key}':");
                GD.PrintErr($"  EN: {{{string.Join(", ", enPlaceholders)}}}");
                GD.PrintErr($"  HU: {{{string.Join(", ", huPlaceholders)}}}");
            }
        }
    }
    
    private static HashSet<string> ExtractPlaceholders(string template)
    {
        var regex = new Regex(@"\{(\w+)\}");
        return regex.Matches(template)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToHashSet();
    }
}
```

### Rationale for Two-Tier System

**Why JSON for Tier 1?**
- Fast to parse and preload
- Simple key-value lookup for frequent operations (UI rendering)
- Easy to version control (clean diffs)
- Standard format with wide tool support

**Why YAML `.i18` for Tier 2?**
- Bundled with script packages (self-contained mods)
- Loaded on-demand (not polluting main translation pool)
- YAML is more readable for longer narrative text
- Natural fit for script-driven content
- Consistent with script package distribution model ([ADR-005])

### Migration Path to Advanced i18n Engine

The current system is designed to support future migration to a more sophisticated
internationalization engine if needed:

1. **Template contract is stable**: `{placeholder}` syntax will remain
2. **No concatenation rule enforced now**: Makes migration easier later
3. **Placeholder-based architecture**: Compatible with advanced morphology engines
4. **Potential future**: Package intelligent language generation as a NuGet dependency
   for features like automatic inflection, case agreement, and language-specific grammar

This migration would be transparent to game code — only LocalizationManager implementation changes.

## References

- JSON format: https://www.json.org/
- YAML format: https://yaml.org/
- Godot localization docs: https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html
- ICU MessageFormat (inspiration for placeholder syntax): https://unicode-org.github.io/icu/userguide/format_parse/messages/
- GetText (for reference): https://www.gnu.org/software/gettext/
- Unicode CLDR Plural Rules: https://cldr.unicode.org/index/cldr-spec/plural-rules

## Related ADRs

- [ADR-003]: Graph-Based Dungeon Model — locations use localization keys (`name_key` fields)
- [ADR-005]: Event-Action System with Lua Scripting — script packages include `.i18` translations

---

**Owner**: Fellhaven Team  
**Last Updated**: 2026-05-15
