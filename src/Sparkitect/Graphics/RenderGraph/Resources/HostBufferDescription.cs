using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Description for the host-mapped storage-buffer leaf. Carries no size — capacity is data-driven at resolve —
/// and marks no moment; it only declares the plain <see cref="BufferResource"/> backing.
/// </summary>
[PublicAPI]
public sealed record HostBufferDescription : IResourceDescription<BufferResource>
{
    /// <inheritdoc/>
    public DeclaredFact<BufferResource> Declare(IResourceTransaction tx)
        => tx.InstantiateFact<HostBufferFact>();
}
