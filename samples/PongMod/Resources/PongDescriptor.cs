using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>
/// PongMod's push-descriptor composite (D-10): a single descriptor set (no pool, no allocated set)
/// composed from an ordered list of <see cref="DescriptorBinding"/>s. Its <see cref="SetLayout"/> is
/// derived at Declare from each bound view's static descriptor type via the graph-local layout cache
/// (owned by the cache, not the descriptor — D-12) and is frame-independent, so it is safe to read at
/// Setup for the pass-owned pipeline layout (D-11). At Execute the pass calls <see cref="Push"/>, which
/// builds a <see cref="WriteDescriptorSet"/> from each view's <see cref="IDescriptorBindingSource.DescribeBinding"/>
/// payload and pushes them inline via <c>VK_KHR_push_descriptor</c> at <c>firstSet:0</c>. Pong-owned;
/// not generalized (D-13).
/// </summary>
[PublicAPI]
public sealed class PongDescriptor
{
    private readonly ImmutableArray<DescriptorBinding> _bindings;

    /// <summary>Composes the descriptor over its derived <paramref name="setLayout"/> and ordered <paramref name="bindings"/>.</summary>
    public PongDescriptor(VkDescriptorSetLayout setLayout, ImmutableArray<DescriptorBinding> bindings)
    {
        SetLayout = setLayout;
        _bindings = bindings;
    }

    /// <summary>
    /// The derived push-descriptor set layout (cache-owned, created with
    /// <see cref="DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr"/>). Populated at Declare and
    /// frame-independent, so it is safe to read at Setup when assembling the pass-owned pipeline layout.
    /// </summary>
    public VkDescriptorSetLayout SetLayout { get; }

    /// <summary>
    /// Builds a write for each binding from its view's Execute-time payload and pushes them inline into
    /// <paramref name="cmd"/> at <c>firstSet:0</c>. Each write's resource-info struct lives in a
    /// caller-owned <see cref="DescriptorBindingPayload.WriteInfoStorage"/> slot that stays alive (on
    /// this stack frame) across the <c>vkCmdPushDescriptorSet</c> call so its pointers never dangle.
    /// </summary>
    public void Push(VkCommandBuffer cmd, VkPipelineLayout pipelineLayout, PipelineBindPoint bindPoint)
    {
        var count = _bindings.Length;
        if (count == 0)
            return;

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
