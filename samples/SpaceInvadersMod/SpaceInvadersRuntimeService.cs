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
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;
using Sparkitect.Windowing;

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
    private Queue _graphicsQueue;
    private VmaAllocator? _vmaAllocator;
    private VmaImage? _storageImage;
    private ImageView _storageImageView;
    private VmaBuffer? _entityBuffer;
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
        _graphicsQueue = queue.Handle;

        var poolResult = VulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit,
            _graphicsQueueFamily);
        if (poolResult is VkResult<VkCommandPool>.Error poolError)
            throw new InvalidOperationException($"Failed to create command pool: {poolError.errorResult}");
        _commandPool = ((VkResult<VkCommandPool>.Success)poolResult).value;

        var bufferResult = _commandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (bufferResult is VkResult<VkCommandBuffer>.Error bufferError)
            throw new InvalidOperationException($"Failed to allocate command buffer: {bufferError.errorResult}");
        _commandBuffer = ((VkResult<VkCommandBuffer>.Success)bufferResult).value;

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

        _vmaAllocator = VmaAllocator.Create(
            VulkanContext.VkInstance.Handle,
            VulkanContext.VkPhysicalDevice.PhysicalDevice,
            device);

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

        var bufferCreateInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(SpaceInvadersConstants.MaxRenderEntities * sizeof(RenderEntity)),
            Usage = BufferUsageFlags.StorageBufferBit,
            SharingMode = SharingMode.Exclusive
        };
        var bufferAllocInfo = new VmaAllocationCreateInfo
        {
            Usage = VmaMemoryUsage.CpuToGpu,
            Flags = VmaAllocationCreateFlags.Mapped
        };
        _entityBuffer = _vmaAllocator.CreateBuffer(bufferCreateInfo, bufferAllocInfo);

        CreateComputePipeline();
        CreateDescriptorResources();

        Log.Debug("Space Invaders rendering initialized with Vulkan resources");
    }

    private unsafe void CreateComputePipeline()
    {
        if (!ShaderManager.TryGetRegisteredShaderModule(ShaderModuleID.SpaceInvadersMod.SpaceInvaders, out var shaderModule))
            throw new InvalidOperationException("Space Invaders shader not registered");

        var bindings = stackalloc DescriptorSetLayoutBinding[2];
        bindings[0] = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.StorageImage,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        bindings[1] = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 2,
            PBindings = bindings
        };
        var layoutResult = VulkanContext.CreateDescriptorSetLayout(layoutInfo);
        if (layoutResult is VkResult<VkDescriptorSetLayout>.Error layoutError)
            throw new InvalidOperationException($"Failed to create descriptor set layout: {layoutError.errorResult}");
        _descriptorSetLayout = ((VkResult<VkDescriptorSetLayout>.Success)layoutResult).value;

        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = (uint)sizeof(SpaceInvadersGameData)
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
        var poolSizes = stackalloc DescriptorPoolSize[2];
        poolSizes[0] = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageImage,
            DescriptorCount = 1
        };
        poolSizes[1] = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageBuffer,
            DescriptorCount = 1
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 1,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes
        };
        var poolResult = VulkanContext.CreateDescriptorPool(poolInfo);
        if (poolResult is VkResult<VkDescriptorPool>.Error descriptorPoolError)
            throw new InvalidOperationException($"Failed to create descriptor pool: {descriptorPoolError.errorResult}");
        _descriptorPool = ((VkResult<VkDescriptorPool>.Success)poolResult).value;

        var setResult = _descriptorPool.AllocateDescriptorSet(_descriptorSetLayout!.Handle);
        if (setResult is VkResult<VkDescriptorSet>.Error setError)
            throw new InvalidOperationException($"Failed to allocate descriptor set: {setError.errorResult}");
        _descriptorSet = ((VkResult<VkDescriptorSet>.Success)setResult).value;

        var imageInfo = new DescriptorImageInfo
        {
            ImageView = _storageImageView,
            ImageLayout = ImageLayout.General
        };
        var imageWrite = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _descriptorSet.Handle,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            PImageInfo = &imageInfo
        };
        VulkanContext.VkApi.UpdateDescriptorSets(VulkanContext.VkDevice.Handle, 1, imageWrite, 0, null);

        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = _entityBuffer!.Buffer,
            Offset = 0,
            Range = (ulong)(SpaceInvadersConstants.MaxRenderEntities * sizeof(RenderEntity))
        };
        var bufferWrite = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _descriptorSet.Handle,
            DstBinding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &bufferInfo
        };
        VulkanContext.VkApi.UpdateDescriptorSets(VulkanContext.VkDevice.Handle, 1, bufferWrite, 0, null);
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
        var mappedPtr = _entityBuffer!.AllocationInfo.MappedData;
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

        var vk = VulkanContext.VkApi;
        var device = VulkanContext.VkDevice.Handle;
        var swapchain = _window.Swapchain;

        _inFlightFence!.Wait();
        _inFlightFence.Reset();

        var acquireResult = swapchain.AcquireNextImage(_imageAvailableSemaphore!, autoRecreate: true);
        if (acquireResult is VkResult<uint>.Error)
            return;
        var imageIndex = ((VkResult<uint>.Success)acquireResult).value;

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

        var gameData = new SpaceInvadersGameData
        {
            EntityCount = (uint)_renderEntityCount,
            ScreenWidth = swapchain.Extent.Width,
            ScreenHeight = swapchain.Extent.Height,
            Padding = 0f,
            BackgroundColor = new Vector3(0.05f, 0.05f, 0.1f)
        };
        vk.CmdPushConstants(cmd, _pipelineLayout.Handle, ShaderStageFlags.ComputeBit, 0, (uint)sizeof(SpaceInvadersGameData), &gameData);

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
        blitRegion.SrcOffsets[0] = new Offset3D(0, 0, 0);
        blitRegion.SrcOffsets[1] = new Offset3D((int)swapchain.Extent.Width, (int)swapchain.Extent.Height, 1);
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

        // Frame pacing: yield CPU to prevent uncapped spin loop causing GPU coil whine.
        // The Vulkan fence wait provides GPU-side throttling, but without this sleep the
        // CPU-side loop spins at maximum speed between frames (especially when GPU work is trivial).
        Thread.Sleep(1);
    }

    public unsafe void CleanupRendering()
    {
        VulkanContext.VkApi.DeviceWaitIdle(VulkanContext.VkDevice.Handle);

        VulkanContext.VkApi.DestroyImageView(VulkanContext.VkDevice.Handle, _storageImageView, null);
        _storageImage?.Dispose();
        _entityBuffer?.Dispose();
        _vmaAllocator?.Dispose();

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
