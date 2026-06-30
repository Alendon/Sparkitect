using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Graphing;

/// <summary>
/// Minimal POCO stand-in resources and description-driven fixtures shared across the Graphing oracle
/// suites. These carry no GPU dependency: a leaf resource is a plain record, a composite holds a
/// sub-resource instance plus CPU-side metadata. Per the generalization principle the staged
/// buffer is size-only — a general description assumes nothing about data shape, stride, or layout.
/// The instance a description's facts build is exactly one of these POCOs.
/// </summary>
internal static class SyntheticDomain
{
    /// <summary>A leaf stand-in resource — a buffer of a given size (no backing, no GPU, no stride).</summary>
    internal sealed record SyntheticBuffer(int Size);

    /// <summary>
    /// A composite stand-in resource — a CPU-side count alongside two resolved sub-resource instances.
    /// Models the staging shape (host + device buffers) MINUS the deferred declaration-product property.
    /// </summary>
    internal sealed record SyntheticComposite(SyntheticBuffer Host, SyntheticBuffer Device, int Count);

    /// <summary>A leaf description resolving to a size-only <see cref="SyntheticBuffer"/>.</summary>
    internal sealed record LeafBufferDescription(int Size) : IResourceDescription<SyntheticBuffer>
    {
        public DeclaredFact<SyntheticBuffer> Declare(IResourceTransaction tx) =>
            new LeafBufferFact(Size);
    }

    /// <summary>Leaf facts: build the buffer directly (no sub-references to resolve).</summary>
    internal sealed record LeafBufferFact(int Size) : DeclaredFact<SyntheticBuffer>
    {
        public SyntheticBuffer CreateInstance(IInstanceContext ctx) => new(Size);

        public CleanupStrategy CleanupStrategy { get; }
    }

    /// <summary>
    /// A composite description (staging shape): sub-declares a host leaf and a device leaf, increments
    /// the device leaf (staging it one epoch forward), and resolves to a <see cref="SyntheticComposite"/>
    /// composed dependency-first from the two sub-instances.
    /// </summary>
    internal sealed record StagingDescription(int HostSize, int DeviceSize, int Count)
        : IResourceDescription<SyntheticComposite>
    {
        public DeclaredFact<SyntheticComposite> Declare(IResourceTransaction tx)
        {
            var host = tx.Declare(new LeafBufferDescription(HostSize));
            var device = tx.Declare(new LeafBufferDescription(DeviceSize));
            var staged = tx.Increment(device);
            return new StagingFact(host, staged, Count);
        }
    }

    /// <summary>Composite facts: compose the POCO from the dependency-first resolved sub-instances.</summary>
    internal sealed record StagingFact(
        ResourceRef<SyntheticBuffer> Host,
        ResourceRef<SyntheticBuffer> Device,
        int Count) : DeclaredFact<SyntheticComposite>
    {
        public SyntheticComposite CreateInstance(IInstanceContext ctx) =>
            new(ctx.Resolve(Host), ctx.Resolve(Device), Count);

        public CleanupStrategy CleanupStrategy { get; }
    }

    /// <summary>
    /// A description that increments the resource it itself resolves to: it advances its own
    /// resource — <c>tx.Self</c> — one epoch via the ordinary Increment verb (no special "advance
    /// myself" verb), then builds that resource's instance directly.
    /// </summary>
    internal sealed record SelfIncrementingDescription(int Size) : IResourceDescription<SyntheticBuffer>
    {
        public DeclaredFact<SyntheticBuffer> Declare(IResourceTransaction tx)
        {
            tx.Increment(tx.Self<SyntheticBuffer>());
            return new SelfIncrementFact(Size);
        }
    }

    /// <summary>Self-increment facts: build the buffer for the resource the description advanced.</summary>
    internal sealed record SelfIncrementFact(int Size) : DeclaredFact<SyntheticBuffer>
    {
        public SyntheticBuffer CreateInstance(IInstanceContext ctx) => new(Size);
        public CleanupStrategy CleanupStrategy { get; }
    }

    /// <summary>
    /// A produce-usage description: it advances the resource it resolves to one epoch and MARKS that
    /// produced increment with the moment it received as a constructor <see cref="Identification"/>.
    /// Marking rides the ordinary Increment (no pass-level marking verb).
    /// </summary>
    internal sealed record ProduceMomentDescription(Identification Moment, int Size)
        : IResourceDescription<SyntheticBuffer>
    {
        public DeclaredFact<SyntheticBuffer> Declare(IResourceTransaction tx)
        {
            tx.Increment(tx.Self<SyntheticBuffer>(), Moment);
            return new SelfIncrementFact(Size);
        }
    }

    /// <summary>
    /// A consume-usage description: it references the moment it received as a constructor
    /// <see cref="Identification"/>. The Link phase binds the reference to the marked increment.
    /// </summary>
    internal sealed record ConsumeMomentDescription(Identification Moment, int Size)
        : IResourceDescription<SyntheticBuffer>
    {
        public DeclaredFact<SyntheticBuffer> Declare(IResourceTransaction tx)
        {
            tx.ReferenceMoment(Moment);
            return new LeafBufferFact(Size);
        }
    }
}
