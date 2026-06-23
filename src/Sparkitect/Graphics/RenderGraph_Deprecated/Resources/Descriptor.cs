using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// A push-descriptor graph resource: a single descriptor set (no pool, no allocated set) composed
/// from an ordered list of <see cref="DescriptorBinding"/>s. Its <see cref="SetLayout"/> is derived
/// at Declare from each bound view's static <see cref="IDescriptorBindingSource.DescriptorType"/> and
/// is frame-independent — safe to read at Setup for the pass-owned pipeline layout. At Execute the
/// pass calls <see cref="Push"/>, which builds a <see cref="WriteDescriptorSet"/> from each view's
/// <see cref="IDescriptorBindingSource.DescribeBinding"/> payload and pushes them inline via
/// <c>VK_KHR_push_descriptor</c> at <c>firstSet:0</c>.
/// </summary>
[ResourceManager<DescriptorResourceManager>]
[GraphResourceRegistry.RegisterResource("descriptor")]
[PublicAPI]
public sealed partial class Descriptor : IHasIdentification
{
    private readonly ImmutableArray<DescriptorBinding> _bindings;

    internal Descriptor(VkDescriptorSetLayout setLayout, ImmutableArray<DescriptorBinding> bindings)
    {
        SetLayout = setLayout;
        _bindings = bindings;
    }

    /// <summary>
    /// The derived descriptor-set layout (created with
    /// <see cref="DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr"/>). Frame-independent and
    /// populated at Declare, so it is safe to read at Setup when assembling the pass-owned pipeline
    /// layout.
    /// </summary>
    public VkDescriptorSetLayout SetLayout { get; }

    /// <summary>
    /// Builds a write for each binding from its view's Execute-time payload and pushes them inline
    /// into <paramref name="cmd"/> at <c>firstSet:0</c>. Each write's resource-info struct lives in a
    /// caller-owned <see cref="DescriptorBindingPayload.WriteInfoStorage"/> slot that stays alive
    /// (on this stack frame) across the <c>vkCmdPushDescriptorSet</c> call.
    /// </summary>
    public void Push(VkCommandBuffer cmd, VkPipelineLayout pipelineLayout, PipelineBindPoint bindPoint)
    {
        var count = _bindings.Length;
        if (count == 0)
            return;

        // One info-storage slot per binding, kept alive on this frame across the push so the
        // WriteDescriptorSet pointers (PImageInfo/PBufferInfo) never dangle (54-02 contract).
        Span<DescriptorBindingPayload.WriteInfoStorage> storage =
            count <= 16
                ? stackalloc DescriptorBindingPayload.WriteInfoStorage[count]
                : new DescriptorBindingPayload.WriteInfoStorage[count];
        Span<WriteDescriptorSet> writes =
            count <= 16
                ? stackalloc WriteDescriptorSet[count]
                : new WriteDescriptorSet[count];

        for (var i = 0; i < count; i++)
        {
            var binding = _bindings[i];
            var payload = binding.View.Fetch().DescribeBinding();
            writes[i] = payload.ToWrite(binding.Binding, ref storage[i]);
        }

        cmd.PushDescriptorSet(bindPoint, pipelineLayout, firstSet: 0, writes);
    }
}
