using System.Diagnostics;
using System.Numerics;
using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;
using Sparkitect.Graphics.Vulkan.Vma;
namespace PongMod;

[StateService<IPongRuntimeService, PongModule>]
internal class PongRuntimeService : IPongRuntimeService, IPongRuntimeServiceStateFacade
{
    private PongGameData _gameData;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    // Window and input
    private ISparkitWindow? _window;
    private VkCommandPool? _commandPool;
    private VkCommandBuffer? _commandBuffer;
    private VkSemaphore? _imageAvailableSemaphore;
    private VkSemaphore? _renderFinishedSemaphore;
    private VkFence? _inFlightFence;
    private uint _graphicsQueueFamily;
    private Queue _graphicsQueue;

    // VMA and storage image
    private VmaAllocator? _vmaAllocator;
    private VmaImage? _storageImage;
    private ImageView _storageImageView;

    // Compute pipeline resources
    private VkDescriptorSetLayout? _descriptorSetLayout;
    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;
    private VkDescriptorPool? _descriptorPool;
    private VkDescriptorSet? _descriptorSet;

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
        var semaphoreResult1 = VulkanContext.CreateSemaphore();
        if (semaphoreResult1 is VkResult<VkSemaphore>.Error semaphoreError1)
            throw new InvalidOperationException($"Failed to create semaphore: {semaphoreError1.errorResult}");
        _imageAvailableSemaphore = ((VkResult<VkSemaphore>.Success)semaphoreResult1).value;

        var semaphoreResult2 = VulkanContext.CreateSemaphore();
        if (semaphoreResult2 is VkResult<VkSemaphore>.Error semaphoreError2)
            throw new InvalidOperationException($"Failed to create semaphore: {semaphoreError2.errorResult}");
        _renderFinishedSemaphore = ((VkResult<VkSemaphore>.Success)semaphoreResult2).value;

        var fenceResult = VulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
        if (fenceResult is VkResult<VkFence>.Error fenceError)
            throw new InvalidOperationException($"Failed to create fence: {fenceError.errorResult}");
        _inFlightFence = ((VkResult<VkFence>.Success)fenceResult).value;

        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice.Handle;

        // Create VMA allocator
        _vmaAllocator = VmaAllocator.Create(
            VulkanContext.VkInstance.Handle,
            VulkanContext.VkPhysicalDevice.PhysicalDevice,
            device);

        // Create storage image (R8G8B8A8_UNORM supports STORAGE_BIT)
        var swapchain = _window!.Swapchain;
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = Format.R8G8B8A8Unorm,
            Extent = new Extent3D(swapchain.Extent.Width, swapchain.Extent.Height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };
        var allocInfo = new VmaAllocationCreateInfo
        {
            Usage = VmaMemoryUsage.GpuOnly
        };
        _storageImage = _vmaAllocator.CreateImage(imageInfo, allocInfo);

        // Create storage image view
        var storageViewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _storageImage.Image,
            ViewType = ImageViewType.Type2D,
            Format = Format.R8G8B8A8Unorm,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        vk.CreateImageView(device, storageViewInfo, null, out _storageImageView);

        CreateComputePipeline();
        CreateDescriptorResources();

