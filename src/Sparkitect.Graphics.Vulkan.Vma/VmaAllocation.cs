namespace Sparkitect.Graphics.Vulkan.Vma;

public readonly struct VmaAllocation : IEquatable<VmaAllocation>
{
    internal nint Handle { get; }

    internal VmaAllocation(nint handle) => Handle = handle;

    public bool IsNull => Handle == 0;

    public bool Equals(VmaAllocation other) => Handle == other.Handle;
    public override bool Equals(object? obj) => obj is VmaAllocation other && Equals(other);
    public override int GetHashCode() => Handle.GetHashCode();

    public static bool operator ==(VmaAllocation left, VmaAllocation right) => left.Equals(right);
    public static bool operator !=(VmaAllocation left, VmaAllocation right) => !left.Equals(right);
}
