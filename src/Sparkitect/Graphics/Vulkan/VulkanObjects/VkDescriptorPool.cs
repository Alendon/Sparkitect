using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkDescriptorPool : VulkanObject
{
    private readonly HashSet<VkDescriptorSet> _allocatedSets = [];

    internal VkDescriptorPool(DescriptorPool handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public DescriptorPool Handle { get; }

    public unsafe VkResult<VkDescriptorSet> AllocateDescriptorSet(DescriptorSetLayout layout)
    {
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = Handle,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        var result = Vk.AllocateDescriptorSets(Device, allocInfo, out var descriptorSet);
        if (result != Result.Success) return VkResult<VkDescriptorSet>._Error(result);

        var set = new VkDescriptorSet(descriptorSet, VulkanContext, this);
        _allocatedSets.Add(set);
        return VkResult<VkDescriptorSet>._Success(set);
    }

    public unsafe VkResult<VkDescriptorSet[]> AllocateDescriptorSets(ReadOnlySpan<DescriptorSetLayout> layouts)
    {
        if (layouts.Length == 0) return VkResult<VkDescriptorSet[]>._Success([]);

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
                var result = Vk.AllocateDescriptorSets(Device, allocInfo, handlesPtr);
                if (result != Result.Success) return VkResult<VkDescriptorSet[]>._Error(result);
            }

            var sets = new VkDescriptorSet[layouts.Length];
            for (var i = 0; i < layouts.Length; i++)
            {
                sets[i] = new VkDescriptorSet(handles[i], VulkanContext, this);
                _allocatedSets.Add(sets[i]);
            }

            return VkResult<VkDescriptorSet[]>._Success(sets);
        }
    }

    public Result Reset(uint flags = 0)
    {
        foreach (var set in _allocatedSets)
            set.MarkDisposed();
        _allocatedSets.Clear();

        return Vk.ResetDescriptorPool(Device, Handle, flags);
    }

    public override unsafe void Destroy()
    {
        foreach (var set in _allocatedSets)
            set.MarkDisposed();
        _allocatedSets.Clear();

        Vk.DestroyDescriptorPool(Device, Handle, AllocationCallbacks);
    }
}
