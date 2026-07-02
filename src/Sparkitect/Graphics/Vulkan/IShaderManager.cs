using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Manages shader module registration and validation.
/// </summary>
[StateFacade<IShaderManagerStateFacade>]
[PublicAPI]
public interface IShaderManager
{
    /// <summary>
    /// Registers a shader module, validating the SPIR-V binary.
    /// </summary>
    /// <param name="id">The identification of the shader module.</param>
    /// <exception cref="InvalidOperationException">Thrown when SPIR-V validation fails.</exception>
    void RegisterModule(Identification id);

    /// <summary>Returns the registered shader module for <paramref name="id"/>, or false if none is registered.</summary>
    bool TryGetRegisteredShaderModule(Identification id, [NotNullWhen(true)] out VkShaderModule? shaderModule);

    /// <summary>
    /// Unregisters a shader module.
    /// </summary>
    /// <param name="id">The identification of the shader module.</param>
    void UnregisterModule(Identification id);
}

/// <summary>Generated state-facade surface for <see cref="IShaderManager"/>.</summary>
[FacadeFor<IShaderManager>]
[PublicAPI]
public interface IShaderManagerStateFacade;