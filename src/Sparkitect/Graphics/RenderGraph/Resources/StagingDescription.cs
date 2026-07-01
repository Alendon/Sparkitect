using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The reusable, parameterless staging composite: sub-declares a host-mapped and a device-local
/// <see cref="BufferResource"/> leaf, performs the device increment (X:0-&gt;X:1, the staging-copy advance —
/// the staging resource is the only legal producer of that advance), and exposes the post-increment ref as
/// the Setup-readable <see cref="PopulatedBuffer"/> declaration product. Size-free: a general staging buffer
/// stages N bytes, data-driven at resolve.
/// </summary>
[PublicAPI]
public sealed record StagingDescription : IResourceDescription<StagingBuffer>
{
    /// <summary>
    /// The device buffer after this description's staging copy (X:1), minted off the staging family directly.
    /// Assigned during <see cref="Declare"/> and read by the declaring pass afterward to wire sibling
    /// declarations; reading it before the description has been declared is an error.
    /// </summary>
    public ResourceRef<BufferResource> PopulatedBuffer { get; private set; }

    /// <inheritdoc/>
    public DeclaredFact<StagingBuffer> Declare(IResourceTransaction tx)
    {
        var host = tx.Declare(new HostBufferDescription());
        var device = tx.Declare(new DeviceBufferDescription());
        var staged = tx.Increment(device);

        PopulatedBuffer = staged;

        var fact = tx.InstantiateFact<StagingFacts>();
        return fact with { Host = host, Device = device, Staged = staged };
    }
}
