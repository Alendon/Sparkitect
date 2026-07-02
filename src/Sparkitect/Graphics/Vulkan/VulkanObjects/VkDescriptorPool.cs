using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a descriptor pool and the descriptor sets allocated from it. Resetting or destroying the pool invalidates all of them.</summary>
[PublicAPI]
public class VkDescriptorPool : VulkanObject
{
    private readonly HashSet<VkDescriptorSet> _allocatedSets = [];

    /// <summary>Wraps an existing <see cref="DescriptorPool"/> handle owned by <paramref name="vulkanContext"/>.</summary>
    public VkDescriptorPool(DescriptorPool handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="DescriptorPool"/> handle.</summary>
    public DescriptorPool Handle { get; }

    /// <summary>Allocates a single descriptor set matching <paramref name="layout"/> from this pool.</summary>
    public unsafe Result<VkDescriptorSet, VkApiResult> AllocateDescriptorSet(
        DescriptorSetLayout layout,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = Handle,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        var result = Vk.AllocateDescriptorSets(Device, in allocInfo, out var descriptorSet);
        if (result != VkApiResult.Success) return result;

        var set = new VkDescriptorSet(descriptorSet, VulkanContext, this, callerContext);
        _allocatedSets.Add(set);
        return set;
    }

    /// <summary>Allocates one descriptor set per entry in <paramref name="layouts"/> from this pool.</summary>
    public unsafe Result<VkDescriptorSet[], VkApiResult> AllocateDescriptorSets(
        ReadOnlySpan<DescriptorSetLayout> layouts,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        if (layouts.Length == 0) return Array.Empty<VkDescriptorSet>();

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = Handle,
                DescriptorSetCount = (uint)layouts.Length,
                PSetLayouts = layoutsPtr
            };

            var handles = layouts.Length < 256
                ? stackalloc DescriptorSet[layouts.Length]
                : new DescriptorSet[layouts.Length];

            fixed (DescriptorSet* handlesPtr = handles)
            {
                var result = Vk.AllocateDescriptorSets(Device, in allocInfo, handlesPtr);
                if (result != VkApiResult.Success) return result;
            }

            var sets = new VkDescriptorSet[layouts.Length];
            for (var i = 0; i < layouts.Length; i++)
            {
                sets[i] = new VkDescriptorSet(handles[i], VulkanContext, this, callerContext);
                _allocatedSets.Add(sets[i]);
            }

            return sets;
        }
    }

    /// <summary>Recycles all descriptor sets allocated from this pool, returning them to it.</summary>
    public VkApiResult Reset(uint flags = 0)
    {
        foreach (var set in _allocatedSets)
            set.MarkDisposed();
        _allocatedSets.Clear();

        return Vk.ResetDescriptorPool(Device, Handle, flags);
    }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        foreach (var set in _allocatedSets)
            set.MarkDisposed();
        _allocatedSets.Clear();

        Vk.DestroyDescriptorPool(Device, Handle, AllocationCallbacks);
    }
}