        Log.Debug("Pong runtime initialized with Vulkan resources");
    }

    private unsafe void CreateComputePipeline()
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
        var layoutResult = VulkanContext.CreateDescriptorSetLayout(layoutInfo);
        if (layoutResult is VkResult<VkDescriptorSetLayout>.Error layoutError)
            throw new InvalidOperationException($"Failed to create descriptor set layout: {layoutError.errorResult}");
        _descriptorSetLayout = ((VkResult<VkDescriptorSetLayout>.Success)layoutResult).value;

        // Pipeline layout: push constants + descriptor set
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = (uint)sizeof(PongGameData)
        };
        var setLayout = _descriptorSetLayout.Handle;
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstantRange
        };
        var pipelineLayoutResult = VulkanContext.CreatePipelineLayout(pipelineLayoutInfo);
        if (pipelineLayoutResult is VkResult<VkPipelineLayout>.Error pipelineLayoutError)
            throw new InvalidOperationException($"Failed to create pipeline layout: {pipelineLayoutError.errorResult}");
        _pipelineLayout = ((VkResult<VkPipelineLayout>.Success)pipelineLayoutResult).value;

        // Compute pipeline
        var entryPoint = "main"u8;
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
                Layout = _pipelineLayout.Handle
            };
            var pipelineResult = VulkanContext.CreateComputePipeline(computePipelineInfo);
            if (pipelineResult is VkResult<VkPipeline>.Error pipelineError)
                throw new InvalidOperationException($"Failed to create compute pipeline: {pipelineError.errorResult}");
            _computePipeline = ((VkResult<VkPipeline>.Success)pipelineResult).value;
        }
    }

    private unsafe void CreateDescriptorResources()
    {
        // Descriptor pool (only need 1)
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageImage,
            DescriptorCount = 1
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 1,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };
        var poolResult = VulkanContext.CreateDescriptorPool(poolInfo);
        if (poolResult is VkResult<VkDescriptorPool>.Error descriptorPoolError)
            throw new InvalidOperationException($"Failed to create descriptor pool: {descriptorPoolError.errorResult}");
        _descriptorPool = ((VkResult<VkDescriptorPool>.Success)poolResult).value;

        // Allocate single descriptor set
        var setResult = _descriptorPool.AllocateDescriptorSet(_descriptorSetLayout!.Handle);
        if (setResult is VkResult<VkDescriptorSet>.Error setError)
            throw new InvalidOperationException($"Failed to allocate descriptor set: {setError.errorResult}");
        _descriptorSet = ((VkResult<VkDescriptorSet>.Success)setResult).value;

        // Write descriptor pointing to storage image
        var imageInfo = new DescriptorImageInfo
        {
            ImageView = _storageImageView,
            ImageLayout = ImageLayout.General
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _descriptorSet.Handle,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            PImageInfo = &imageInfo
        };
        VulkanContext.VkApi.UpdateDescriptorSets(VulkanContext.VkDevice.Handle, 1, write, 0, null);
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
        _inFlightFence!.Wait();
        _inFlightFence.Reset();

        // Acquire image
        var acquireResult = swapchain.AcquireNextImage(_imageAvailableSemaphore!, autoRecreate: true);
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

        // Transition storage image to General for compute write
        var storageBarrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.General,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = _storageImage!.Image,
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
            0, 0, null, 0, null, 1, storageBarrier);

        // Bind compute pipeline and dispatch
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Compute, _computePipeline!.Handle);
        var descriptorSet = _descriptorSet!.Handle;
        vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Compute, _pipelineLayout!.Handle, 0, 1, &descriptorSet, 0, null);

        // Update screen dimensions and push constants
        _gameData.ScreenWidth = swapchain.Extent.Width;
        _gameData.ScreenHeight = swapchain.Extent.Height;
        fixed (PongGameData* gameDataPtr = &_gameData)
        {
            vk.CmdPushConstants(cmd, _pipelineLayout.Handle, ShaderStageFlags.ComputeBit, 0, (uint)sizeof(PongGameData), gameDataPtr);
        }

        // Dispatch compute shader (8x8 workgroups)
        var groupCountX = (swapchain.Extent.Width + 7) / 8;
        var groupCountY = (swapchain.Extent.Height + 7) / 8;
        vk.CmdDispatch(cmd, groupCountX, groupCountY, 1);

        // Transition storage image: GENERAL -> TRANSFER_SRC_OPTIMAL
        storageBarrier.OldLayout = ImageLayout.General;
        storageBarrier.NewLayout = ImageLayout.TransferSrcOptimal;
        storageBarrier.SrcAccessMask = AccessFlags.ShaderWriteBit;
        storageBarrier.DstAccessMask = AccessFlags.TransferReadBit;
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.TransferBit,
            0, 0, null, 0, null, 1, storageBarrier);

        // Transition swapchain image: UNDEFINED -> TRANSFER_DST_OPTIMAL
        var swapchainBarrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.TransferDstOptimal,
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
            DstAccessMask = AccessFlags.TransferWriteBit
        };
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.TransferBit,
            0, 0, null, 0, null, 1, swapchainBarrier);

        // Blit storage image to swapchain
        var blitRegion = new ImageBlit
        {
            SrcSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            DstSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        // Source region (storage image)
        blitRegion.SrcOffsets[0] = new Offset3D(0, 0, 0);
        blitRegion.SrcOffsets[1] = new Offset3D((int)swapchain.Extent.Width, (int)swapchain.Extent.Height, 1);
        // Destination region (swapchain image)
        blitRegion.DstOffsets[0] = new Offset3D(0, 0, 0);
        blitRegion.DstOffsets[1] = new Offset3D((int)swapchain.Extent.Width, (int)swapchain.Extent.Height, 1);

        vk.CmdBlitImage(cmd,
            _storageImage.Image, ImageLayout.TransferSrcOptimal,
            swapchain.Images[(int)imageIndex].Handle, ImageLayout.TransferDstOptimal,
            1, blitRegion, Filter.Nearest);

        // Transition swapchain to present
        swapchainBarrier.OldLayout = ImageLayout.TransferDstOptimal;
        swapchainBarrier.NewLayout = ImageLayout.PresentSrcKhr;
        swapchainBarrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        swapchainBarrier.DstAccessMask = 0;
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.BottomOfPipeBit,
            0, 0, null, 0, null, 1, swapchainBarrier);

        vk.EndCommandBuffer(cmd);

        // Submit
        var waitSemaphore = _imageAvailableSemaphore.Handle;
        var signalSemaphore = _renderFinishedSemaphore!.Handle;
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
        vk.QueueSubmit(_graphicsQueue, 1, submitInfo, _inFlightFence.Handle);

        // Present
        swapchain.Present(imageIndex, _renderFinishedSemaphore, _graphicsQueue);
    }

    public unsafe void Cleanup()
    {
        VulkanContext.VkApi.DeviceWaitIdle(VulkanContext.VkDevice.Handle);

        // Cleanup storage image resources
        VulkanContext.VkApi.DestroyImageView(VulkanContext.VkDevice.Handle, _storageImageView, null);
        _storageImage?.Dispose();
        _vmaAllocator?.Dispose();

        // Dispose wrappers (pool disposes its descriptor sets automatically)
        _descriptorPool?.Dispose();
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
        _descriptorSetLayout?.Dispose();

        _inFlightFence?.Dispose();
        _renderFinishedSemaphore?.Dispose();
        _imageAvailableSemaphore?.Dispose();

        _commandPool?.Dispose();
        _window?.Dispose();

        Log.Debug("Pong runtime cleanup complete");
    }

    private void UpdateSimulation()
    {
        // Poll input for paddle movement
        if (_window != null)
        {
            var keyboard = _window.Keyboard;
            var paddleSpeed = 0.8f;

            // Left paddle: W/S
            if (keyboard.IsKeyDown(Key.W))
                MoveLeftPaddle(-paddleSpeed * DeltaTime);
            if (keyboard.IsKeyDown(Key.S))
                MoveLeftPaddle(paddleSpeed * DeltaTime);

            // Right paddle: Up/Down arrows
            if (keyboard.IsKeyDown(Key.Up))
                MoveRightPaddle(-paddleSpeed * DeltaTime);
            if (keyboard.IsKeyDown(Key.Down))
                MoveRightPaddle(paddleSpeed * DeltaTime);
        }

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
                float speed = _gameData.BallVelocity.Length();
                float offset = (_gameData.BallPosition.Y - _gameData.LeftPaddleY) / (_gameData.PaddleHeight / 2);
                float maxAngle = 0.6f;
                _gameData.BallVelocity = new Vector2(1, offset * maxAngle);
                _gameData.BallVelocity = Vector2.Normalize(_gameData.BallVelocity) * speed;
                _gameData.BallPosition.X = _gameData.PaddleWidth + _gameData.BallRadius;
            }
        }

        if (_gameData.BallPosition.X + _gameData.BallRadius >= 1 - _gameData.PaddleWidth)
        {
            if (Math.Abs(_gameData.BallPosition.Y - _gameData.RightPaddleY) < _gameData.PaddleHeight / 2)
            {
                float speed = _gameData.BallVelocity.Length();
                float offset = (_gameData.BallPosition.Y - _gameData.RightPaddleY) / (_gameData.PaddleHeight / 2);
                float maxAngle = 0.6f;
                _gameData.BallVelocity = new Vector2(-1, offset * maxAngle);
                _gameData.BallVelocity = Vector2.Normalize(_gameData.BallVelocity) * speed;
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
