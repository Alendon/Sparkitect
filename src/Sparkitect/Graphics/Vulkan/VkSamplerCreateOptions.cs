using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

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
