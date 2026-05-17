using JetBrains.Annotations;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan;

[StateService<IVulkanContext, VulkanModule>]
[PublicAPI]
public unsafe class VulkanContext : IVulkanContext, IVulkanContextStateFacade
{
    private const string ValidationLayerName = "VK_LAYER_KHRONOS_validation";
    private const string DebugUtilsExtensionName = "VK_EXT_debug_utils";

    public Vk VkApi { get; private set; } = null!;
    public VkInstance VkInstance { get; private set; } = null!;
    public VkPhysicalDevice VkPhysicalDevice { get; private set; } = null!;
    public VkDevice VkDevice { get; private set; } = null!;
    public VmaAllocator VmaAllocator { get; private set; } = null!;
    public AllocationCallbacks* DefaultAllocationCallbacks { get; }
    public IObjectTracker<VulkanObject> ObjectTracker { get; private set; } = null!;

    private readonly Dictionary<uint, List<VkQueue>> _queuesByFamily = [];
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private KhrSurface? _khrSurface;

    public required IDIService ModDIService { private get; init; }
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

    public void CreateInstance()
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

    private void SetupDebugMessenger()
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

    private static uint DebugCallback(
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

    public void CreateDevice()
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
                                          ?? throw new InvalidOperationException("No transfer queue family found");
                configContext.RequestQueues(transferQueueFamily, 1);
            }
            
            var computeQueueFamily = configContext.FindQueueFamily(QueueFlags.ComputeBit)
                                     ?? throw new InvalidOperationException("No compute queue family found");
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

