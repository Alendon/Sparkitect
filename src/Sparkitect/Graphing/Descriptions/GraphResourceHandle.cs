using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>The pass-facing handle returned by a pass-level <c>use(description)</c>; its <see cref="Fetch"/> resolves to the instance the declaration's facts built. Fetching inside the declaring hook is undefined (not runtime-guarded in this layer).</summary>
/// <typeparam name="T">The resource type the handle resolves to.</typeparam>
[PublicAPI]
public sealed class GraphResourceHandle<T> : IGraphResource<T>
{
    private readonly ResourceRef<T> _reference;
    private readonly IInstanceContext _context;

    /// <summary>Creates the handle for <paramref name="reference"/> resolving through <paramref name="context"/> for the frame.</summary>
    public GraphResourceHandle(ResourceRef<T> reference, IInstanceContext context)
    {
        _reference = reference;
        _context = context;
    }

    /// <inheritdoc/>
    public T Fetch() => _context.Resolve(_reference);
}
