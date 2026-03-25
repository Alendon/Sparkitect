using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.Container;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Runtime resolution implementation that resolves services through a metadata-driven
/// provider with fallback to direct container resolution.
/// </summary>
internal class ResolutionScope : IResolutionScope
{
    private readonly ICoreContainer _container;
    private readonly IResolutionProvider? _provider;
    private readonly Dictionary<Type, Dictionary<Type, List<object>>> _metadata;

    internal ResolutionScope(
        ICoreContainer container,
        IResolutionProvider? provider,
        Dictionary<Type, Dictionary<Type, List<object>>> metadata)
    {
        _container = container;
        _provider = provider;
        _metadata = metadata;
    }

    /// <inheritdoc />
    public bool TryResolve<T>(Type wrapperType, [NotNullWhen(true)] out T? service) where T : class
    {
        if (TryResolve(wrapperType, typeof(T), out var obj) && obj is T typed)
        {
            service = typed;
            return true;
        }

        service = null;
        return false;
    }

    /// <inheritdoc />
    public bool TryResolve(Type wrapperType, Type serviceType, out object? service)
    {
        _metadata.TryGetValue(wrapperType, out var deps);
        List<object>? entries = [];
        deps?.TryGetValue(serviceType, out entries);
        
        if (_provider is not null &&
            _provider.TryResolve(serviceType, _container, entries ?? [], out service))
        {
            return true;
        }

        return _container.TryResolve(serviceType, out service);
    }
}
