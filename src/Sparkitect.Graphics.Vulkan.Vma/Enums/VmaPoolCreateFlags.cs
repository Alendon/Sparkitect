using JetBrains.Annotations;

namespace Sparkitect.Graphics.Vulkan.Vma;

[Flags]
[PublicAPI]
public enum VmaPoolCreateFlags
{
    None = 0,
    IgnoreBufferImageGranularity = 1 << 1,
    LinearAlgorithm = 1 << 2,
}
