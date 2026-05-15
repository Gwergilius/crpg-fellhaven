namespace Fellhaven.Systems;

/// <summary>
/// Manages game localization (internationalization).
/// Loads translation files and provides access to localized strings.
/// </summary>
public partial class LocalizationManager : Node
{
    private static LocalizationManager? _instance;

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static LocalizationManager Instance => _instance!;

    private readonly Dictionary<string, Dictionary<string, string>> _translations = [];
    private string _currentLanguage = "en";
    private const string _localizationPath = "res://localization/";

    /// <summary>
    /// Gets or sets the current language code.
    /// </summary>
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set => SetLanguage(value);
    }

    public override void _Ready()
    {
        _instance = this;
        LoadTranslations();

        GD.Print($"[LocalizationManager] Initialized with language: {_currentLanguage}");
    }

    /// <summary>
    /// Loads all translation files from the localization directory.
    /// </summary>
    private void LoadTranslations()
    {
        // Load English (default/fallback)
        LoadLanguage("en", $"{_localizationPath}en.json");

        // Load Hungarian
        LoadLanguage("hu", $"{_localizationPath}hu.json");

        // Set default language
        _currentLanguage = "en";
    }

    /// <summary>
    /// Loads a specific language file.
    /// </summary>
    private void LoadLanguage(string langCode, string filePath)
    {
        if (!Godot.FileAccess.FileExists(filePath))
        {
            GD.PrintErr($"[LocalizationManager] Translation file not found: {filePath}");
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[LocalizationManager] Failed to open: {filePath}");
                return;
            }

            string jsonText = file.GetAsText();
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);

            if (translations != null)
            {
                _translations[langCode] = translations;
                GD.Print($"[LocalizationManager] Loaded {translations.Count} translations for '{langCode}'");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LocalizationManager] Error loading {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the current language and notifies all localized UI elements.
    /// </summary>
    public void SetLanguage(string langCode)
    {
        if (!_translations.ContainsKey(langCode))
        {
            GD.PrintErr($"[LocalizationManager] Language '{langCode}' not available");
            return;
        }

        _currentLanguage = langCode;
        GD.Print($"[LocalizationManager] Language changed to: {langCode}");

        // Notify all localized UI elements to refresh
        GetTree().CallGroup("localized_ui", "UpdateLocalization");

        EmitSignal(SignalName.LanguageChanged, langCode);
    }

    /// <summary>
    /// Translates a key to the current language.
    /// Falls back to English if key not found in current language.
    /// </summary>
    public string Translate(string key)
    {
        // Try current language
        if (_translations.TryGetValue(_currentLanguage, out var currentDict) &&
            currentDict.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English
        if (_currentLanguage != "en" &&
            _translations.TryGetValue("en", out var englishDict) &&
            englishDict.TryGetValue(key, out var englishTranslation))
        {
            return englishTranslation;
        }

        // Key not found
        GD.PrintErr($"[LocalizationManager] Missing translation key: {key}");
        return $"[MISSING: {key}]";
    }

    /// <summary>
    /// Shorthand for Translate().
    /// </summary>
    public string Tr(string key) => Translate(key);

    /// <summary>
    /// Translates with parameter substitution.
    /// Example: Tr("ui.level_up", ("level", "5")) -> "You reached level 5!"
    /// </summary>
    public string Translate(string key, params (string key, string value)[] parameters)
    {
        string text = Translate(key);

        foreach (var (paramKey, paramValue) in parameters)
        {
            text = text.Replace($"{{{paramKey}}}", paramValue);
        }

        return text;
    }

    /// <summary>
    /// Gets all available language codes.
    /// </summary>
    public string[] GetAvailableLanguages()
    {
        return [.. _translations.Keys];
    }

    /// <summary>
    /// Checks if a translation key exists.
    /// </summary>
    public bool HasKey(string key)
    {
        return _translations.TryGetValue(_currentLanguage, out var dict) && dict.ContainsKey(key);
    }

    [Signal]
    public delegate void LanguageChangedEventHandler(string languageCode);
}
