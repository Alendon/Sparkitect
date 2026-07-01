using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.Vulkan;

[ModuleRegistry.RegisterModule("vulkan")]
[PublicAPI]
public partial class VulkanModule : IStateModule
{
    
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

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

    [TransitionFunction("begin_vulkan_teardown")]
    [OnDestroyScheduling]
    public static void BeginVulkanTeardown(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.BeginVulkanTeardown();
    }

    [TransitionFunction("process_shader_module_registry_enter")]
    [OnFrameEnterScheduling]
    [OrderAfter<CreateDeviceFunc>]
    public static void ProcessShaderModuleRegistryEnter(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ShaderModuleRegistry, VulkanModule>();
    }

    // Teardown on the generation path, with the device still valid: after wait-idle, before device destroy.
    // This disposes VkShaderModule handles at shutdown (closes the shader-module leak).
    [TransitionFunction("process_shader_module_registry_exit")]
    [OnFrameExitScheduling]
    [OrderAfter<BeginVulkanTeardownFunc>]
    [OrderBefore<DestroyDeviceFunc>]
    public static void ProcessShaderModuleRegistryExit(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ShaderModuleRegistry, VulkanModule>();
    }

    [TransitionFunction("destroy_device")]
    [OnDestroyScheduling]
    [OrderAfter<BeginVulkanTeardownFunc>]
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