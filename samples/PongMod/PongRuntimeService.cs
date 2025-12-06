using System.Diagnostics;
using System.Numerics;
using Serilog;
using Serilog.Core;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
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

    public required IWindowManager WindowManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

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

        Log.Debug("Pong runtime initialized with Vulkan resources");
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

        // Transition image to transfer dst
        var barrier = new ImageMemoryBarrier
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
            0, 0, null, 0, null, 1, barrier);

        // Clear to blue
        var clearColor = new ClearColorValue(0.0f, 0.2f, 0.4f, 1.0f);
        var range = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1
        };
        vk.CmdClearColorImage(cmd, swapchain.Images[(int)imageIndex].Handle,
            ImageLayout.TransferDstOptimal, clearColor, 1, range);

        // Transition to present
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.PresentSrcKhr;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = 0;
        vk.CmdPipelineBarrier(cmd,
            PipelineStageFlags.TransferBit,
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