        VmaAllocator = VmaAllocator.Create(
            VkInstance.Handle,
            VkPhysicalDevice.PhysicalDevice,
            VkDevice.Handle,
            Vk.Version13);
    }

    public Result<VkCommandPool, VkApiResult> CreateCommandPool(CommandPoolCreateFlags flags, uint queueFamilyIndex, [InjectCallerContext] CallerContext callerContext = default)
    {
        CommandPoolCreateInfo createInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = flags,
            QueueFamilyIndex = queueFamilyIndex
        };

        var result = VkApi.CreateCommandPool(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var pool);

        if (result != VkApiResult.Success || pool.Handle == 0) return result;

        return new VkCommandPool(pool, this, callerContext);
    }

    public Result<VkDescriptorPool, VkApiResult> CreateDescriptorPool(VkDescriptorPoolCreateOptions options, [InjectCallerContext] CallerContext callerContext = default)
    {
        var poolSizesSpan = options.PoolSizes.AsSpan();
        fixed (DescriptorPoolSize* poolSizesPtr = poolSizesSpan)
        {
            var createInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = options.MaxSets,
                PoolSizeCount = (uint)poolSizesSpan.Length,
                PPoolSizes = poolSizesPtr,
                Flags = options.Flags,
            };
            var result = VkApi.CreateDescriptorPool(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var pool);
            if (result != VkApiResult.Success) return result;
            return new VkDescriptorPool(pool, this, callerContext);
        }
    }

    public Result<VkSemaphore, VkApiResult> CreateSemaphore(SemaphoreCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default)
    {
        var createInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
            Flags = flags
        };
        var result = VkApi.CreateSemaphore(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var semaphore);
        if (result != VkApiResult.Success) return result;
        return new VkSemaphore(semaphore, this, callerContext);
    }

    public Result<VkFence, VkApiResult> CreateFence(FenceCreateFlags flags = 0, [InjectCallerContext] CallerContext callerContext = default)
    {
        var createInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = flags
        };
        var result = VkApi.CreateFence(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var fence);
        if (result != VkApiResult.Success) return result;
        return new VkFence(fence, this, callerContext);
    }

    public Result<VkDescriptorSetLayout, VkApiResult> CreateDescriptorSetLayout(VkDescriptorSetLayoutCreateOptions options, [InjectCallerContext] CallerContext callerContext = default)
    {
        var bindingsSpan = options.Bindings.AsSpan();
        fixed (DescriptorSetLayoutBinding* bindingsPtr = bindingsSpan)
        {
            var createInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindingsSpan.Length,
                PBindings = bindingsPtr,
                Flags = options.Flags,
            };
            var result = VkApi.CreateDescriptorSetLayout(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var layout);
            if (result != VkApiResult.Success) return result;
            return new VkDescriptorSetLayout(layout, this, callerContext);
        }
    }

    public Result<VkPipelineLayout, VkApiResult> CreatePipelineLayout(VkPipelineLayoutCreateOptions options, [InjectCallerContext] CallerContext callerContext = default)
    {
        var setLayouts = options.SetLayouts;
        Span<DescriptorSetLayout> handles = setLayouts.Length <= 16
            ? stackalloc DescriptorSetLayout[setLayouts.Length]
            : new DescriptorSetLayout[setLayouts.Length];
        for (var i = 0; i < setLayouts.Length; i++)
            handles[i] = setLayouts[i].Handle;

        var pushConstantsSpan = options.PushConstantRanges.AsSpan();

        fixed (DescriptorSetLayout* setLayoutsPtr = handles)
        fixed (PushConstantRange* pushConstantsPtr = pushConstantsSpan)
        {
            var createInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)setLayouts.Length,
                PSetLayouts = setLayoutsPtr,
                PushConstantRangeCount = (uint)pushConstantsSpan.Length,
                PPushConstantRanges = pushConstantsPtr,
            };
            var result = VkApi.CreatePipelineLayout(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var layout);
            if (result != VkApiResult.Success) return result;
            return new VkPipelineLayout(layout, this, callerContext);
        }
    }

    public Result<VkPipeline, VkApiResult> CreateComputePipeline(VkComputePipelineCreateOptions options, [InjectCallerContext] CallerContext callerContext = default)
    {
        var byteCount = Encoding.UTF8.GetByteCount(options.EntryPoint);
        Span<byte> nameBuffer = byteCount + 1 <= 64
            ? stackalloc byte[byteCount + 1]
            : new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(options.EntryPoint, nameBuffer);
        nameBuffer[byteCount] = 0;

        fixed (byte* pName = nameBuffer)
        {
            var stageInfo = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ComputeBit,
                Module = options.Shader.Handle,
                PName = pName,
            };
            var createInfo = new ComputePipelineCreateInfo
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Stage = stageInfo,
                Layout = options.Layout.Handle,
            };
            var result = VkApi.CreateComputePipelines(VkDevice.Handle, default, 1, createInfo, DefaultAllocationCallbacks, out var pipeline);
            if (result != VkApiResult.Success) return result;
            return new VkPipeline(pipeline, this, callerContext);
        }
    }

    public Result<VkShaderModule, VkApiResult> CreateShaderModule(ReadOnlySpan<uint> spirvCode, [InjectCallerContext] CallerContext callerContext = default)
    {
        fixed (uint* codePtr = spirvCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                PCode = codePtr,
                CodeSize = (nuint)spirvCode.Length * sizeof(uint),
            };
            var result = VkApi.CreateShaderModule(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var module);
            if (result != VkApiResult.Success) return result;
            return new VkShaderModule(module, this, callerContext);
        }
    }

    public Result<VkImage, VkApiResult> CreateImage(VkImageCreateOptions options, in VmaAllocationCreateInfo allocInfo, [InjectCallerContext] CallerContext callerContext = default)
    {
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = options.Type,
            Format = options.Format,
            Extent = options.Extent,
            MipLevels = options.MipLevels,
            ArrayLayers = options.ArrayLayers,
            Samples = options.Samples,
            Tiling = options.Tiling,
            Usage = options.Usage,
            SharingMode = options.SharingMode,
            InitialLayout = options.InitialLayout,
        };

        var result = VmaAllocator.CreateImage(in imageInfo, in allocInfo, out var image, out var allocation, out _);
        if (result != VkApiResult.Success) return result;

        return new VkImage(
            image,
            imageInfo.Format,
            imageInfo.Extent,
            imageInfo.MipLevels,
            imageInfo.ArrayLayers,
            imageInfo.ImageType,
            imageInfo.Usage,
            allocation,
            this,
            callerContext);
    }

    public Result<VkBuffer, VkApiResult> CreateBuffer(VkBufferCreateOptions options, in VmaAllocationCreateInfo allocInfo, [InjectCallerContext] CallerContext callerContext = default)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = options.Size,
            Usage = options.Usage,
            SharingMode = options.SharingMode,
        };

        var result = VmaAllocator.CreateBuffer(in bufferInfo, in allocInfo, out var buffer, out var allocation, out var allocationInfo);
        if (result != VkApiResult.Success) return result;

        return new VkBuffer(
            buffer, bufferInfo.Size, bufferInfo.Usage,
            allocation, allocationInfo.MappedData, this, callerContext);
    }

    public Result<VkImage, VkApiResult> CreateStorageImage2D(
        Extent2D extent,
        Format format,
        VmaMemoryUsage memoryUsage = VmaMemoryUsage.GpuOnly,
        ImageUsageFlags extraUsage = ImageUsageFlags.TransferSrcBit,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        var options = new VkImageCreateOptions(
            Extent: new Extent3D(extent.Width, extent.Height, 1),
            Format: format,
            Usage: ImageUsageFlags.StorageBit | extraUsage);
        var allocInfo = new VmaAllocationCreateInfo { Usage = memoryUsage };
        return CreateImage(options, in allocInfo, callerContext);
    }

    public Result<VkBuffer, VkApiResult> CreateMappedStorageBuffer(
        ulong size,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        var options = new VkBufferCreateOptions(
            Size: size,
            Usage: BufferUsageFlags.StorageBufferBit);
        var allocInfo = new VmaAllocationCreateInfo
        {
            Usage = VmaMemoryUsage.CpuToGpu,
            Flags = VmaAllocationCreateFlags.Mapped
        };
        return CreateBuffer(options, in allocInfo, callerContext);
    }

    public Result<VkSampler, VkApiResult> CreateSampler(
        VkSamplerCreateOptions options,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        var createInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = options.MagFilter,
            MinFilter = options.MinFilter,
            MipmapMode = options.MipmapMode,
            AddressModeU = options.AddressModeU,
            AddressModeV = options.AddressModeV,
            AddressModeW = options.AddressModeW,
            MipLodBias = options.MipLodBias,
            AnisotropyEnable = options.AnisotropyEnable,
            MaxAnisotropy = options.MaxAnisotropy,
            CompareEnable = options.CompareEnable,
            CompareOp = options.CompareOp,
            MinLod = options.MinLod,
            MaxLod = options.MaxLod,
            BorderColor = options.BorderColor,
            UnnormalizedCoordinates = options.UnnormalizedCoordinates,
        };
        var result = VkApi.CreateSampler(VkDevice.Handle, createInfo, DefaultAllocationCallbacks, out var sampler);
        if (result != VkApiResult.Success) return result;
        return new VkSampler(sampler, this, callerContext);
    }

    public VkSurface? CreateSurface(IWindow window)
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
            var queues = new List<VkQueue>((int)request.QueueCount);

            for (uint i = 0; i < request.QueueCount; i++)
            {
                var handle = VkDevice.GetQueue(request.QueueFamilyIndex, i);
                queues.Add(new VkQueue(handle, request.QueueFamilyIndex, i, capabilities, this));
            }

            _queuesByFamily[request.QueueFamilyIndex] = queues;
        }

        Log.Debug("Retrieved {QueueCount} queue(s) across {FamilyCount} family(s)",
            _queuesByFamily.Values.Sum(q => q.Count), _queuesByFamily.Count);
    }

    public VkQueue? GetQueue(uint familyIndex, uint queueIndex)
    {
        return _queuesByFamily.TryGetValue(familyIndex, out var queues)
            ? queues.FirstOrDefault(q => q.QueueIndex == queueIndex)
            : null;
    }

    public IReadOnlyList<VkQueue> GetQueuesForFamily(uint familyIndex)
    {
        return _queuesByFamily.TryGetValue(familyIndex, out var queues) ? queues : [];
    }

    /// <summary>
    /// Pre-teardown device-idle checkpoint. <see cref="DestroyDevice"/> and all VMA / render-graph
    /// teardown transitions order themselves after this method so they see an idle device.
    /// </summary>
    public void BeginVulkanTeardown()
    {
        VkDevice?.WaitIdle();
    }

    public void DestroyDevice()
    {
        VmaAllocator?.Dispose();
        VmaAllocator = null!;

        foreach (var queues in _queuesByFamily.Values)
        {
            foreach (var queue in queues)
                queue.Dispose();
        }
        _queuesByFamily.Clear();

        VkDevice?.Dispose();
        VkDevice = null!;
    }

    public void DestroyPhysicalDevice()
    {
        VkPhysicalDevice?.Dispose();
        VkPhysicalDevice = null!;
    }

    public void DestroyInstance()
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
        var leakedCount = ObjectTracker.Count;
        if (leakedCount > 0)
        {
            Log.Warning("Vulkan resource leaks detected: {Count} object(s) not disposed", leakedCount);
            foreach (var (obj, callsite) in ObjectTracker.GetTrackingEntries())
            {
                Log.Warning("  Leaked {Type} created at {Callsite}",
                    obj.GetType().Name, callsite);
            }
        }

        VkApi?.Dispose();
        VkApi = null!;
    }
}