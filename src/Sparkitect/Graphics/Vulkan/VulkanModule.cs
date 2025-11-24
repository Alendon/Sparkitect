using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Graphics.Vulkan;

[ModuleRegistry.RegisterModule("vulkan")]
public partial class VulkanModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];
    public static Identification Identification => StateModuleID.Sparkitect.Vulkan;
}