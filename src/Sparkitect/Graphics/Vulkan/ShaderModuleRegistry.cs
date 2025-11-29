using Microsoft.Win32;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.Vulkan;

[Registry(Identifier = "shader_module")]
[UseResourceFile(Identifier = "module", Required = true)]
public partial class ShaderModuleRegistry : IRegistry
{

    [RegistryMethod]
    public void RegisterShaderModule(Identification id)
    {
        
    }
    
    public void Unregister(Identification id)
    {
        throw new NotImplementedException();
    }

    public static string Identifier => "shader_module";
}