using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Manages shader module registration and validation.
/// </summary>
public interface IShaderManager
{
    /// <summary>
    /// Registers a shader module, validating the SPIR-V binary.
    /// </summary>
    /// <param name="id">The identification of the shader module.</param>
    /// <exception cref="InvalidOperationException">Thrown when SPIR-V validation fails.</exception>
    void RegisterModule(Identification id);

    /// <summary>
    /// Unregisters a shader module.
    /// </summary>
    /// <param name="id">The identification of the shader module.</param>
    void UnregisterModule(Identification id);
}
