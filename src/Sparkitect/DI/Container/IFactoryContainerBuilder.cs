using JetBrains.Annotations;
using Sparkitect.DI.Resolution;

namespace Sparkitect.DI.Container;

/// <summary>
/// Stateless builder that materializes a finalized key-to-factory map into a factory container.
/// DIService accumulates the map by iterating every discovered configurator across the whole execution
/// set, then hands the finalized map to <see cref="Build"/> once. The builder itself carries no
/// registration state — it is a pure function of (registrations, scope) to <see cref="IFactoryContainer{TKey,TBase}"/>.
/// </summary>
/// <typeparam name="TKey">The key type used to identify factories</typeparam>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
[PublicAPI]
public interface IFactoryContainerBuilder<TKey, TBase>
    where TBase : class
    where TKey : notnull
{
    /// <summary>
    /// Prepares every registered factory against the provided resolution scope and returns the
    /// fully constructed container.
    /// </summary>
    /// <param name="registrations">The finalized key-to-factory map produced by the configurator sweep.</param>
    /// <param name="scope">Resolution scope used to prepare factory dependencies.</param>
    /// <param name="skipMissing">When true, factories whose dependencies cannot be resolved are silently dropped; otherwise an exception is thrown.</param>
    /// <returns>The constructed factory container.</returns>
    IFactoryContainer<TKey, TBase> Build(
        IReadOnlyDictionary<TKey, IKeyedFactory<TBase>> registrations,
        IResolutionScope scope,
        bool skipMissing = false);
}
