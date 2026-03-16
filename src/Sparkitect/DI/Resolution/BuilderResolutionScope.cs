using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.Container;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Build-time resolution adapter that delegates directly to an <see cref="ICoreContainerBuilder"/>.
/// Used during core container construction where no metadata or providers are available.
/// </summary>
internal class BuilderResolutionScope : IResolutionScope
{
    private readonly ICoreContainerBuilder _builder;

    internal BuilderResolutionScope(ICoreContainerBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public bool TryResolve<T>(Type wrapperType, [NotNullWhen(true)] out T? service) where T : class
    {
        return _builder.TryResolveInternal(out service);
    }

    /// <inheritdoc />
    public bool TryResolve(Type wrapperType, Type serviceType, out object? service)
    {
        return _builder.TryResolveInternal(serviceType, out service);
    }
}
