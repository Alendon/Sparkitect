using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>A push-descriptor composite: a single set (no pool, no allocated set) composed from an ordered <see cref="DescriptorBinding"/> list and pushed inline via <c>VK_KHR_push_descriptor</c> at <c>firstSet:0</c>.</summary>
[PublicAPI]
public sealed class PongDescriptor
{
    private readonly ImmutableArray<DescriptorBinding> _bindings;

    public PongDescriptor(VkDescriptorSetLayout setLayout, ImmutableArray<DescriptorBinding> bindings)
    {
        SetLayout = setLayout;
        _bindings = bindings;
    }

    /// <summary>The derived set layout (cache-owned); frame-independent, so it is safe to read at Setup.</summary>
    public VkDescriptorSetLayout SetLayout { get; }

    /// <summary>Builds and pushes a write per binding at <c>firstSet:0</c>; each write's info struct lives in stack-local storage kept alive across the push so its pointers never dangle.</summary>
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
