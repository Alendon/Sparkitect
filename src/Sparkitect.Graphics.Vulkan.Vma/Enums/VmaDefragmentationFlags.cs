namespace Sparkitect.Graphics.Vulkan.Vma;

[Flags]
public enum VmaDefragmentationFlags
{
    None = 0,
    AlgorithmFast = 1 << 0,
    AlgorithmBalanced = 1 << 1,
    AlgorithmFull = 1 << 2,
    AlgorithmExtensive = 1 << 3,
}
