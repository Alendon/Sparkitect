using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.Vulkan;

[ModuleRegistry.RegisterModule("vulkan")]
public partial class VulkanModule : IStateModule
{
    
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];
    public static Identification Identification => StateModuleID.Sparkitect.Vulkan;

    [TransitionFunction("vulkan_init")]
    [OnCreateScheduling]
    public static void VulkanInit(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Initialize();
    }

    [TransitionFunction("create_instance")]
    [OnCreateScheduling]
    [OrderAfter<VulkanInitFunc>]
    public static void CreateInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateInstance();
    }

    [TransitionFunction("select_physical_device")]
    [OnCreateScheduling]
    [OrderAfter<CreateInstanceFunc>]
    public static void SelectPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.SelectPhysicalDevice();
    }

    [TransitionFunction("create_device")]
    [OnCreateScheduling]
    [OrderAfter<SelectPhysicalDeviceFunc>]
    public static void CreateDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateDevice();
    }

    [TransitionFunction("add_registries")]
    [OnCreateScheduling]
    public static void AddRegistries(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<ShaderModuleRegistry>();
    }

    [TransitionFunction("process_registries")]
    [OnFrameEnterScheduling]
    public static void ProcessRegistries(IRegistryManager registryManager)
    {
        registryManager.ProcessAllMissing<ShaderModuleRegistry>();
    }

    [TransitionFunction("destroy_device")]
    [OnDestroyScheduling]
    public static void DestroyDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyDevice();
    }

    [TransitionFunction("destroy_physical_device")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyDeviceFunc>]
    public static void DestroyPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyPhysicalDevice();
    }

    [TransitionFunction("destroy_instance")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyPhysicalDeviceFunc>]
    public static void DestroyInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyInstance();
    }

    [TransitionFunction("vulkan_shutdown")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyInstanceFunc>]
    public static void VulkanShutdown(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Shutdown();
    }
}