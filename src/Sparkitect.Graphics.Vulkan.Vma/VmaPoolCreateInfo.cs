namespace Sparkitect.Graphics.Vulkan.Vma;

public record struct VmaPoolCreateInfo
{
    public uint MemoryTypeIndex { get; init; }
    public VmaPoolCreateFlags Flags { get; init; }
    public ulong BlockSize { get; init; }
    public nuint MinBlockCount { get; init; }
    public nuint MaxBlockCount { get; init; }
    public float Priority { get; init; }
}
