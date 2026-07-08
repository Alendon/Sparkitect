using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>State module that drives Vulkan lifecycle: instance, physical device, logical device, and shader-module processing.</summary>
[ModuleRegistry.RegisterModule("vulkan")]
[PublicAPI]
public partial class VulkanModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires => [StateModuleID.Sparkitect.Event];

    /// <summary>Initializes the Vulkan context (loads the API and allocation callbacks).</summary>
    [TransitionFunction("vulkan_init")]
    [OnCreateScheduling]
    public static void VulkanInit(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Initialize();
    }

    /// <summary>Creates the Vulkan instance after configurators have run.</summary>
    [TransitionFunction("create_instance")]
    [OnCreateScheduling]
    [OrderAfter<VulkanInitFunc>]
    public static void CreateInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateInstance();
    }

    /// <summary>Selects the physical device to use.</summary>
    [TransitionFunction("select_physical_device")]
    [OnCreateScheduling]
    [OrderAfter<CreateInstanceFunc>]
    public static void SelectPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.SelectPhysicalDevice();
    }

    /// <summary>Creates the logical device and retrieves its queues.</summary>
    [TransitionFunction("create_device")]
    [OnCreateScheduling]
    [OrderAfter<SelectPhysicalDeviceFunc>]
    public static void CreateDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.CreateDevice();
    }

    /// <summary>Begins teardown by waiting for the device to become idle.</summary>
    [TransitionFunction("begin_vulkan_teardown")]
    [OnDestroyScheduling]
    public static void BeginVulkanTeardown(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.BeginVulkanTeardown();
    }

    /// <summary>Processes newly registered shader modules on frame enter.</summary>
    [TransitionFunction("process_shader_module_registry_enter")]
    [OnFrameEnterScheduling]
    [OrderAfter<CreateDeviceFunc>]
    public static void ProcessShaderModuleRegistryEnter(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ShaderModuleRegistry, VulkanModule>();
    }

    /// <summary>Disposes shader-module handles at shutdown while the device is still valid.</summary>
    [TransitionFunction("process_shader_module_registry_exit")]
    [OnFrameExitScheduling]
    [OrderAfter<BeginVulkanTeardownFunc>]
    [OrderBefore<DestroyDeviceFunc>]
    public static void ProcessShaderModuleRegistryExit(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ShaderModuleRegistry, VulkanModule>();
    }

    /// <summary>Destroys the logical device.</summary>
    [TransitionFunction("destroy_device")]
    [OnDestroyScheduling]
    [OrderAfter<BeginVulkanTeardownFunc>]
    public static void DestroyDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyDevice();
    }

    /// <summary>Releases the selected physical device.</summary>
    [TransitionFunction("destroy_physical_device")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyDeviceFunc>]
    public static void DestroyPhysicalDevice(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyPhysicalDevice();
    }

    /// <summary>Destroys the Vulkan instance.</summary>
    [TransitionFunction("destroy_instance")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyPhysicalDeviceFunc>]
    public static void DestroyInstance(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.DestroyInstance();
    }

    /// <summary>Finalizes Vulkan shutdown and releases the context.</summary>
    [TransitionFunction("vulkan_shutdown")]
    [OnDestroyScheduling]
    [OrderAfter<DestroyInstanceFunc>]
    public static void VulkanShutdown(IVulkanContextStateFacade vulkanContext)
    {
        vulkanContext.Shutdown();
    }
}
