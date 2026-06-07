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
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;
namespace PongMod;

[StateService<IPongRuntimeService, PongModule>]
internal class PongRuntimeService : IPongRuntimeService
{
    private PongGameData _gameData;
    private Vector3 _backgroundColor = new(0.1f, 0.1f, 0.15f);
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
    private VkQueue _graphicsQueue = null!;

    // Storage image
    private VkImage? _storageImage;
    private VkImageView? _storageImageView;

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

    public ISparkitWindow Window => _window!;

    public Vector3 BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    public void Initialize()
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
        _graphicsQueue = queue;

        // Create command pool
        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            _graphicsQueueFamily);
        if (poolResult is Result<VkCommandPool, VkApiResult>.Error poolError)
            throw new InvalidOperationException($"Failed to create command pool: {poolError.Value}");
        _commandPool = ((Result<VkCommandPool, VkApiResult>.Ok)poolResult).Value;

        // Allocate command buffer
        var bufferResult = _commandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (bufferResult is Result<VkCommandBuffer, VkApiResult>.Error bufferError)
            throw new InvalidOperationException($"Failed to allocate command buffer: {bufferError.Value}");
        _commandBuffer = ((Result<VkCommandBuffer, VkApiResult>.Ok)bufferResult).Value;

        // Create sync objects
        var semaphoreResult1 = VulkanContext.CreateSemaphore();
        if (semaphoreResult1 is Result<VkSemaphore, VkApiResult>.Error semaphoreError1)
            throw new InvalidOperationException($"Failed to create semaphore: {semaphoreError1.Value}");
        _imageAvailableSemaphore = ((Result<VkSemaphore, VkApiResult>.Ok)semaphoreResult1).Value;

        var semaphoreResult2 = VulkanContext.CreateSemaphore();
        if (semaphoreResult2 is Result<VkSemaphore, VkApiResult>.Error semaphoreError2)
            throw new InvalidOperationException($"Failed to create semaphore: {semaphoreError2.Value}");
        _renderFinishedSemaphore = ((Result<VkSemaphore, VkApiResult>.Ok)semaphoreResult2).Value;

        var fenceResult = VulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
        if (fenceResult is Result<VkFence, VkApiResult>.Error fenceError)
            throw new InvalidOperationException($"Failed to create fence: {fenceError.Value}");
        _inFlightFence = ((Result<VkFence, VkApiResult>.Ok)fenceResult).Value;

        // Create storage image (R8G8B8A8_UNORM supports STORAGE_BIT)
        var swapchain = _window!.Swapchain;
        var storageImageResult = VulkanContext.CreateStorageImage2D(swapchain.Extent, Format.R8G8B8A8Unorm);
        if (storageImageResult is Result<VkImage, VkApiResult>.Error storageImageError)
            throw new InvalidOperationException($"Failed to create storage image: {storageImageError.Value}");
        _storageImage = ((Result<VkImage, VkApiResult>.Ok)storageImageResult).Value;

        // Create storage image view
        var storageViewResult = _storageImage!.CreateView(ImageAspectFlags.ColorBit);
        if (storageViewResult is Result<VkImageView, VkApiResult>.Error storageViewErr)
            throw new InvalidOperationException($"Failed to create storage image view: {storageViewErr.Value}");
        _storageImageView = ((Result<VkImageView, VkApiResult>.Ok)storageViewResult).Value;

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
        var layoutResult = VulkanContext.CreateDescriptorSetLayout(
            new VkDescriptorSetLayoutCreateOptions(Bindings:
            [
                new DescriptorSetLayoutBinding
                {
                    Binding = 0,
                    DescriptorType = DescriptorType.StorageImage,
                    DescriptorCount = 1,
                    StageFlags = ShaderStageFlags.ComputeBit,
                },
            ]));
        if (layoutResult is Result<VkDescriptorSetLayout, VkApiResult>.Error layoutError)
            throw new InvalidOperationException($"Failed to create descriptor set layout: {layoutError.Value}");
        _descriptorSetLayout = ((Result<VkDescriptorSetLayout, VkApiResult>.Ok)layoutResult).Value;

        // Pipeline layout: push constants + descriptor set
        var pipelineLayoutResult = VulkanContext.CreatePipelineLayout(
            new VkPipelineLayoutCreateOptions(
                SetLayouts: [_descriptorSetLayout!],
                PushConstantRanges:
                [
                    new PushConstantRange
                    {
                        StageFlags = ShaderStageFlags.ComputeBit,
                        Offset = 0,
                        Size = (uint)sizeof(PongGameData),
                    },
                ]));
        if (pipelineLayoutResult is Result<VkPipelineLayout, VkApiResult>.Error pipelineLayoutError)
            throw new InvalidOperationException($"Failed to create pipeline layout: {pipelineLayoutError.Value}");
        _pipelineLayout = ((Result<VkPipelineLayout, VkApiResult>.Ok)pipelineLayoutResult).Value;

        // Compute pipeline
        var pipelineResult = VulkanContext.CreateComputePipeline(
            new VkComputePipelineCreateOptions(shaderModule, _pipelineLayout!));
        if (pipelineResult is Result<VkPipeline, VkApiResult>.Error pipelineError)
            throw new InvalidOperationException($"Failed to create compute pipeline: {pipelineError.Value}");
        _computePipeline = ((Result<VkPipeline, VkApiResult>.Ok)pipelineResult).Value;
    }

    private void CreateDescriptorResources()
    {
        // Descriptor pool (only need 1)
        var poolResult = VulkanContext.CreateDescriptorPool(
            new VkDescriptorPoolCreateOptions(
                MaxSets: 1,
                PoolSizes:
                [
                    new DescriptorPoolSize
                    {
                        Type = DescriptorType.StorageImage,
                        DescriptorCount = 1,
                    },
                ]));
        if (poolResult is Result<VkDescriptorPool, VkApiResult>.Error descriptorPoolError)
            throw new InvalidOperationException($"Failed to create descriptor pool: {descriptorPoolError.Value}");
        _descriptorPool = ((Result<VkDescriptorPool, VkApiResult>.Ok)poolResult).Value;

        // Allocate single descriptor set
        var setResult = _descriptorPool.AllocateDescriptorSet(_descriptorSetLayout!.Handle);
        if (setResult is Result<VkDescriptorSet, VkApiResult>.Error setError)
            throw new InvalidOperationException($"Failed to allocate descriptor set: {setError.Value}");
        _descriptorSet = ((Result<VkDescriptorSet, VkApiResult>.Ok)setResult).Value;

        // Write descriptor pointing to storage image
        _descriptorSet!.WriteStorageImage(binding: 0, _storageImageView!, ImageLayout.General);
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

    public void Render()
    {
        if (_window is null) return;

        _window.PollEvents();
        if (!_window.IsOpen)
        {
            Log.Information("Shutting down");
            GameStateManager.Shutdown();
            return;
        }

        var swapchain = _window.Swapchain;

        // Wait for previous frame
        _inFlightFence!.Wait();
        _inFlightFence.Reset();

        // Acquire image
        var acquireResult = swapchain.AcquireNextImage(_imageAvailableSemaphore!, autoRecreate: true);
        if (acquireResult is Result<uint, VkApiResult>.Error)
            return;
        var imageIndex = ((Result<uint, VkApiResult>.Ok)acquireResult).Value;

        _commandBuffer!.Reset();
        _commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        // Transition storage image to General for compute write
        _commandBuffer!.ImageBarrier(
            _storageImage!,
            oldLayout: ImageLayout.Undefined,
            newLayout: ImageLayout.General,
            srcStage: PipelineStageFlags.TopOfPipeBit,
            dstStage: PipelineStageFlags.ComputeShaderBit,
            srcAccess: 0,
            dstAccess: AccessFlags.ShaderWriteBit);

        _commandBuffer.BindPipeline(PipelineBindPoint.Compute, _computePipeline!);
        _commandBuffer!.BindDescriptorSets(PipelineBindPoint.Compute, _pipelineLayout!, firstSet: 0, _descriptorSet!);

        _gameData.ScreenWidth = swapchain.Extent.Width;
        _gameData.ScreenHeight = swapchain.Extent.Height;
        _gameData.BackgroundColor = _backgroundColor;
        _commandBuffer.PushConstants(_pipelineLayout!, ShaderStageFlags.ComputeBit, 0, in _gameData);

        var groupCountX = (swapchain.Extent.Width + 7) / 8;
        var groupCountY = (swapchain.Extent.Height + 7) / 8;
        _commandBuffer.Dispatch(groupCountX, groupCountY, 1);

        // Transition storage image: GENERAL -> TRANSFER_SRC_OPTIMAL
        _commandBuffer!.ImageBarrier(
            _storageImage!,
            oldLayout: ImageLayout.General,
            newLayout: ImageLayout.TransferSrcOptimal,
            srcStage: PipelineStageFlags.ComputeShaderBit,
            dstStage: PipelineStageFlags.TransferBit,
            srcAccess: AccessFlags.ShaderWriteBit,
            dstAccess: AccessFlags.TransferReadBit);

        var swapchainImage = swapchain.Images[(int)imageIndex];
        _commandBuffer.ImageBarrier(swapchainImage,
            ImageLayout.Undefined, ImageLayout.TransferDstOptimal,
            PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit,
            0, AccessFlags.TransferWriteBit);

        // Blit storage image to swapchain
        _commandBuffer.BlitFullExtent(
            _storageImage!, ImageLayout.TransferSrcOptimal,
            swapchainImage, ImageLayout.TransferDstOptimal,
            Filter.Nearest);

        _commandBuffer.ImageBarrier(swapchainImage,
            ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr,
            PipelineStageFlags.TransferBit, PipelineStageFlags.BottomOfPipeBit,
            AccessFlags.TransferWriteBit, 0);

        _commandBuffer.End();

        // Submit
        _graphicsQueue.Submit(
            _commandBuffer!,
            waitSemaphores: [_imageAvailableSemaphore!],
            waitStages: [PipelineStageFlags.ColorAttachmentOutputBit],
            signalSemaphores: [_renderFinishedSemaphore!],
            fence: _inFlightFence!);

        // Present
        swapchain.Present(imageIndex, _renderFinishedSemaphore!, _graphicsQueue);
    }

    public void Cleanup()
    {
        VulkanContext.VkDevice.WaitIdle();

        // Cleanup storage image resources
        _storageImageView?.Dispose();
        _storageImage?.Dispose();

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
