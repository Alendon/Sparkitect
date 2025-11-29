using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.Vulkan;

[ModuleRegistry.RegisterModule("vulkan")]
public partial class VulkanModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];
    public static Identification Identification => StateModuleID.Sparkitect.Vulkan;

    [StateFunction("vulkan_init")]
    [OnCreate]
    public static void VulkanInit(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Initialize();
    }

    [StateFunction("create_instance")]
    [OnCreate]
    [OrderAfter("vulkan_init")]
    [OrderAfter<WindowingModule>("create_window")]
    public static void CreateInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateInstance();
    }

    [StateFunction("select_physical_device")]
    [OnCreate]
    [OrderAfter("create_instance")]
    public static void SelectPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.SelectPhysicalDevice();
    }

    [StateFunction("create_device")]
    [OnCreate]
    [OrderAfter("select_physical_device")]
    public static void CreateDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateDevice();
    }

    [StateFunction("destroy_device")]
    [OnDestroy]
    public static void DestroyDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyDevice();
    }

    [StateFunction("destroy_physical_device")]
    [OnDestroy]
    [OrderAfter("destroy_device")]
    public static void DestroyPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyPhysicalDevice();
    }

    [StateFunction("destroy_instance")]
    [OnDestroy]
    [OrderAfter("destroy_physical_device")]
    public static void DestroyInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyInstance();
    }

    [StateFunction("vulkan_shutdown")]
    [OnDestroy]
    [OrderAfter("destroy_instance")]
    public static void VulkanShutdown(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Shutdown();
    }
}