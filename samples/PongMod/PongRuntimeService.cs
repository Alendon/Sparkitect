using System.Diagnostics;
using System.Numerics;
using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace PongMod;

[StateService<IPongRuntimeService, PongModule>]
internal class PongRuntimeService : IPongRuntimeService
{
    private PongGameData _gameData;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    // Vulkan resources
    private ISparkitWindow? _window;
    private VkCommandPool? _commandPool;
    private VkCommandBuffer? _commandBuffer;
    private VkSemaphore _imageAvailableSemaphore;
    private VkSemaphore _renderFinishedSemaphore;
    private Fence _inFlightFence;
    private uint _graphicsQueueFamily;
    private Queue _graphicsQueue;

    // Compute pipeline resources
    private DescriptorSetLayout _descriptorSetLayout;
    private PipelineLayout _pipelineLayout;
    private Pipeline _computePipeline;
    private DescriptorPool _descriptorPool;
    private DescriptorSet[] _descriptorSets = [];
    private ImageView[] _storageImageViews = [];

    public required IWindowManager WindowManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required IShaderManager ShaderManager { private get; init; }

    public ref PongGameData GameData => ref _gameData;
    public float DeltaTime { get; private set; }

    public unsafe void Initialize()
    {
        _gameData = PongGameData.CreateDefault();
        _frameTimer.Start();
        _lastFrameTime = 0;

        // Create window
        _window = WindowManager.CreateWindow("Pong", 800, 600);

        // Find graphics queue
        var physicalDevice = VulkanContext.VkPhysicalDevice;
        _graphicsQueueFamily = FindGraphicsQueueFamily(physicalDevice);
        var queue = VulkanContext.GetQueue(_graphicsQueueFamily, 0);
        if (queue == null)
            throw new InvalidOperationException("No graphics queue available");
        _graphicsQueue = queue.Handle;

        // Create command pool
        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            _graphicsQueueFamily);
        if (poolResult is VkResult<VkCommandPool>.Error poolError)
            throw new InvalidOperationException($"Failed to create command pool: {poolError.errorResult}");
        _commandPool = ((VkResult<VkCommandPool>.Success)poolResult).value;

        // Allocate command buffer
        var bufferResult = _commandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (bufferResult is VkResult<VkCommandBuffer>.Error bufferError)
            throw new InvalidOperationException($"Failed to allocate command buffer: {bufferError.errorResult}");
        _commandBuffer = ((VkResult<VkCommandBuffer>.Success)bufferResult).value;

        // Create sync objects
        var semaphoreInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice.Handle;

        vk.CreateSemaphore(device, semaphoreInfo, null, out _imageAvailableSemaphore);
        vk.CreateSemaphore(device, semaphoreInfo, null, out _renderFinishedSemaphore);
        vk.CreateFence(device, fenceInfo, null, out _inFlightFence);

        CreateComputePipeline(vk, device);
        CreateDescriptorResources(vk, device);

