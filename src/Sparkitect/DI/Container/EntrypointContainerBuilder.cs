using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

/// <summary>
/// Builder for creating entrypoint containers
/// </summary>
/// <typeparam name="TBase">The base type for the entrypoints</typeparam>
internal class EntrypointContainerBuilder<TBase> : IEntrypointContainerBuilder<TBase> where TBase : class
{
    private readonly ICoreContainer _coreContainer;
    private readonly List<IEntrypointFactory<TBase>> _factories = [];
    
    /// <summary>
    /// Creates a new entrypoint container builder with the given core container
    /// </summary>
    /// <param name="coreContainer">The core container to resolve dependencies from</param>
    public EntrypointContainerBuilder(ICoreContainer coreContainer)
    {
        _coreContainer = coreContainer ?? throw new ArgumentNullException(nameof(coreContainer));
    }

    /// <summary>
    /// Registers an entrypoint factory with the builder
    /// </summary>
    /// <param name="entrypointFactory">The entrypoint factory to register</param>
    /// <returns>The builder instance for method chaining</returns>
    public IEntrypointContainerBuilder<TBase> Register(IEntrypointFactory<TBase> entrypointFactory)
    {
        if (entrypointFactory is null)
            throw new ArgumentNullException(nameof(entrypointFactory));
            
        _factories.Add(entrypointFactory);
        return this;
    }
    
    /// <summary>
    /// Builds the entrypoint container with all registered entrypoints
    /// </summary>
    /// <returns>The constructed entrypoint container</returns>
    public IEntrypointContainer<TBase> Build()
    {
        var instances = new List<TBase>();
        
        // Create all instances
        foreach (var factory in _factories)
        {
            try
            {
                var instance = factory.CreateInstance(_coreContainer);
                instances.Add(instance);
            }
            catch (Exception ex)
            {
                throw new DependencyResolutionException(
                    $"Failed to create instance of type {factory.ImplementationType.Name}", ex);
            }
        }

        return new EntrypointContainer<TBase>(instances);
    }
}