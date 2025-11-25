using OneOf;
using OneOf.Types;
using Serilog;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Utils;

/// <summary>
/// Implementation of the ICliArgumentHandler interface for handling command-line arguments.
/// </summary>
[CreateServiceFactory<ICliArgumentHandler>]internal class CliArgumentHandler : ICliArgumentHandler
{
    private readonly Dictionary<string, OneOf<None, string, List<string>>> _arguments = 
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
                    // Update existing entry based on its current type
                    existingValue.Switch(
                        none => _arguments[key] = valueList.Count == 1 ? valueList[0] : valueList,
                        str => 
                        {
                            var newList = new List<string> { str };
                            newList.AddRange(valueList);
                            _arguments[key] = newList;
                        },
                        list => 
                        {
                            list.AddRange(valueList);
                            // No need to update dictionary as the list reference is the same
                        }
                    );
                }
                else
                {
                    // Create new entry
                    _arguments[key] = valueList.Count == 1 ? valueList[0] : valueList;
                    Log.Debug("Added CLI argument: {Key} with {ValueCount} values", key, valueList.Count);
                }
            }
            else
            {
                // Handle flag arguments (without values)
                _arguments[trimmedArg] = new None();
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
        if (_arguments.TryGetValue(key, out var oneOfValue))
        {
            return oneOfValue.TryPickT1(out value, out _);
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
        if (_arguments.TryGetValue(key, out var oneOfValue))
        {
            IReadOnlyList<string> resultValues = [];
            var success = oneOfValue.Match(
                none => 
                {
                    resultValues = Array.Empty<string>();
                    return false;
                },
                str => 
                {
                    resultValues = new[] { str };
                    return true;
                },
                list => 
                {
                    resultValues = list;
                    return list.Count > 0;
                }
            );
            values = resultValues;
            return success;
        }
        
        values = Array.Empty<string>();
        return false;
    }

    public IEnumerable<string> GetArgumentValues(string argument)
    {
        return TryGetArgumentValues(argument, out var values) ? values : [];
    }
}