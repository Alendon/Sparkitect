using Sparkitect.DI.Resolution;

namespace Sparkitect.DI.Container;

/// <summary>
/// Stateless materializer that prepares every registered factory against the given resolution scope
/// and returns the constructed container. The builder holds no registration state — DIService is the
/// single owner of the aggregate registration map across the whole execution set and passes the
/// finalized map to <see cref="Build"/> once.
/// </summary>
/// <typeparam name="TKey">The key type used to identify factories</typeparam>
/// <typeparam name="TBase">The base type for objects created by the factories</typeparam>
internal sealed class FactoryContainerBuilder<TKey, TBase> : IFactoryContainerBuilder<TKey, TBase>
    where TBase : class
    where TKey : notnull
{
    /// <summary>
    /// Prepares every registered factory against <paramref name="scope"/> and returns the container.
    /// </summary>
    /// <param name="registrations">The finalized key-to-factory map produced by the configurator sweep.</param>
    /// <param name="scope">Resolution scope used to prepare factory dependencies.</param>
    /// <param name="skipMissing">When true, factories whose dependencies cannot be resolved are silently dropped; otherwise an exception is thrown.</param>
    /// <exception cref="InvalidOperationException">Thrown when a factory's dependencies cannot be resolved and <paramref name="skipMissing"/> is false.</exception>
    public IFactoryContainer<TKey, TBase> Build(
        IReadOnlyDictionary<TKey, IKeyedFactory<TBase>> registrations,
        IResolutionScope scope,
        bool skipMissing = false)
    {
        var preparedFactories = new Dictionary<TKey, IKeyedFactory<TBase>>();

        foreach (var (key, factory) in registrations)
        {
            if (!factory.TryPrepare(scope))
            {
                if (skipMissing) continue;

                throw new InvalidOperationException(
                    $"Failed to prepare factory with key '{key}' of type '{factory.ImplementationType.Name}'. " +
                    "One or more required dependencies could not be resolved from the container.");
            }
            preparedFactories[key] = factory;
        }

        return new FactoryContainer<TKey, TBase>(preparedFactories);
    }
}
