namespace Sparkitect.Graphics.Vulkan.Vma;

[Flags]
public enum VmaPoolCreateFlags
{
    None = 0,
    IgnoreBufferImageGranularity = 1 << 1,
    LinearAlgorithm = 1 << 2,
}
