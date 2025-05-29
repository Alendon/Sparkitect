namespace Sparkitect.DI.Container;

/// <summary>
/// Implementation of an entrypoint container that stores and provides access to entrypoint instances
/// </summary>
/// <typeparam name="TBase">The base type for the entrypoints</typeparam>
internal sealed class EntrypointContainer<TBase> : IEntrypointContainer<TBase> 
    where TBase : class
{
    private readonly List<TBase> _instances;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new entrypoint container with the given instances
    /// </summary>
    /// <param name="instances">The list of entrypoint instances</param>
    public EntrypointContainer(List<TBase> instances)
    {
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));
        _disposed = false;
    }
    
    /// <summary>
    /// Resolves all instances of the entrypoint base type
    /// </summary>
    /// <returns>A read-only list of all entrypoint instances</returns>
    public IReadOnlyList<TBase> ResolveMany()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EntrypointContainer<TBase>));
            
        return _instances.AsReadOnly();
    }
    
    /// <summary>
    /// Disposes the container and all disposable entrypoint instances
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        // Dispose all disposable entrypoint instances
        foreach (var instance in _instances)
        {
            if (instance is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception)
                {
                    // Suppress exceptions during disposal
                }
            }
        }
        
        _instances.Clear();
        _disposed = true;
    }
}