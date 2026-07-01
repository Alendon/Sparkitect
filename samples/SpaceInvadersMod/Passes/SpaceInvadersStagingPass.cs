using System.Runtime.InteropServices;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Resources;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Modding.IDs;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// Staging pass: memcpys the pushed CPU entity snapshot (the <c>entities_raw</c> read view) into the staging
/// host buffer, copies host-&gt;device (the X:0-&gt;X:1 staging advance), and publishes the device-buffer
/// composite by sealing its element count on the <c>entities_gpu</c> moment. Its data-flow position — reading
/// the pushed snapshot, producing the composite the compute pass consumes — derives all ordering; the
/// transfer-&gt;compute barrier lives in the consuming read view's pre-execute hook, so this pass records none.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_staging")]
internal sealed partial class SpaceInvadersStagingPass : ComputePass
{
    private IGraphResource<EntitiesRawReadView> _snapshot = null!;
    private IGraphResource<StagingBuffer> _staging = null!;
    private IGraphResource<EntityListResource> _entities = null!;

    public override void Setup(ISetupContext ctx)
    {
        // The pushed CPU snapshot the ECS published through the external-push door.
        _snapshot = ctx.Use(new EntitiesRawReadViewDescription(GraphMomentID.SpaceInvadersMod.EntitiesRaw));

        // Host+device staging pair; the device increment is the X:0->X:1 staging-copy advance.
        var staging = new StagingDescription();
        _staging = ctx.Use(staging);

        // Publish the device buffer as the entities_gpu composite (Variant A: thread the staged device ref).
        _entities = ctx.Use(new EntityListResourceDescription(
            staging.PopulatedBuffer, GraphMomentID.SpaceInvadersMod.EntitiesGpu));
    }

    public override unsafe void Execute(VkCommandBuffer commandBuffer)
    {
        var snapshot = _snapshot.Fetch().Entities;
        var staging = _staging.Fetch();
        var count = snapshot.Length;

        if (count > 0)
        {
            var source = MemoryMarshal.AsBytes(snapshot);
            var destination = new Span<byte>((void*)staging.Host.MappedData, source.Length);
            source.CopyTo(destination);

            commandBuffer.CopyBuffer(staging.Host.Backing, staging.Device.Backing, (ulong)source.Length);
        }

        // Materialize + seal the count on the published composite (epoch 1); the compute pass reads it off here.
        _entities.Fetch().SetCount(count);
    }
}
