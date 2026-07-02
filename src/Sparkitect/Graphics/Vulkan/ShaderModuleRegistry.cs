using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Registry that loads shader modules from resource files and hands them to the <see cref="IShaderManager"/>.</summary>
/// <param name="shaderManager">The manager that owns the compiled shader-module handles.</param>
[Registry(Identifier = "shader_module")]
[UseResourceFile(Key = "module", Required = true, Primary = true)]
[PublicAPI]
public partial class ShaderModuleRegistry(IShaderManager shaderManager) : IRegistry<VulkanModule>
{
    /// <summary>Registers and validates the shader module identified by <paramref name="id"/>.</summary>
    [RegistryMethod]
    public void RegisterShaderModule(Identification id)
    {
        shaderManager.RegisterModule(id);
    }

    /// <summary>Unregisters and releases the shader module identified by <paramref name="id"/>.</summary>
    public void Unregister(Identification id)
    {
        shaderManager.UnregisterModule(id);
    }

    /// <summary>The registry identifier used in resource files.</summary>
    public static string Identifier => "shader_module";

    /// <summary>The resource subfolder shader modules are loaded from.</summary>
    public static string ResourceFolder => "shaders";
}
