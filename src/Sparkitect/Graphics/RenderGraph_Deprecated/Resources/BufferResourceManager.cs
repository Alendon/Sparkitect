using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Single device storage-buffer manager. Owns the device-local backings for the whole buffer
/// family, drains registered buffers at Setup, and resolves <c>FromRegistered</c> declarations
/// against them. Mirrors <see cref="ImageResourceManager"/> for the buffer family; it does not
/// author barrier emission — the staging pass records the copy and barrier.
/// </summary>
[GraphLocal<IBufferResourceManager>]
public sealed class BufferResourceManager : IBufferResourceManager, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly IResourceRegistrationStore _registrationStore;
    private readonly Dictionary<Identification, VkBuffer> _registeredBackings = new();
    private readonly Dictionary<Identification, ulong> _capacities = new();

    public BufferResourceManager(IVulkanContext vulkanContext, IResourceRegistrationStore registrationStore)
    {
        _vulkanContext = vulkanContext;
        _registrationStore = registrationStore;
    }

    public void DrainRegisteredBuffers()
    {
        foreach (var (id, description) in _registrationStore.RegisteredBuffers)
        {
            if (_registeredBackings.ContainsKey(id)) continue;

            var capacity = BufferCapacity.NextCapacity(0, description.InitialCapacity);
            _capacities[id] = capacity;
            _registeredBackings[id] = AllocateBacking(description.ElementStride * capacity);
        }
    }

    public IGraphResource<StorageBufferView> Declare(
        Identification passId, int slot, BufferRequest request)
    {
        return request switch
        {
            BufferRequest.FromRegistered registered
                => new RegisteredStorageBufferViewHandle(
                    slot,
                    new StorageBufferView(ResolveRegistered(registered.Id, passId))),
        };
    }

    private VkBuffer ResolveRegistered(Identification id, Identification passId)
    {
        if (_registeredBackings.TryGetValue(id, out var backing))
            return backing;

        throw new InvalidOperationException(
            $"BufferResourceManager.Declare: pass {passId} declared FromRegistered({id}) but no shared backing " +
            $"was drained for that identification. Register the buffer via GraphBufferRegistry before graph setup.");
    }

    private VkBuffer AllocateBacking(ulong byteSize)
    {
        var result = _vulkanContext.CreateDeviceStorageBuffer(byteSize);
        if (result is not Result<VkBuffer, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"BufferResourceManager: CreateDeviceStorageBuffer failed ({((Result<VkBuffer, VkApiResult>.Error)result).Value}).");

        return ok.Value;
    }

    public void Dispose()
    {
        foreach (var backing in _registeredBackings.Values)
            backing.Dispose();
        _registeredBackings.Clear();
        _capacities.Clear();
    }

    private sealed class RegisteredStorageBufferViewHandle : IGraphResource<StorageBufferView>
    {
        private readonly StorageBufferView _view;
        public RegisteredStorageBufferViewHandle(int slot, StorageBufferView view) { Slot = slot; _view = view; }
        public int Slot { get; }
        public StorageBufferView Fetch() => _view;
    }
}
