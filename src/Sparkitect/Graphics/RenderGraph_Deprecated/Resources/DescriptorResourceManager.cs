using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Push-descriptor resource manager. At Declare it fail-fast validates the binding config, derives a
/// <c>VkDescriptorSetLayout</c> with <see cref="DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr"/>
/// from each bound view's static <see cref="IDescriptorBindingSource.DescriptorType"/>, constructs the
/// <see cref="Descriptor"/>, and owns the layout lifetime (no pool, no allocated sets). Pushes happen
/// inline in pass Execute via <see cref="Descriptor.Push"/> — there is no per-frame hook.
/// </summary>
[GraphLocal<IDescriptorResourceManager>]
[PublicAPI]
public sealed class DescriptorResourceManager : IDescriptorResourceManager, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly List<VkDescriptorSetLayout> _layouts = new();

    internal DescriptorResourceManager(IVulkanContext vulkanContext)
    {
        _vulkanContext = vulkanContext;
    }

    public IGraphResource<Descriptor> Declare(Identification passId, int slot, DescriptorRequest request)
    {
        var bindings = request.Bindings;

        if (bindings.IsDefaultOrEmpty)
            throw new InvalidOperationException(
                $"DescriptorResourceManager.Declare: pass {passId} slot {slot} declared a descriptor " +
                "with an empty binding set. A descriptor must bind at least one view.");

        var seen = new HashSet<(uint Binding, uint ArrayIndex)>();
        var layoutBindings = ImmutableArray.CreateBuilder<DescriptorSetLayoutBinding>(bindings.Length);

        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];

            if (!seen.Add((binding.Binding, binding.ArrayIndex)))
                throw new InvalidOperationException(
                    $"DescriptorResourceManager.Declare: pass {passId} slot {slot} has a duplicate " +
                    $"(binding {binding.Binding}, arrayIndex {binding.ArrayIndex}) at binding index {i}.");

            var source = ResolveBindingSource(binding, passId, slot, i);
            var descriptorType = source.DescriptorType;

            if (descriptorType is not (DescriptorType.StorageImage or DescriptorType.StorageBuffer))
                throw new InvalidOperationException(
                    $"DescriptorResourceManager.Declare: pass {passId} slot {slot} binding index {i} " +
                    $"(binding {binding.Binding}) has unsupported descriptor type {descriptorType}. " +
                    "Only StorageImage and StorageBuffer are supported.");

            layoutBindings.Add(new DescriptorSetLayoutBinding
            {
                Binding = binding.Binding,
                DescriptorType = descriptorType,
                DescriptorCount = 1,
                StageFlags = request.Stages,
            });
        }

        var layout = CreateLayout(layoutBindings.MoveToImmutable(), passId, slot);
        _layouts.Add(layout);

        var descriptor = new Descriptor(layout, bindings);
        return new DescriptorHandle(slot, descriptor);
    }

    public void Dispose()
    {
        foreach (var layout in _layouts)
            layout.Dispose();
        _layouts.Clear();
    }

    private static IDescriptorBindingSource ResolveBindingSource(
        DescriptorBinding binding, Identification passId, int slot, int index)
    {
        if (binding.View is null)
            throw new InvalidOperationException(
                $"DescriptorResourceManager.Declare: pass {passId} slot {slot} binding index {index} " +
                $"(binding {binding.Binding}) has a null view handle.");

        IDescriptorBindingSource? source;
        try
        {
            source = binding.View.Fetch();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"DescriptorResourceManager.Declare: pass {passId} slot {slot} binding index {index} " +
                $"(binding {binding.Binding}) has an unresolved view handle.", ex);
        }

        return source ?? throw new InvalidOperationException(
            $"DescriptorResourceManager.Declare: pass {passId} slot {slot} binding index {index} " +
            $"(binding {binding.Binding}) resolved to a null binding source.");
    }

    private VkDescriptorSetLayout CreateLayout(
        ImmutableArray<DescriptorSetLayoutBinding> layoutBindings, Identification passId, int slot)
    {
        var result = _vulkanContext.CreateDescriptorSetLayout(
            new VkDescriptorSetLayoutCreateOptions(
                layoutBindings,
                Flags: DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr));

        if (result is not Result<VkDescriptorSetLayout, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"DescriptorResourceManager.Declare: pass {passId} slot {slot} failed to create the " +
                $"push-descriptor layout ({((Result<VkDescriptorSetLayout, VkApiResult>.Error)result).Value}).");

        return ok.Value;
    }

    private sealed class DescriptorHandle : IGraphResource<Descriptor>
    {
        private readonly Descriptor _descriptor;

        public DescriptorHandle(int slot, Descriptor descriptor)
        {
            Slot = slot;
            _descriptor = descriptor;
        }

        public int Slot { get; }

        public Descriptor Fetch() => _descriptor;
    }
}
