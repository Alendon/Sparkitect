using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Sparkitect.DI.Container;

/// <summary>
/// Container for keyed factory instances that create objects on demand
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
[PublicAPI]
public interface IFactoryContainer<TBase> : IDisposable where TBase : class
{
    /// <summary>
    /// Resolves all available factories and their keys
    /// </summary>
    /// <returns>A read-only dictionary of all registered factory keys and their created instances</returns>
    IReadOnlyDictionary<string, TBase> ResolveAll();

    /// <summary>
    /// Attempts to resolve a factory by key and create an instance
    /// </summary>
    /// <param name="key">The string key to look up the factory</param>
    /// <param name="instance">The created instance if found</param>
    /// <returns>True if the factory was found and instance created successfully</returns>
    bool TryResolve(string key, [NotNullWhen(true)] out TBase? instance);

    /// <summary>
    /// Gets metadata mapping keys to concrete implementation types without creating instances.
    /// </summary>
    IReadOnlyDictionary<string, Type> Metadata { get; }
}