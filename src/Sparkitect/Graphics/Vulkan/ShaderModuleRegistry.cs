using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

[Registry(Identifier = "shader_module")]
[UseResourceFile(Key = "module", Required = true, Primary = true)]
[PublicAPI]
public partial class ShaderModuleRegistry(IShaderManager shaderManager) : IRegistry<VulkanModule>
{
    [RegistryMethod]
    public void RegisterShaderModule(Identification id)
    {
        shaderManager.RegisterModule(id);
    }

    public void Unregister(Identification id)
    {
        shaderManager.UnregisterModule(id);
    }

    public static string Identifier => "shader_module";
    public static string ResourceFolder => "shaders";
}
