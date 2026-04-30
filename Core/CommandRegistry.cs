public class CommandRegistry
{
    private readonly Dictionary<string, (string Description, Action<string[]> Handler)> _commands
        = new(StringComparer.OrdinalIgnoreCase);

    public bool ShouldExit { get; private set; }

    public void Register(string name, string description, Action<string[]> handler) =>
        _commands[name] = (description, handler);

    public bool Execute(string input)
    {
        string[] tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return true;
        if (_commands.TryGetValue(tokens[0], out var def))
            def.Handler(tokens[1..]);
        else
            Map.outputBuffer.Add($"Unknown command: {tokens[0]}. Type 'help' for a list.");
        return !ShouldExit;
    }

    public void SignalExit() => ShouldExit = true;

    public string? GetSuggestion(string prefix) =>
        _commands.Keys.FirstOrDefault(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<string> GetHelp() =>
        _commands.OrderBy(k => k.Key).Select(k => $"{k.Key,-20} {k.Value.Description}");
}
