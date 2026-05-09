using Serilog;
using Sparkitect.GameState;
using Sparkitect.Utils.DU;

namespace Sparkitect.Utils;

/// <summary>
/// Implementation of the ICliArgumentHandler interface for handling command-line arguments.
/// </summary>
[StateService<ICliArgumentHandler, CoreModule>]
internal class CliArgumentHandler : ICliArgumentHandler
{
    private readonly Dictionary<string, CliArgValue> _arguments =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the CLI argument handler with the provided arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    public void Initialize(string[] args)
    {
        Log.Debug("Initializing CLI argument handler with {ArgCount} arguments", args.Length);
        _arguments.Clear();
        
        foreach (var arg in args)
        {
            // Skip arguments that don't start with '-' or '--'
            if (!arg.StartsWith('-'))
            {
                Log.Verbose("Skipping argument that doesn't start with '-': {Arg}", arg);
                continue;
            }

            var trimmedArg = arg.TrimStart('-');
            
            // Handle key=value arguments
            var equalsIndex = trimmedArg.IndexOf('=');
            if (equalsIndex > 0)
            {
                var key = trimmedArg[..equalsIndex].Trim();
                var value = trimmedArg[(equalsIndex + 1)..].Trim();
                
                // Process semicolon-separated values
                var valueList = value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                
                if (_arguments.TryGetValue(key, out var existingValue))
                {
                    Log.Debug("Updating existing CLI argument: {Key}", key);
                    switch (existingValue)
                    {
                        case CliArgValue.Flag:
                            _arguments[key] = valueList.Count == 1
                                ? new CliArgValue.Single(valueList[0])
                                : new CliArgValue.Multi(valueList);
                            break;
                        case CliArgValue.Single single:
                            var newList = new List<string> { single.Value };
                            newList.AddRange(valueList);
                            _arguments[key] = new CliArgValue.Multi(newList);
                            break;
                        case CliArgValue.Multi multi:
                            ((List<string>)multi.Values).AddRange(valueList);
                            // No dictionary update needed: the list reference is the same.
                            break;
                    }
                }
                else
                {
                    // Create new entry
                    _arguments[key] = valueList.Count == 1
                        ? new CliArgValue.Single(valueList[0])
                        : new CliArgValue.Multi(valueList);
                    Log.Debug("Added CLI argument: {Key} with {ValueCount} values", key, valueList.Count);
                }
            }
            else
            {
                // Handle flag arguments (without values)
                _arguments[trimmedArg] = new CliArgValue.Flag();
                Log.Debug("Added CLI flag argument: {Key}", trimmedArg);
            }
        }
    }

    /// <summary>
    /// Checks if the specified argument exists.
    /// </summary>
    /// <param name="key">The argument key to check.</param>
    /// <returns>True if the argument exists; otherwise, false.</returns>
    public bool HasArgument(string key)
    {
        var hasArg = _arguments.ContainsKey(key);
        Log.Verbose("Checking for CLI argument {Key}: {Result}", key, hasArg ? "Found" : "Not found");
        return hasArg;
    }

    /// <summary>
    /// Gets the value of the specified argument.
    /// </summary>
    /// <param name="key">The argument key to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if found; otherwise, null.</param>
    /// <returns>True if the argument exists and has a value; otherwise, false.</returns>
    public bool TryGetArgumentValue(string key, out string? value)
    {
        if (_arguments.TryGetValue(key, out var argValue) && argValue is CliArgValue.Single single)
        {
            value = single.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets multiple values for a specified argument.
    /// </summary>
    /// <param name="key">The argument key to retrieve.</param>
    /// <param name="values">When this method returns, contains the values associated with the specified key, if found; otherwise, an empty collection.</param>
    /// <returns>True if the argument exists and has at least one value; otherwise, false.</returns>
    public bool TryGetArgumentValues(string key, out IReadOnlyList<string> values)
    {
        if (_arguments.TryGetValue(key, out var argValue))
        {
            switch (argValue)
            {
                case CliArgValue.Flag:
                    values = Array.Empty<string>();
                    return false;
                case CliArgValue.Single single:
                    values = new[] { single.Value };
                    return true;
                case CliArgValue.Multi multi:
                    values = multi.Values;
                    return multi.Values.Count > 0;
            }
        }

        values = Array.Empty<string>();
        return false;
    }

    public IEnumerable<string> GetArgumentValues(string argument)
    {
        return TryGetArgumentValues(argument, out var values) ? values : [];
    }
}