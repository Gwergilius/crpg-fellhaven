using Godot;
using MoonSharp.Interpreter;
using System;

namespace Fellhaven.Core;

/// <summary>
/// Lua scripting engine for evaluating conditions and executing actions.
/// Uses MoonSharp library for Lua 5.2 compatibility.
/// </summary>
public static class LuaScriptEngine
{
    private static Script? _luaEngine;
    private static bool _initialized = false;
    
    /// <summary>
    /// Initializes the Lua engine and registers built-in functions.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;
        
        _luaEngine = new Script();
        RegisterBuiltInFunctions();
        _initialized = true;
        
        GD.Print("[LuaScriptEngine] Initialized with MoonSharp");
    }
    
    /// <summary>
    /// Registers all built-in Lua functions that can be called from scripts.
    /// </summary>
    private static void RegisterBuiltInFunctions()
    {
        if (_luaEngine == null)
            return;
        
        // Register GameState type
        UserData.RegisterType<GameState>();
        
        // === Flag Management ===
        _luaEngine.Globals["HasFlag"] = (Func<GameState, string, bool>)
            ((state, flag) => state.HasFlag(flag));
        
        _luaEngine.Globals["SetFlag"] = (Action<GameState, string, bool>)
            ((state, flag, value) => state.SetFlag(flag, value));
        
        // === Variable Management ===
        _luaEngine.Globals["GetVar"] = (Func<GameState, string, int>)
            ((state, varName) => state.GetVariable(varName));
        
        _luaEngine.Globals["SetVar"] = (Action<GameState, string, int>)
            ((state, varName, value) => state.SetVariable(varName, value));
        
        _luaEngine.Globals["AddVar"] = (Action<GameState, string, int>)
            ((state, varName, amount) => state.AddVariable(varName, amount));
        
        // === Inventory Management ===
        _luaEngine.Globals["HasItem"] = (Func<GameState, string, bool>)
            ((state, itemId) => state.Inventory.HasItem(itemId));
        
        _luaEngine.Globals["HasItems"] = (Func<GameState, string, int, bool>)
            ((state, itemId, count) => state.Inventory.HasItems(itemId, count));
        
        _luaEngine.Globals["GetItemCount"] = (Func<GameState, string, int>)
            ((state, itemId) => state.Inventory.GetItemCount(itemId));
        
        _luaEngine.Globals["GiveItem"] = (Action<GameState, string, int>)
            ((state, itemId, count) => state.Inventory.AddItem(itemId, count));
        
        _luaEngine.Globals["RemoveItem"] = (Action<GameState, string, int>)
            ((state, itemId, count) => state.Inventory.RemoveItem(itemId, count));
        
        // === Party & Character ===
        _luaEngine.Globals["HasClass"] = (Func<GameState, string, bool>)
            ((state, className) => state.PlayerParty.HasClass(className));
        
        _luaEngine.Globals["HasSkill"] = (Func<GameState, string, int, bool>)
            ((state, skillName, minLevel) => state.PlayerParty.HasSkill(skillName, minLevel));
        
        _luaEngine.Globals["GetPartyLevel"] = (Func<GameState, int>)
            ((state) => state.PlayerParty.GetAverageLevel());
        
        // === UI & Messaging ===
        _luaEngine.Globals["ShowMessage"] = (Action<GameState, string>)
            ((state, messageKey) => ShowMessage(messageKey));
        
        // === Utility ===
        _luaEngine.Globals["Log"] = (Action<string>)
            ((message) => GD.Print($"[Lua] {message}"));
    }
    
    /// <summary>
    /// Evaluates a Lua condition script.
    /// </summary>
    /// <param name="luaScript">Lua script that should return a boolean.</param>
    /// <param name="gameState">Current game state.</param>
    /// <returns>True if condition passes, false otherwise (including on error).</returns>
    public static bool EvaluateCondition(string luaScript, GameState gameState)
    {
        if (!_initialized)
            Initialize();
        
        if (_luaEngine == null)
        {
            GD.PrintErr("[LuaScriptEngine] Engine not initialized");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(luaScript))
            return true; // Empty script = always true
        
        try
        {
            _luaEngine.Globals["gameState"] = gameState;
            DynValue result = _luaEngine.DoString(luaScript);
            
            // Handle different return types
            if (result.Type == DataType.Boolean)
                return result.Boolean;
            
            if (result.Type == DataType.Nil)
                return false;
            
            // Truthy evaluation for other types
            return result.CastToBool();
        }
        catch (ScriptRuntimeException ex)
        {
            GD.PrintErr($"[LuaScriptEngine] Runtime error in condition script:\n{ex.DecoratedMessage}");
            return false; // Fail-safe: deny on error
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LuaScriptEngine] Unexpected error in condition script: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Executes a Lua action script.
    /// </summary>
    /// <param name="luaScript">Lua script to execute.</param>
    /// <param name="gameState">Current game state.</param>
    public static void ExecuteAction(string luaScript, GameState gameState)
    {
        if (!_initialized)
            Initialize();
        
        if (_luaEngine == null)
        {
            GD.PrintErr("[LuaScriptEngine] Engine not initialized");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(luaScript))
            return; // Empty script = no action
        
        try
        {
            _luaEngine.Globals["gameState"] = gameState;
            _luaEngine.DoString(luaScript);
        }
        catch (ScriptRuntimeException ex)
        {
            GD.PrintErr($"[LuaScriptEngine] Runtime error in action script:\n{ex.DecoratedMessage}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LuaScriptEngine] Unexpected error in action script: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Shows a localized message to the player.
    /// </summary>
    private static void ShowMessage(string messageKey)
    {
        // TODO: Implement message display system
        GD.Print($"[Message] {messageKey}");
    }
    
    /// <summary>
    /// Validates a Lua script for syntax errors without executing it.
    /// </summary>
    public static bool ValidateScript(string luaScript, out string? errorMessage)
    {
        if (!_initialized)
            Initialize();
        
        if (_luaEngine == null)
        {
            errorMessage = "Engine not initialized";
            return false;
        }
        
        try
        {
            _luaEngine.LoadString(luaScript);
            errorMessage = null;
            return true;
        }
        catch (SyntaxErrorException ex)
        {
            errorMessage = ex.DecoratedMessage;
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
