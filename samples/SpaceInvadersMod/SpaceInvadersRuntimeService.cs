using System.Diagnostics;
using System.Numerics;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace SpaceInvadersMod;

[StateService<ISpaceInvadersRuntimeService, SpaceInvadersModule>]
public class SpaceInvadersRuntimeService(IComponentManager componentManager, ISystemManager systemManager)
    : ISpaceInvadersRuntimeService, ISpaceInvadersRuntimeServiceStateFacade
{
    private IWorld? _world;
    private IObjectTracker<IDisposable>? _tracker;
    private ICommandBufferAccessor? _commandBufferAccessor;
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    private RenderEntity[] _renderBuffer = new RenderEntity[SpaceInvadersConstants.MaxRenderEntities];
    private int _renderEntityCount;
    private bool _isGameplayActive;
    private ISparkitWindow? _window;
    private StorageHandle _playerStorageHandle;
    private StorageHandle _enemyStorageHandle;

    // Vulkan rendering resources
    private VkCommandPool? _commandPool;
    private VkCommandBuffer? _commandBuffer;
    private VkSemaphore? _imageAvailableSemaphore;
    private VkSemaphore? _renderFinishedSemaphore;
    private VkFence? _inFlightFence;
    private uint _graphicsQueueFamily;
    private VkQueue _graphicsQueue = null!;
    private VkImage? _storageImage;
    private VkImageView? _storageImageView;
    private VkBuffer? _entityBuffer;
    private VkDescriptorSetLayout? _descriptorSetLayout;
    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;
    private VkDescriptorPool? _descriptorPool;
    private VkDescriptorSet? _descriptorSet;

    // Input: physical key → action mapping, cached per frame
    private static readonly Dictionary<Key, GameAction> KeyMap = new()
    {
        [Key.Left] = GameAction.MoveLeft,
        [Key.A] = GameAction.MoveLeft,
        [Key.Right] = GameAction.MoveRight,
        [Key.D] = GameAction.MoveRight,
        [Key.Up] = GameAction.Shoot,
        [Key.Space] = GameAction.Shoot,
        [Key.P] = GameAction.TogglePause,
        [Key.R] = GameAction.Restart,
    };

    private readonly HashSet<GameAction> _activeActions = [];
    private bool _pauseToggleConsumed;
    private bool _restartConsumed;

    public required IWindowManager WindowManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required IShaderManager ShaderManager { private get; init; }

    public IWorld? GetWorld() => _world;
    public RenderEntity[] GetRenderBuffer() => _renderBuffer;
    public void SetRenderEntityCount(int count) => _renderEntityCount = count;
    public int GetRenderEntityCount() => _renderEntityCount;
    public bool IsGameplayActive => _isGameplayActive;
    public void SetGameplayActive(bool active) => _isGameplayActive = active;

    public bool IsActionDown(GameAction action) => _activeActions.Contains(action);

    public void ProcessInput()
    {
        _activeActions.Clear();

        var keyboard = _window?.Keyboard;
        if (keyboard is null) return;

        foreach (var (key, action) in KeyMap)
        {
            if (keyboard.IsKeyDown(key))
                _activeActions.Add(action);
        }

        // Edge-detect pause toggle: only fire once per press
        if (_activeActions.Contains(GameAction.TogglePause))
        {
            if (_pauseToggleConsumed)
                _activeActions.Remove(GameAction.TogglePause);
            else
                _pauseToggleConsumed = true;
        }
        else
        {
            _pauseToggleConsumed = false;
        }

        // Edge-detect restart: only fire once per press
        if (_activeActions.Contains(GameAction.Restart))
        {
            if (_restartConsumed)
                _activeActions.Remove(GameAction.Restart);
            else
                _restartConsumed = true;
        }
        else
        {
            _restartConsumed = false;
        }
    }

    public void CheckGameState()
    {
        if (_world is null) return;

        if (_isGameplayActive)
        {
            var playerCount = _world.GetStorage(_playerStorageHandle).Count;
            var enemyCount = _world.GetStorage(_enemyStorageHandle).Count;

            if (playerCount == 0 || enemyCount == 0)
            {
                // Auto-reset: rebuild world immediately on death/victory
                DestroyWorld();
                BuildWorld();
                // Start paused so player can see fresh board before pressing Space
            }
            else if (IsActionDown(GameAction.TogglePause))
            {
                _isGameplayActive = false;
                _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
            }
        }
        else if (IsActionDown(GameAction.Restart))
        {
            // Rebuild the world to restart the game
            DestroyWorld();
            BuildWorld();
            _isGameplayActive = true;
            _world!.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
        }
        else if (IsActionDown(GameAction.TogglePause) || IsActionDown(GameAction.Shoot))
        {
            _isGameplayActive = true;
            _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
        }
    }

    public IWorld BuildWorld()
    {
        _world = IWorld.Create();
        _tracker = new ObjectTracker<IDisposable>();
        _frameTimer.Restart();
        _lastFrameTime = 0f;

        (Identification, int) Meta(Identification id) => (id, componentManager.GetSize(id));

        var posId = UnmanagedComponentID.SpaceInvadersMod.Position;
        var velId = UnmanagedComponentID.SpaceInvadersMod.Velocity;
        var cooldownId = UnmanagedComponentID.SpaceInvadersMod.ShootCooldown;
        var playerTagId = UnmanagedComponentID.SpaceInvadersMod.PlayerTag;
        var enemyTagId = UnmanagedComponentID.SpaceInvadersMod.EnemyTag;
        var bulletDataId = UnmanagedComponentID.SpaceInvadersMod.BulletData;

        // Player archetype: Position, Velocity, ShootCooldown, PlayerTag
        var playerStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(cooldownId), Meta(playerTagId)],
            _tracker, _world, initialCapacity: 4);

        // Enemy archetype: Position, Velocity, ShootCooldown, EnemyTag
        var enemyStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(cooldownId), Meta(enemyTagId)],
            _tracker, _world, initialCapacity: 64);

        // Bullet archetype: Position, Velocity, BulletData
        var bulletStorage = new SoAStorage(
            [Meta(posId), Meta(velId), Meta(bulletDataId)],
            _tracker, _world, initialCapacity: 32);

        var playerHandle = _world.AddStorage(playerStorage, playerStorage.CreateCapabilityRegistrations());
        var enemyHandle = _world.AddStorage(enemyStorage, enemyStorage.CreateCapabilityRegistrations());
        var bulletHandle = _world.AddStorage(bulletStorage, bulletStorage.CreateCapabilityRegistrations());
        
        playerStorage.SetHandle(playerHandle);
        enemyStorage.SetHandle(enemyHandle);
        bulletStorage.SetHandle(bulletHandle);

        _playerStorageHandle = playerHandle;
        _enemyStorageHandle = enemyHandle;

        _world.SetSystemTree(systemManager.BuildTree(EcsSystemGroupID.SpaceInvadersMod.SpaceInvaders));

        // Spawn player entity
        var playerAccessor = _world.GetStorage(playerHandle);
        var playerSlot = playerAccessor.AsStorage<int>()!.AllocateEntity();
        var playerComponents = playerAccessor.As<IComponentAccess<int>>()!;
        var playerIdAccessor = playerAccessor.As<IEntityIdentity<int>>()!;
        playerIdAccessor.Assign(_world.AllocateEntityId(), playerSlot);
        playerComponents.Set(playerSlot, new Position { Value = new Vector2(0.5f, 0.9f) });
        playerComponents.Set(playerSlot, new Velocity { Value = Vector2.Zero });
        playerComponents.Set(playerSlot, new ShootCooldown { Remaining = 0f });
        playerComponents.Set(playerSlot, new PlayerTag());

        // Spawn enemy formation: 5 rows x 11 cols
        var enemyAccessor = _world.GetStorage(enemyHandle);
        var enemySlotAllocator = enemyAccessor.AsStorage<int>()!;
        var enemyComponents = enemyAccessor.As<IComponentAccess<int>>()!;
        var enemyIdAccessor = enemyAccessor.As<IEntityIdentity<int>>()!;

        const int rows = 5;
        const int cols = 11;
        const float startX = 0.15f;
        const float startY = 0.1f;
        const float spacingX = 0.07f;
        const float spacingY = 0.06f;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var slot = enemySlotAllocator.AllocateEntity();
                enemyIdAccessor.Assign(_world.AllocateEntityId(), slot);
                enemyComponents.Set(slot, new Position { Value = new Vector2(startX + col * spacingX, startY + row * spacingY) });
                enemyComponents.Set(slot, new Velocity { Value = Vector2.Zero });
                enemyComponents.Set(slot, new ShootCooldown { Remaining = SpaceInvadersConstants.EnemyShootCooldownMin +
                    Random.Shared.NextSingle() * (SpaceInvadersConstants.EnemyShootCooldownMax - SpaceInvadersConstants.EnemyShootCooldownMin) });
                enemyComponents.Set(slot, new EnemyTag());
            }
        }

        systemManager.NotifyRebuild(_world);

        // Per D-11: Game starts with GameplayGroup inactive (paused)
        _world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
        _isGameplayActive = false;

        return _world;
    }

    public void SimulateWorld()
    {
        if (_world is null) return;

        var currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        systemManager.ExecuteSystems(_world, new FrameTiming(deltaTime, currentTime));

        _commandBufferAccessor ??= systemManager.GetCommandBufferAccessor(_world);
        _commandBufferAccessor?.Playback();
    }

    public void DestroyWorld()
    {
        _commandBufferAccessor = null;
        _frameTimer.Stop();
        _world?.Dispose();
        _world = null;
    }

    public unsafe void InitializeRendering()
    {
        _window = WindowManager.CreateWindow("Space Invaders", 800, 600);

        var physicalDevice = VulkanContext.VkPhysicalDevice;
        _graphicsQueueFamily = FindGraphicsQueueFamily(physicalDevice);
        var queue = VulkanContext.GetQueue(_graphicsQueueFamily, 0);
        if (queue == null)
            throw new InvalidOperationException("No graphics queue available");
        _graphicsQueue = queue;

        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            _graphicsQueueFamily);
        if (poolResult is Result<VkCommandPool, VkApiResult>.Error poolError)
            throw new InvalidOperationException($"Failed to create command pool: {poolError.Value}");
        _commandPool = ((Result<VkCommandPool, VkApiResult>.Ok)poolResult).Value;

        var bufferResult = _commandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (bufferResult is Result<VkCommandBuffer, VkApiResult>.Error bufferError)
            throw new InvalidOperationException($"Failed to allocate command buffer: {bufferError.Value}");
        _commandBuffer = ((Result<VkCommandBuffer, VkApiResult>.Ok)bufferResult).Value;

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

        var swapchain = _window!.Swapchain;
        var storageImageResult = VulkanContext.CreateStorageImage2D(swapchain.Extent, Format.R8G8B8A8Unorm);
        if (storageImageResult is Result<VkImage, VkApiResult>.Error storageImageError)
            throw new InvalidOperationException($"Failed to create storage image: {storageImageError.Value}");
        _storageImage = ((Result<VkImage, VkApiResult>.Ok)storageImageResult).Value;

        var storageViewResult = _storageImage!.CreateView(ImageAspectFlags.ColorBit);
        if (storageViewResult is Result<VkImageView, VkApiResult>.Error storageViewErr)
            throw new InvalidOperationException($"Failed to create storage image view: {storageViewErr.Value}");
        _storageImageView = ((Result<VkImageView, VkApiResult>.Ok)storageViewResult).Value;

        var entityBufferSize = (ulong)(SpaceInvadersConstants.MaxRenderEntities * sizeof(RenderEntity));
        var entityBufferResult = VulkanContext.CreateMappedStorageBuffer(entityBufferSize);
        if (entityBufferResult is Result<VkBuffer, VkApiResult>.Error entityBufferError)
            throw new InvalidOperationException($"Failed to create entity buffer: {entityBufferError.Value}");
        _entityBuffer = ((Result<VkBuffer, VkApiResult>.Ok)entityBufferResult).Value;

        CreateComputePipeline();
        CreateDescriptorResources();

        Log.Debug("Space Invaders rendering initialized with Vulkan resources");
    }

    private unsafe void CreateComputePipeline()
    {
        if (!ShaderManager.TryGetRegisteredShaderModule(ShaderModuleID.SpaceInvadersMod.SpaceInvaders, out var shaderModule))
            throw new InvalidOperationException("Space Invaders shader not registered");

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
                new DescriptorSetLayoutBinding
                {
                    Binding = 1,
                    DescriptorType = DescriptorType.StorageBuffer,
                    DescriptorCount = 1,
                    StageFlags = ShaderStageFlags.ComputeBit,
                },
            ]));
        if (layoutResult is Result<VkDescriptorSetLayout, VkApiResult>.Error layoutError)
            throw new InvalidOperationException($"Failed to create descriptor set layout: {layoutError.Value}");
        _descriptorSetLayout = ((Result<VkDescriptorSetLayout, VkApiResult>.Ok)layoutResult).Value;

        var pipelineLayoutResult = VulkanContext.CreatePipelineLayout(
            new VkPipelineLayoutCreateOptions(
                SetLayouts: [_descriptorSetLayout!],
                PushConstantRanges:
                [
                    new PushConstantRange
                    {
                        StageFlags = ShaderStageFlags.ComputeBit,
                        Offset = 0,
                        Size = (uint)sizeof(SpaceInvadersGameData),
                    },
                ]));
        if (pipelineLayoutResult is Result<VkPipelineLayout, VkApiResult>.Error pipelineLayoutError)
            throw new InvalidOperationException($"Failed to create pipeline layout: {pipelineLayoutError.Value}");
        _pipelineLayout = ((Result<VkPipelineLayout, VkApiResult>.Ok)pipelineLayoutResult).Value;

        var pipelineResult = VulkanContext.CreateComputePipeline(
            new VkComputePipelineCreateOptions(shaderModule, _pipelineLayout!));
        if (pipelineResult is Result<VkPipeline, VkApiResult>.Error pipelineError)
            throw new InvalidOperationException($"Failed to create compute pipeline: {pipelineError.Value}");
        _computePipeline = ((Result<VkPipeline, VkApiResult>.Ok)pipelineResult).Value;
    }

    private void CreateDescriptorResources()
    {
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
                    new DescriptorPoolSize
                    {
                        Type = DescriptorType.StorageBuffer,
                        DescriptorCount = 1,
                    },
                ]));
        if (poolResult is Result<VkDescriptorPool, VkApiResult>.Error descriptorPoolError)
            throw new InvalidOperationException($"Failed to create descriptor pool: {descriptorPoolError.Value}");
        _descriptorPool = ((Result<VkDescriptorPool, VkApiResult>.Ok)poolResult).Value;

        var setResult = _descriptorPool.AllocateDescriptorSet(_descriptorSetLayout!.Handle);
        if (setResult is Result<VkDescriptorSet, VkApiResult>.Error setError)
            throw new InvalidOperationException($"Failed to allocate descriptor set: {setError.Value}");
        _descriptorSet = ((Result<VkDescriptorSet, VkApiResult>.Ok)setResult).Value;

        _descriptorSet!.WriteStorageImage(binding: 0, _storageImageView!, ImageLayout.General);
        _descriptorSet!.WriteStorageBuffer(binding: 1, _entityBuffer!);
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

    public unsafe void Render()
    {
        if (_window is null) return;

        _window.PollEvents();
        if (!_window.IsOpen)
        {
            GameStateManager.Shutdown();
            return;
        }

        // Copy render entities to mapped SSBO
        var mappedPtr = _entityBuffer!.MappedData;
        var renderCount = _renderEntityCount;
        if (renderCount > 0)
        {
            fixed (RenderEntity* src = _renderBuffer)
            {
                System.Buffer.MemoryCopy(src, (void*)mappedPtr,
                    SpaceInvadersConstants.MaxRenderEntities * sizeof(RenderEntity),
                    renderCount * sizeof(RenderEntity));
            }
        }

        var swapchain = _window.Swapchain;

        _inFlightFence!.Wait();
        _inFlightFence.Reset();

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

        var gameData = new SpaceInvadersGameData
        {
            EntityCount = (uint)_renderEntityCount,
            ScreenWidth = swapchain.Extent.Width,
            ScreenHeight = swapchain.Extent.Height,
            Padding = 0f,
            BackgroundColor = new Vector3(0.05f, 0.05f, 0.1f)
        };
        _commandBuffer.PushConstants(_pipelineLayout!, ShaderStageFlags.ComputeBit, 0, in gameData);

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

        // Frame pacing: yield CPU to prevent uncapped spin loop causing GPU coil whine.
        // The Vulkan fence wait provides GPU-side throttling, but without this sleep the
        // CPU-side loop spins at maximum speed between frames (especially when GPU work is trivial).
        Thread.Sleep(1);
    }

    public void CleanupRendering()
    {
        VulkanContext.VkDevice.WaitIdle();

        _storageImageView?.Dispose();
        _storageImage?.Dispose();
        _entityBuffer?.Dispose();

        _descriptorPool?.Dispose();
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
        _descriptorSetLayout?.Dispose();

        _inFlightFence?.Dispose();
        _renderFinishedSemaphore?.Dispose();
        _imageAvailableSemaphore?.Dispose();

        _commandPool?.Dispose();
        _window?.Dispose();

        Log.Debug("Space Invaders rendering cleanup complete");
    }
}
