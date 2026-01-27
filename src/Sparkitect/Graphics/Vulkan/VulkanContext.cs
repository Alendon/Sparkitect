using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Serilog;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.Vulkan;

[StateService<IVulkanContext, VulkanModule>]
public unsafe class VulkanContext : IVulkanContext, IVulkanContextStateFacade
{
    private const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";
    private const string DebugUtilsExtensionName = "VK_EXT_debug_utils";

    public Vk VkApi { get; private set; } = null!;
    public VkInstance VkInstance { get; private set; } = null!;
    public VkPhysicalDevice VkPhysicalDevice { get; private set; } = null!;
    public VkDevice VkDevice { get; private set; } = null!;
    public unsafe AllocationCallbacks* DefaultAllocationCallbacks { get; }
    public IObjectTracker<VulkanObject> ObjectTracker { get; private set; } = null!;

    private readonly Dictionary<uint, List<VulkanQueue>> _queuesByFamily = [];
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private KhrSurface? _khrSurface;

    public required IModDIService ModDIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required ICliArgumentHandler CliArgumentHandler { private get; init; }

    // Optional - present if WindowingModule is active
    public required IWindowManager? WindowManager { private get; init; }

    private bool ValidationEnabled() => true;

    public void Initialize()
    {
        VkApi = Vk.GetApi();
        ObjectTracker = new ObjectTracker<VulkanObject>();
    }

