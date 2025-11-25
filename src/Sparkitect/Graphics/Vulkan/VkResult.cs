using Silk.NET.Vulkan;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.Vulkan;

[DiscriminatedUnion]
public abstract partial record VkResult<TResultObject>
{
    public sealed record Success(TResultObject value) : VkResult<TResultObject>;
    
    public sealed record Error(Result errorResult) : VkResult<TResultObject>;
}