using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The pass-facing handle returned by a pass-level <c>use(description)</c>. It carries the
/// epoch-qualified reference to the declared resource and the instance context for the frame; its
/// <see cref="Fetch"/> resolves to exactly the instance the declaration's facts built (N=1, the same
/// instance the facts' <c>CreateInstance</c> produced, deps resolved first). Fetching it inside the
/// declaring hook is undefined (contract-level only — not guarded at runtime in this layer).
/// </summary>
/// <typeparam name="T">The resource type the handle resolves to.</typeparam>
[PublicAPI]
public sealed class GraphResourceHandle<T> : IGraphResource<T>
{
    private readonly ResourceRef<T> _reference;
    private readonly IInstanceContext _context;

    /// <summary>
    /// Creates the handle for <paramref name="reference"/> (the declared resource) resolving through
    /// <paramref name="context"/> for the frame.
    /// </summary>
    public GraphResourceHandle(ResourceRef<T> reference, IInstanceContext context)
    {
        _reference = reference;
        _context = context;
    }

    /// <inheritdoc/>
    public T Fetch() => _context.Resolve(_reference);
}
