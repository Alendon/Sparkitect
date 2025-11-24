using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.Vma;

public record struct VmaAllocationCreateInfo
{
    public VmaAllocationCreateFlags Flags { get; init; }
    public VmaMemoryUsage Usage { get; init; }
    public MemoryPropertyFlags RequiredFlags { get; init; }
    public MemoryPropertyFlags PreferredFlags { get; init; }
    public uint MemoryTypeBits { get; init; }
    public VmaPool? Pool { get; init; }
    public float Priority { get; init; }
}
