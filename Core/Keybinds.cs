using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum InputContext { World, Config, Menu }

public class KeybindSerializable
{
    public InputContext Context { get; set; }
    public ConsoleKey Key { get; set; }
    public ConsoleModifiers Modifiers { get; set; }
    public string CommandId { get; set; } = string.Empty;
}

public class KeybindRegistry
{
    private readonly Dictionary<string, Action> _commands = new();
    
    // Context -> (Key, Mods) -> CommandName
    public Dictionary<InputContext, Dictionary<(ConsoleKey, ConsoleModifiers), string>> Binds { get; set; } = new()
    {
        { InputContext.World, new() },
        { InputContext.Config, new() },
        { InputContext.Menu, new() }
    };
    
    public InputContext CurrentContext { get; set; } = InputContext.World;
    
    public void RegisterCommand(string id, Action action)
    {
        _commands[id] = action;
    }
    
    public void Bind(InputContext context, ConsoleKey key, string commandId) =>
        Binds[context][(key, ConsoleModifiers.None)] = commandId;
        
    public void Bind(InputContext context, ConsoleKey[] keys, string commandId)
    {
        foreach (var key in keys) Binds[context][(key, ConsoleModifiers.None)] = commandId;
    }

    public void Bind(InputContext context, ConsoleKey key, ConsoleModifiers mods, string commandId) =>
        Binds[context][(key, mods)] = commandId;

    public void Bind(InputContext context, ConsoleKey[] keys, ConsoleModifiers mods, string commandId)
    {
        foreach (var key in keys) Binds[context][(key, mods)] = commandId;
    }

    public bool Execute(ConsoleKeyInfo info)
    {
        if (Binds[CurrentContext].TryGetValue((info.Key, info.Modifiers), out var commandId))
        {
            if (_commands.TryGetValue(commandId, out var action))
            {
                action();
                return true;
            }
        }
        return false;
    }

    public void SaveToFile(string path)
    {
        var list = new List<KeybindSerializable>();
        foreach (var ctx in Binds)
        {
            foreach (var bind in ctx.Value)
            {
                list.Add(new KeybindSerializable
                {
                    Context = ctx.Key,
                    Key = bind.Key.Item1,
                    Modifiers = bind.Key.Item2,
                    CommandId = bind.Value
                });
            }
        }
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        var json = JsonSerializer.Serialize(list, options);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, json);
    }

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var list = JsonSerializer.Deserialize<List<KeybindSerializable>>(json, options);
        if (list != null)
        {
            // Clear existing binds safely
            Binds[InputContext.World].Clear();
            Binds[InputContext.Config].Clear();
            Binds[InputContext.Menu].Clear();
            
            foreach (var bind in list)
            {
                Binds[bind.Context][(bind.Key, bind.Modifiers)] = bind.CommandId;
            }
        }
    }
}
