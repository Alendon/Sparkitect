using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using OneOf.Types;

namespace Sparkitect.Utils;

/// <summary>
/// Implementation of the ICliArgumentHandler interface for handling command-line arguments.
/// </summary>
internal class CliArgumentHandler : ICliArgumentHandler
{
    private readonly Dictionary<string, OneOf<None, string, List<string>>> _arguments = 
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the CLI argument handler with the provided arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    public void Initialize(string[] args)
    {
        _arguments.Clear();
        
        foreach (var arg in args)
        {
            // Skip arguments that don't start with '-' or '--'
            if (!arg.StartsWith('-'))
                continue;

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
                }
            }
            else
            {
                // Handle flag arguments (without values)
                _arguments[trimmedArg] = new None();
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
        return _arguments.ContainsKey(key);
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
}