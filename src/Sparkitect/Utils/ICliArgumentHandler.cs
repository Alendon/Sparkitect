using System.Collections.Generic;

namespace Sparkitect.Utils;

/// <summary>
/// Interface for handling command-line arguments passed to the application.
/// </summary>
public interface ICliArgumentHandler
{
    /// <summary>
    /// Initializes the CLI argument handler with the provided arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    void Initialize(string[] args);

    /// <summary>
    /// Checks if the specified argument exists.
    /// </summary>
    /// <param name="key">The argument key to check.</param>
    /// <returns>True if the argument exists; otherwise, false.</returns>
    bool HasArgument(string key);

    /// <summary>
    /// Gets the value of the specified argument.
    /// </summary>
    /// <param name="key">The argument key to retrieve.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if found; otherwise, null.</param>
    /// <returns>True if the argument exists and has a value; otherwise, false.</returns>
    bool TryGetArgumentValue(string key, out string? value);
    
    /// <summary>
    /// Gets multiple values for a specified argument.
    /// </summary>
    /// <param name="key">The argument key to retrieve.</param>
    /// <param name="values">When this method returns, contains the values associated with the specified key, if found; otherwise, an empty collection.</param>
    /// <returns>True if the argument exists and has at least one value; otherwise, false.</returns>
    bool TryGetArgumentValues(string key, out IReadOnlyList<string> values);
}