    public unsafe void CreateInstance()
    {
        var configContext = new VulkanInstanceConfigurationContext(VkApi);

        using var configuratorContainer =
            ModDIService.CreateEntrypointContainer<IVulkanInstanceConfigurator>(GameStateManager.LoadedMods);
        configuratorContainer.ProcessMany(c => c.Configure(configContext));

        var validationEnabled = ValidationEnabled();
        if (validationEnabled)
        {
            if (configContext.IsLayerAvailable(ValidationLayerName))
            {
                configContext.AddLayer(ValidationLayerName);
                Log.Debug("Validation layer enabled: {Layer}", ValidationLayerName);
            }
            else
            {
                Log.Warning("Validation requested but layer {Layer} not available", ValidationLayerName);
                validationEnabled = false;
            }

            if (validationEnabled && configContext.IsExtensionAvailable(DebugUtilsExtensionName))
            {
                configContext.AddExtension(DebugUtilsExtensionName);
            }
        }

        // Add window surface extensions if windowing module is active
        if (WindowManager != null)
        {
            var windowExtensions = WindowManager.GetRequiredVulkanExtensions();
            foreach (var ext in windowExtensions)
            {
                if (configContext.IsExtensionAvailable(ext))
                {
                    configContext.AddExtension(ext);
                    Log.Debug("Added window extension: {Extension}", ext);
                }
                else
                {
                    Log.Warning("Window requires extension {Extension} which is not available", ext);
                }
            }
        }

        var enabledLayers = configContext.GetEnabledLayers();
        var enabledExtensions = configContext.GetEnabledExtensions();

        var applicationName = SilkMarshal.StringToPtr("Sparkitect");
        var engineName = SilkMarshal.StringToPtr("Sparkitect");

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)applicationName,
            ApplicationVersion = new Version32(0, 1, 0),
            PEngineName = (byte*)engineName,
            EngineVersion = new Version32(0, 1, 0),
            ApiVersion = Vk.Version13
        };

        var layerPtr = SilkMarshal.StringArrayToPtr(enabledLayers);
        var extensionPtr = SilkMarshal.StringArrayToPtr(enabledExtensions);

        try
        {
            var instanceCreateInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledLayerCount = (uint)enabledLayers.Count,
                PpEnabledLayerNames = (byte**)layerPtr,
                EnabledExtensionCount = (uint)enabledExtensions.Count,
                PpEnabledExtensionNames = (byte**)extensionPtr
            };

            var result = VkApi.CreateInstance(instanceCreateInfo, DefaultAllocationCallbacks, out var instance);
            if (result != Result.Success)
                throw new InvalidOperationException($"Failed to create Vulkan instance: {result}");

            VkInstance = new VkInstance(instance, this);

            if (validationEnabled)
                SetupDebugMessenger();

            // Initialize KhrSurface extension if windowing module is active
            if (WindowManager != null)
            {
                if (VkApi.TryGetInstanceExtension(instance, out _khrSurface))
                {
                    Log.Debug("KHR_surface extension loaded");
                }
            }
        }
        finally
        {
            SilkMarshal.Free(applicationName);
            SilkMarshal.Free(engineName);
            SilkMarshal.Free(layerPtr);
            SilkMarshal.Free(extensionPtr);
        }
    }

    private unsafe void SetupDebugMessenger()
    {
        if (!VkApi.TryGetInstanceExtension(VkInstance.Handle, out _debugUtils))
        {
            Log.Warning("Failed to get debug utils extension");
            return;
        }

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.InfoBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                          DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                          DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback)
        };

        var result = _debugUtils!.CreateDebugUtilsMessenger(VkInstance.Handle, createInfo, DefaultAllocationCallbacks,
            out _debugMessenger);
        if (result != Result.Success)
        {
            Log.Warning("Failed to create debug messenger: {Result}", result);
            _debugUtils = null;
        }
        else
        {
            Log.Debug("Vulkan debug messenger created");
        }
    }

    private static unsafe uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        var message = SilkMarshal.PtrToString((nint)pCallbackData->PMessage) ?? "Unknown";

        switch (messageSeverity)
        {
            case DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt:
                Log.Error("[Vulkan] {Message}", message);
                break;
            case DebugUtilsMessageSeverityFlagsEXT.WarningBitExt:
                Log.Warning("[Vulkan] {Message}", message);
                break;
            case DebugUtilsMessageSeverityFlagsEXT.InfoBitExt:
                Log.Information("[Vulkan] {Message}", message);
                break;
            default:
                Log.Verbose("[Vulkan] {Message}", message);
                break;
        }

        return Vk.False;
    }

    public void SelectPhysicalDevice()
    {
        var devices = VkInstance.EnumeratePhysicalDevices();
        if (devices.Length == 0)
            throw new InvalidOperationException("No physical devices found");

        using var selectorContainer =
            ModDIService.CreateEntrypointContainer<IVulkanPhysicalDeviceSelector>(GameStateManager.LoadedMods);
        var selectors = selectorContainer.ResolveMany();

        PhysicalDevice bestDevice = default;
        var bestScore = int.MinValue;

        foreach (var device in devices)
        {
            var context = new VulkanPhysicalDeviceSelectionContext(VkApi, device);
            var score = ComputeDeviceScore(context, selectors);

            if (score > bestScore)
            {
                bestScore = score;
                bestDevice = device;
            }
        }

        if (bestDevice.Handle == 0)
            throw new InvalidOperationException("No suitable physical device found");

        VkPhysicalDevice = new VkPhysicalDevice(this, bestDevice);
    }

    private static int ComputeDeviceScore(VulkanPhysicalDeviceSelectionContext context,
        IReadOnlyList<IVulkanPhysicalDeviceSelector> selectors)
    {
        foreach (var selector in selectors)
        {
            var score = selector.ScoreDevice(context);
            if (score.HasValue)
                return score.Value;
        }

        return context.Properties.DeviceType switch
        {
            PhysicalDeviceType.DiscreteGpu => 3,
            PhysicalDeviceType.IntegratedGpu => 2,
            PhysicalDeviceType.VirtualGpu => 1,
            _ => 0
        };
    }

    public unsafe void CreateDevice()
    {
        var configContext = new VulkanDeviceConfigurationContext(VkApi, VkPhysicalDevice.PhysicalDevice);

        using var configuratorContainer =
            ModDIService.CreateEntrypointContainer<IVulkanDeviceConfigurator>(GameStateManager.LoadedMods);
        configuratorContainer.ProcessMany(c => c.Configure(configContext));

        // Add swapchain extension if windowing module is active
        if (WindowManager is not null)
        {
            if (!configContext.AddExtension("VK_KHR_swapchain"))
                Log.Warning("VK_KHR_swapchain extension not available");
        }

        var queueRequests = configContext.GetQueueRequests();
        if (queueRequests.Count == 0)
        {

            if (WindowManager is not null)
            {
                var graphicsQueueFamily = configContext.FindQueueFamily(QueueFlags.GraphicsBit)
                                          ?? throw new InvalidOperationException("No graphics queue family found");
                configContext.RequestQueues(graphicsQueueFamily, 1);
                var transferQueueFamily = configContext.FindQueueFamily(QueueFlags.TransferBit)
                                          ?? throw new InvalidOperationException("No graphics queue family found");
                configContext.RequestQueues(transferQueueFamily, 1);
            }
            
            var computeQueueFamily = configContext.FindQueueFamily(QueueFlags.ComputeBit)
                                     ?? throw new InvalidOperationException("No graphics queue family found");
            configContext.RequestQueues(computeQueueFamily, 1);
            
            
            queueRequests = configContext.GetQueueRequests();
        }

        var enabledExtensions = configContext.GetEnabledExtensions();

        // Build pNext chain: Features2 -> 13 -> 12 -> 11
        var features13 = configContext.EnabledFeatures13;
        var features12 = configContext.EnabledFeatures12;
        var features11 = configContext.EnabledFeatures11;
        features12.PNext = &features13;
        features11.PNext = &features12;

        var features2 = new PhysicalDeviceFeatures2
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            Features = configContext.EnabledFeatures,
            PNext = &features11
        };

        // Pin priorities and build queue infos
        var handles = new GCHandle[queueRequests.Count];
        var queueCreateInfos = new DeviceQueueCreateInfo[queueRequests.Count];

        try
        {
            for (var i = 0; i < queueRequests.Count; i++)
            {
                var request = queueRequests[i];
                handles[i] = GCHandle.Alloc(request.Priorities, GCHandleType.Pinned);
                queueCreateInfos[i] = new DeviceQueueCreateInfo
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = request.QueueFamilyIndex,
                    QueueCount = request.QueueCount,
                    PQueuePriorities = (float*)handles[i].AddrOfPinnedObject()
                };
            }

            var extensionPtr = SilkMarshal.StringArrayToPtr(enabledExtensions);
            try
            {
                fixed (DeviceQueueCreateInfo* queueInfoPtr = queueCreateInfos)
                {
                    var deviceCreateInfo = new DeviceCreateInfo
                    {
                        SType = StructureType.DeviceCreateInfo,
                        PNext = &features2,
                        QueueCreateInfoCount = (uint)queueRequests.Count,
                        PQueueCreateInfos = queueInfoPtr,
                        EnabledExtensionCount = (uint)enabledExtensions.Count,
                        PpEnabledExtensionNames = (byte**)extensionPtr
                    };

                    var result = VkApi.CreateDevice(VkPhysicalDevice.PhysicalDevice, deviceCreateInfo,
                        DefaultAllocationCallbacks, out var device);
                    if (result != Result.Success)
                        throw new InvalidOperationException($"Failed to create Vulkan device: {result}");

                    VkDevice = new VkDevice(this, device);
                    Log.Debug("Vulkan device created with {QueueCount} queue families, {ExtCount} extensions",
                        queueRequests.Count, enabledExtensions.Count);
                }
            }
            finally
            {
                SilkMarshal.Free(extensionPtr);
            }
        }
        finally
        {
            foreach (var handle in handles)
                if (handle.IsAllocated)
                    handle.Free();
        }

        RetrieveQueues(queueRequests, configContext.QueueFamilyProperties);
        
        
    }

    public VkResult<VkCommandPool> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex)
    {
        CommandPoolCreateInfo createInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = flags,
            QueueFamilyIndex = queueFamilyIndex
        };

        var result = VkApi.CreateCommandPool(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var pool);

        if (result != Result.Success || pool.Handle == 0) return VkResult<VkCommandPool>._Error(result);

        return VkResult<VkCommandPool>._Success(new VkCommandPool(pool, this));
    }

    public unsafe VkResult<VkDescriptorPool> CreateDescriptorPool(in DescriptorPoolCreateInfo createInfo)
    {
        fixed (DescriptorPoolCreateInfo* infoPtr = &createInfo)
        {
            var result = VkApi.CreateDescriptorPool(VkDevice.Handle, infoPtr, DefaultAllocationCallbacks, out var pool);
            if (result != Result.Success) return VkResult<VkDescriptorPool>._Error(result);
            return VkResult<VkDescriptorPool>._Success(new VkDescriptorPool(pool, this));
        }
    }

    public VkResult<VkSemaphore> CreateSemaphore(SemaphoreCreateFlags flags = 0)
    {
        var createInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
            Flags = flags
        };
        var result = VkApi.CreateSemaphore(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var semaphore);
        if (result != Result.Success) return VkResult<VkSemaphore>._Error(result);
        return VkResult<VkSemaphore>._Success(new VkSemaphore(semaphore, this));
    }

    public VkResult<VkFence> CreateFence(FenceCreateFlags flags = 0)
    {
        var createInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = flags
        };
        var result = VkApi.CreateFence(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var fence);
        if (result != Result.Success) return VkResult<VkFence>._Error(result);
        return VkResult<VkFence>._Success(new VkFence(fence, this));
    }

    public VkResult<VkDescriptorSetLayout> CreateDescriptorSetLayout(in DescriptorSetLayoutCreateInfo createInfo)
    {
        fixed (DescriptorSetLayoutCreateInfo* infoPtr = &createInfo)
        {
            var result = VkApi.CreateDescriptorSetLayout(VkDevice.Handle, infoPtr, DefaultAllocationCallbacks, out var layout);
            if (result != Result.Success) return VkResult<VkDescriptorSetLayout>._Error(result);
            return VkResult<VkDescriptorSetLayout>._Success(new VkDescriptorSetLayout(layout, this));
        }
    }

    public VkResult<VkPipelineLayout> CreatePipelineLayout(in PipelineLayoutCreateInfo createInfo)
    {
        fixed (PipelineLayoutCreateInfo* infoPtr = &createInfo)
        {
            var result = VkApi.CreatePipelineLayout(VkDevice.Handle, infoPtr, DefaultAllocationCallbacks, out var layout);
            if (result != Result.Success) return VkResult<VkPipelineLayout>._Error(result);
            return VkResult<VkPipelineLayout>._Success(new VkPipelineLayout(layout, this));
        }
    }

    public VkResult<VkPipeline> CreateComputePipeline(in ComputePipelineCreateInfo createInfo)
    {
        fixed (ComputePipelineCreateInfo* infoPtr = &createInfo)
        {
            var result = VkApi.CreateComputePipelines(VkDevice.Handle, default, 1, infoPtr, DefaultAllocationCallbacks, out var pipeline);
            if (result != Result.Success) return VkResult<VkPipeline>._Error(result);
            return VkResult<VkPipeline>._Success(new VkPipeline(pipeline, this));
        }
    }

    public unsafe VkSurface? CreateSurface(IWindow window)
    {
        if (window.VkSurface == null || _khrSurface == null)
        {
            Log.Warning("Cannot create surface: window has no VkSurface or KHR_surface not loaded");
            return null;
        }

        var rawHandle = window.VkSurface.Create<AllocationCallbacks>(
            VkInstance.Handle.ToHandle(), null);

        if (rawHandle.Handle == 0)
        {
            Log.Error("Failed to create Vulkan surface from window");
            return null;
        }

        var surfaceHandle = new SurfaceKHR(rawHandle.Handle);

        Log.Information("Vulkan surface created");
        return new VkSurface(surfaceHandle, _khrSurface, this);
    }

    private void RetrieveQueues(IReadOnlyList<QueueFamilyRequest> requests,
        IReadOnlyList<QueueFamilyProperties> familyProperties)
    {
        foreach (var request in requests)
        {
            var capabilities = familyProperties[(int)request.QueueFamilyIndex].QueueFlags;
            var queues = new List<VulkanQueue>((int)request.QueueCount);

            for (uint i = 0; i < request.QueueCount; i++)
            {
                var handle = VkDevice.GetQueue(request.QueueFamilyIndex, i);
                queues.Add(new VulkanQueue(handle, request.QueueFamilyIndex, i, capabilities));
            }

            _queuesByFamily[request.QueueFamilyIndex] = queues;
        }

        Log.Debug("Retrieved {QueueCount} queue(s) across {FamilyCount} family(s)",
            _queuesByFamily.Values.Sum(q => q.Count), _queuesByFamily.Count);
    }

    public VulkanQueue? GetQueue(uint familyIndex, uint queueIndex)
    {
        return _queuesByFamily.TryGetValue(familyIndex, out var queues)
            ? queues.FirstOrDefault(q => q.QueueIndex == queueIndex)
            : null;
    }

    public IReadOnlyList<VulkanQueue> GetQueuesForFamily(uint familyIndex)
    {
        return _queuesByFamily.TryGetValue(familyIndex, out var queues) ? queues : [];
    }

    public void DestroyDevice()
    {
        _queuesByFamily.Clear();
        VkDevice?.WaitIdle();
        VkDevice?.Dispose();
        VkDevice = null!;
    }

    public void DestroyPhysicalDevice()
    {
        VkPhysicalDevice?.Dispose();
        VkPhysicalDevice = null!;
    }

    public unsafe void DestroyInstance()
    {
        if (_debugUtils != null && _debugMessenger.Handle != 0)
        {
            _debugUtils.DestroyDebugUtilsMessenger(VkInstance.Handle, _debugMessenger, DefaultAllocationCallbacks);
            _debugMessenger = default;
            _debugUtils.Dispose();
            _debugUtils = null;
        }

        VkInstance?.Dispose();
        VkInstance = null!;
    }

    public void Shutdown()
    {
        VkApi?.Dispose();
        VkApi = null!;
    }
}