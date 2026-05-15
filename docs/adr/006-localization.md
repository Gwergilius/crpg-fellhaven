[adr-004]: 004-lua-scripting.md "Lua Scripting for Conditions and Actions"

# ADR-005: Internationalization (i18n) Strategy

**Status**: Accepted

**Date**: 2026-05-14

## Status

Accepted

## Context

Fellhaven should support multiple languages from day one:
- **Primary**: English, Hungarian
- **Future**: Potentially more languages

Content needing localization:
- UI text (menus, buttons, tooltips)
- Location names and descriptions
- Item names and descriptions
- Dialog and narrative text
- System messages and errors

Requirements:
- Easy to add new languages
- Text changes don't require code recompilation
- Runtime language switching
- Support for plural forms and gender (future)
- Version control friendly
- Simple for translators to work with

## Decision

We will implement a **key-based localization system** using **JSON files** with a central `LocalizationManager`.

### Architecture

1. **Localization Keys**: All text uses string keys (e.g., `"ui.menu.new_game"`)
2. **Translation Files**: One JSON file per language in `localization/` folder
3. **LocalizationManager**: Singleton that loads translations and provides `Translate(key)` method
4. **Localized UI Components**: Custom UI controls that auto-update on language change
5. **Fallback Chain**: Missing translations fall back to English

### File Structure

```
localization/
├─ en.json          # English (primary)
├─ hu.json          # Hungarian
└─ keys.txt         # Documentation of all keys
```

### JSON Format

```json
{
  "ui.menu.new_game": "New Game",
  "ui.menu.load_game": "Load Game",
  "location.dungeon_entrance.name": "Dungeon Entrance",
  "location.dungeon_entrance.desc": "A dark stone archway..."
}
```

### Usage in Code

```csharp
// Get translation
string text = LocalizationManager.Instance.Tr("ui.menu.new_game");

// In Godot scenes
[Export] public string LocalizationKey { get; set; }
Text = LocalizationManager.Instance.Tr(LocalizationKey);
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

- **Simple implementation**: Straightforward key-value lookup
- **Easy to add languages**: Just create a new JSON file
- **Runtime switching**: Change language without restart
- **Version control**: JSON files diff well, translators can work independently
- **No external dependencies**: Pure C# implementation
- **Extendable**: Can add features (plurals, variables) later
- **Designer-friendly**: Content creators work with keys, not strings

### Negative Consequences

- **Key management**: Need discipline to use consistent naming convention
- **Missing keys**: Typos in keys only caught at runtime
- **No compile-time safety**: Can't verify all keys exist
- **Manual sync**: Must ensure all language files have same keys
- **Limited features**: No built-in plural forms, gender, or parameter substitution (yet)

### Neutral Consequences

- Localization keys are visible in code/data files
- Translators need basic JSON understanding (or we provide a tool)

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

**Why rejected**: Over-engineered. JSON files are sufficient and simpler.

### Alternative 4: Inline String Tables (C#)

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
    
    public string Tr(string key)
    {
        // Try current language
        if (_translations[_currentLanguage].TryGetValue(key, out var text))
            return text;
        
        // Fallback to English
        if (_translations["en"].TryGetValue(key, out text))
            return text;
        
        // Key not found
        return $"[MISSING: {key}]";
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

### Future Enhancements

1. **Parameter substitution**:
   ```json
   "ui.level_up": "You reached level {level}!"
   ```

2. **Plural forms**:
   ```json
   "ui.items_found": {
     "zero": "No items found",
     "one": "Found 1 item",
     "other": "Found {count} items"
   }
   ```

3. **Context and comments for translators**:
   ```json
   {
     "key": "ui.action.block",
     "en": "Block",
     "context": "Combat action: defend with shield",
     "note": "Keep it short (max 10 chars)"
   }
   ```

### Translation Workflow

1. Developer adds new text with key
2. Key added to `en.json` (primary language)
3. Key documented in `keys.txt` with context
4. Translators update their language files
5. CI validates all language files have same keys
6. Missing translations flagged in PR

### Validation Tool

Create a simple validator tool:

```csharp
public static void ValidateTranslations()
{
    var enKeys = LoadKeys("en.json");
    var huKeys = LoadKeys("hu.json");
    
    var missing = enKeys.Except(huKeys);
    var extra = huKeys.Except(enKeys);
    
    // Report discrepancies
}
```

## References

- JSON format: https://www.json.org/
- Godot localization docs: https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html
- GetText (for reference): https://www.gnu.org/software/gettext/

## Related Decisions

- [ADR-003][adr-003]: Graph-Based Dungeon Model (locations use localization keys)
- ADR-006: UI Framework (localized UI components) [TBD]
