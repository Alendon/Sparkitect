using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkSampler"/>. Defaults describe a trilinear repeating sampler.</summary>
/// <param name="MagFilter">Filter applied when magnifying.</param>
/// <param name="MinFilter">Filter applied when minifying.</param>
/// <param name="MipmapMode">How samples between mip levels are combined.</param>
/// <param name="AddressModeU">Addressing for texture coordinates outside [0,1] along U.</param>
/// <param name="AddressModeV">Addressing for texture coordinates outside [0,1] along V.</param>
/// <param name="AddressModeW">Addressing for texture coordinates outside [0,1] along W.</param>
/// <param name="MipLodBias">Bias added to the computed level-of-detail.</param>
/// <param name="AnisotropyEnable">Whether anisotropic filtering is enabled.</param>
/// <param name="MaxAnisotropy">Maximum anisotropy ratio when <paramref name="AnisotropyEnable"/> is set.</param>
/// <param name="CompareEnable">Whether sampled values are compared against a reference (for shadow maps).</param>
/// <param name="CompareOp">The comparison operator used when <paramref name="CompareEnable"/> is set.</param>
/// <param name="MinLod">Lower clamp on the level-of-detail.</param>
/// <param name="MaxLod">Upper clamp on the level-of-detail.</param>
/// <param name="BorderColor">Border color used with clamp-to-border addressing.</param>
/// <param name="UnnormalizedCoordinates">Whether texel coordinates are addressed unnormalized.</param>
[PublicAPI]
public sealed record VkSamplerCreateOptions(
    Filter MagFilter = Filter.Linear,
    Filter MinFilter = Filter.Linear,
    SamplerMipmapMode MipmapMode = SamplerMipmapMode.Linear,
    SamplerAddressMode AddressModeU = SamplerAddressMode.Repeat,
    SamplerAddressMode AddressModeV = SamplerAddressMode.Repeat,
    SamplerAddressMode AddressModeW = SamplerAddressMode.Repeat,
    float MipLodBias = 0f,
    bool AnisotropyEnable = false,
    float MaxAnisotropy = 1f,
    bool CompareEnable = false,
    CompareOp CompareOp = CompareOp.Never,
    float MinLod = 0f,
    float MaxLod = 0f,
    BorderColor BorderColor = BorderColor.FloatTransparentBlack,
    bool UnnormalizedCoordinates = false);
