using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

[StateService<IShaderManager, VulkanModule>]
internal class ShaderManager : IShaderManager
{
    private const uint SpirvMagicNumber = 0x07230203;
    private const string ShaderResourceKey = "module";

    public required IResourceManager ResourceManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }

    private readonly Dictionary<Identification, VkShaderModule> _loadedModules = new();

    public unsafe void RegisterModule(Identification id)
    {
        using var stream = ResourceManager.GetResourceStream(id, ShaderResourceKey);
        if (stream is null)
            throw new InvalidOperationException($"Shader resource not found for {id}");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        ValidateSpirvBinary(bytes, id);

        Span<uint> spirvCode = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan());

        Result result;
        ShaderModule module;
        fixed(uint* codeStart = spirvCode)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                PCode = codeStart,
                CodeSize = (UIntPtr)spirvCode.Length
            };
            result = VulkanContext.VkApi.CreateShaderModule(VulkanContext.VkDevice.Handle, in createInfo,
                VulkanContext.DefaultAllocationCallbacks, out module);
        }

        if (result != Result.Success)
        {
            throw new Exception($"Failed to create Shader Module for {id}, Reason: {result}");
        }
        
        _loadedModules[id] = new VkShaderModule(module, VulkanContext);
    }

    public bool TryGetRegisteredShaderModule(Identification id, [NotNullWhen(true)] out VkShaderModule? shaderModule)
    {
        return _loadedModules.TryGetValue(id, out shaderModule);
    }

    public void UnregisterModule(Identification id)
    {
        _loadedModules.Remove(id);
    }

    private static void ValidateSpirvBinary(byte[] bytes, Identification id)
    {
        if (bytes.Length < 4)
            throw new InvalidOperationException($"Shader {id}: SPIR-V binary too small ({bytes.Length} bytes)");

        if (bytes.Length % 4 != 0)
            throw new InvalidOperationException(
                $"Shader {id}: SPIR-V binary length ({bytes.Length}) must be a multiple of 4 bytes");

        var magic = BitConverter.ToUInt32(bytes, 0);
        if (magic != SpirvMagicNumber)
            throw new InvalidOperationException(
                $"Shader {id}: Invalid SPIR-V magic number (expected 0x{SpirvMagicNumber:X8}, got 0x{magic:X8})");

        Log.Debug("Validated spirv binary for {ShaderModuleId}", id);
    }
}