        Log.Debug("Pong runtime initialized with Vulkan resources");
    }

    private unsafe void CreateComputePipeline(Vk vk, Device device)
    {
        // Get shader module
        if (!ShaderManager.TryGetRegisteredShaderModule(ShaderModuleID.PongMod.Pong, out var shaderModule))
            throw new InvalidOperationException("Pong shader not registered");

        // Descriptor set layout: binding 0 = storage image
        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.StorageImage,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };
        vk.CreateDescriptorSetLayout(device, layoutInfo, null, out _descriptorSetLayout);

        // Pipeline layout: push constants + descriptor set
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = (uint)sizeof(PongGameData)
        };
        var setLayout = _descriptorSetLayout;
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstantRange
        };
        vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out _pipelineLayout);

        // Compute pipeline
        var entryPoint = "computeMain"u8;
        fixed (byte* entryPointPtr = entryPoint)
        {
            var stageInfo = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ComputeBit,
                Module = shaderModule.Handle,
                PName = entryPointPtr
            };
            var computePipelineInfo = new ComputePipelineCreateInfo
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Stage = stageInfo,
                Layout = _pipelineLayout
            };
            vk.CreateComputePipelines(device, default, 1, computePipelineInfo, null, out _computePipeline);
        }
    }

    private unsafe void CreateDescriptorResources(Vk vk, Device device)
    {
        var swapchain = _window!.Swapchain;
        var imageCount = (int)swapchain.ImageCount;

        // Descriptor pool
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageImage,
            DescriptorCount = (uint)imageCount
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = (uint)imageCount,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };
        vk.CreateDescriptorPool(device, poolInfo, null, out _descriptorPool);

        // Storage image views
        _storageImageViews = new ImageView[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            var viewInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapchain.Images[i].Handle,
                ViewType = ImageViewType.Type2D,
                Format = swapchain.ImageFormat,
                SubresourceRange = new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };
            vk.CreateImageView(device, viewInfo, null, out _storageImageViews[i]);
        }

        // Allocate descriptor sets
        _descriptorSets = new DescriptorSet[imageCount];
        var layouts = new DescriptorSetLayout[imageCount];
        Array.Fill(layouts, _descriptorSetLayout);
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        fixed (DescriptorSet* setsPtr = _descriptorSets)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = _descriptorPool,
                DescriptorSetCount = (uint)imageCount,
                PSetLayouts = layoutsPtr
            };
            vk.AllocateDescriptorSets(device, allocInfo, setsPtr);
        }

        // Write descriptor sets
        for (var i = 0; i < imageCount; i++)
        {
            var imageInfo = new DescriptorImageInfo
            {
                ImageView = _storageImageViews[i],
                ImageLayout = ImageLayout.General
            };
            var write = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSets[i],
                DstBinding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageImage,
                PImageInfo = &imageInfo
            };
            vk.UpdateDescriptorSets(device, 1, write, 0, null);
        }
    }

    private uint FindGraphicsQueueFamily(VkPhysicalDevice physicalDevice)
    {
        var properties = physicalDevice.GetQueueFamilyProperties();
        for (uint i = 0; i < properties.Length; i++)
        {
            if ((properties[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                return i;
        }
        throw new InvalidOperationException("No graphics queue family found");
    }

    public void Tick()
    {
        var currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        DeltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        UpdateSimulation();
    }

    public unsafe void Render()
    {
        if (_window is null) return;

        _window.PollEvents();
        if (!_window.IsOpen)
        {
            Log.Information("Shutting down");
            GameStateManager.Shutdown();
            return;
        }

        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice.Handle;
        var swapchain = _window.Swapchain;

        // Wait for previous frame
        vk.WaitForFences(device, 1, _inFlightFence, true, ulong.MaxValue);
        vk.ResetFences(device, 1, _inFlightFence);

        // Acquire image
        var acquireResult = swapchain.AcquireNextImage(_imageAvailableSemaphore, autoRecreate: true);
        if (acquireResult is VkResult<uint>.Error)
            return;
        var imageIndex = ((VkResult<uint>.Success)acquireResult).value;

        // Record command buffer
        var cmd = _commandBuffer!.Handle;
        vk.ResetCommandBuffer(cmd, 0);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        vk.BeginCommandBuffer(cmd, beginInfo);

        // Transition image to General for compute write
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.General,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = swapchain.Images[(int)imageIndex].Handle,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = 0,
            DstAccessMask = AccessFlags.ShaderWriteBit
        };
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.ComputeShaderBit,
            0, 0, null, 0, null, 1, barrier);

        // Bind compute pipeline and dispatch
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Compute, _computePipeline);
        var descriptorSet = _descriptorSets[imageIndex];
        vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Compute, _pipelineLayout, 0, 1, &descriptorSet, 0, null);

        // Update screen dimensions and push constants
        _gameData.ScreenWidth = swapchain.Extent.Width;
        _gameData.ScreenHeight = swapchain.Extent.Height;
        fixed (PongGameData* gameDataPtr = &_gameData)
        {
            vk.CmdPushConstants(cmd, _pipelineLayout, ShaderStageFlags.ComputeBit, 0, (uint)sizeof(PongGameData), gameDataPtr);
        }

        // Dispatch compute shader (8x8 workgroups)
        var groupCountX = (swapchain.Extent.Width + 7) / 8;
        var groupCountY = (swapchain.Extent.Height + 7) / 8;
        vk.CmdDispatch(cmd, groupCountX, groupCountY, 1);

        // Transition to present
        barrier.OldLayout = ImageLayout.General;
        barrier.NewLayout = ImageLayout.PresentSrcKhr;
        barrier.SrcAccessMask = AccessFlags.ShaderWriteBit;
        barrier.DstAccessMask = 0;
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.BottomOfPipeBit,
            0, 0, null, 0, null, 1, barrier);

        vk.EndCommandBuffer(cmd);

        // Submit
        var waitSemaphore = _imageAvailableSemaphore;
        var signalSemaphore = _renderFinishedSemaphore;
        var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };
        vk.QueueSubmit(_graphicsQueue, 1, submitInfo, _inFlightFence);

        // Present
        swapchain.Present(imageIndex, _renderFinishedSemaphore, _graphicsQueue);
    }

    public unsafe void Cleanup()
    {
        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice.Handle;

        vk.DeviceWaitIdle(device);

        // Cleanup compute resources
        foreach (var view in _storageImageViews)
            vk.DestroyImageView(device, view, null);
        _storageImageViews = [];

        vk.DestroyDescriptorPool(device, _descriptorPool, null);
        vk.DestroyPipeline(device, _computePipeline, null);
        vk.DestroyPipelineLayout(device, _pipelineLayout, null);
        vk.DestroyDescriptorSetLayout(device, _descriptorSetLayout, null);

        vk.DestroyFence(device, _inFlightFence, null);
        vk.DestroySemaphore(device, _renderFinishedSemaphore, null);
        vk.DestroySemaphore(device, _imageAvailableSemaphore, null);

        _commandPool?.Dispose();
        _window?.Dispose();

        Log.Debug("Pong runtime cleanup complete");
    }

    private void UpdateSimulation()
    {
        var deltaTime = DeltaTime;
        _gameData.BallPosition += _gameData.BallVelocity * deltaTime;

        if (_gameData.BallPosition.Y - _gameData.BallRadius <= 0 ||
            _gameData.BallPosition.Y + _gameData.BallRadius >= 1)
        {
            _gameData.BallVelocity.Y = -_gameData.BallVelocity.Y;
            _gameData.BallPosition.Y = Math.Clamp(_gameData.BallPosition.Y,
                _gameData.BallRadius, 1 - _gameData.BallRadius);
        }

        if (_gameData.BallPosition.X - _gameData.BallRadius <= _gameData.PaddleWidth)
        {
            if (Math.Abs(_gameData.BallPosition.Y - _gameData.LeftPaddleY) < _gameData.PaddleHeight / 2)
            {
                _gameData.BallVelocity.X = Math.Abs(_gameData.BallVelocity.X);
                _gameData.BallPosition.X = _gameData.PaddleWidth + _gameData.BallRadius;
            }
        }

        if (_gameData.BallPosition.X + _gameData.BallRadius >= 1 - _gameData.PaddleWidth)
        {
            if (Math.Abs(_gameData.BallPosition.Y - _gameData.RightPaddleY) < _gameData.PaddleHeight / 2)
            {
                _gameData.BallVelocity.X = -Math.Abs(_gameData.BallVelocity.X);
                _gameData.BallPosition.X = 1 - _gameData.PaddleWidth - _gameData.BallRadius;
            }
        }

        if (_gameData.BallPosition.X < 0)
        {
            _gameData.RightScore++;
            Log.Information("Right scores! {Left} - {Right}", _gameData.LeftScore, _gameData.RightScore);
            ResetBall();
        }
        else if (_gameData.BallPosition.X > 1)
        {
            _gameData.LeftScore++;
            Log.Information("Left scores! {Left} - {Right}", _gameData.LeftScore, _gameData.RightScore);
            ResetBall();
        }
    }

    public void MoveLeftPaddle(float delta)
    {
        _gameData.LeftPaddleY = Math.Clamp(_gameData.LeftPaddleY + delta,
            _gameData.PaddleHeight / 2, 1 - _gameData.PaddleHeight / 2);
    }

    public void MoveRightPaddle(float delta)
    {
        _gameData.RightPaddleY = Math.Clamp(_gameData.RightPaddleY + delta,
            _gameData.PaddleHeight / 2, 1 - _gameData.PaddleHeight / 2);
    }

    public void ResetBall()
    {
        _gameData.BallPosition = new Vector2(0.5f, 0.5f);
        var angle = (Random.Shared.NextSingle() - 0.5f) * 0.5f;
        var direction = Random.Shared.Next(2) == 0 ? 1 : -1;
        _gameData.BallVelocity = new Vector2(0.4f * direction, 0.3f * angle);
    }
}
