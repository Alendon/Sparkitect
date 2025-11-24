using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.Vma;

public readonly struct VmaAllocationInfo
{
    public uint MemoryType { get; }
    public DeviceMemory DeviceMemory { get; }
    public ulong Offset { get; }
    public ulong Size { get; }
    public nint MappedData { get; }

    internal VmaAllocationInfo(uint memoryType, DeviceMemory deviceMemory, ulong offset, ulong size, nint mappedData)
    {
        MemoryType = memoryType;
        DeviceMemory = deviceMemory;
        Offset = offset;
        Size = size;
        MappedData = mappedData;
    }
}
