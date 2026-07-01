using System.Collections.Immutable;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The graph-local descriptor-layout cache. Lives in the per-graph child DI container (fact-ctor
/// injectable); get-or-creates a push-descriptor <see cref="VkDescriptorSetLayout"/> keyed by the
/// ordered binding shape, deduping identical shapes intra-graph, and disposes every owned layout at
/// graph teardown. The graph calls <c>DeviceWaitIdle</c> before disposing its graph-local services, so
/// disposal here is safe (D-12).
/// </summary>
[PublicAPI]
[GraphLocal<IDescriptorLayoutCache, IRenderGraph>]
public sealed class DescriptorLayoutCache : IDescriptorLayoutCache, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly Dictionary<ShapeKey, VkDescriptorSetLayout> _layouts = new();

    public DescriptorLayoutCache(IVulkanContext vulkanContext) => _vulkanContext = vulkanContext;

    /// <inheritdoc/>
    public VkDescriptorSetLayout GetOrCreate(ImmutableArray<DescriptorLayoutBinding> bindings)
    {
        if (bindings.IsDefaultOrEmpty)
            throw new InvalidOperationException(
                "DescriptorLayoutCache.GetOrCreate: an empty binding shape was requested. " +
                "A descriptor-set layout must declare at least one binding.");

        var key = new ShapeKey(bindings);
        if (_layouts.TryGetValue(key, out var cached))
            return cached;

        var layout = CreateLayout(bindings);
        _layouts.Add(key, layout);
        return layout;
    }

    public void Dispose()
    {
        foreach (var layout in _layouts.Values)
            layout.Dispose();
        _layouts.Clear();
    }

    private VkDescriptorSetLayout CreateLayout(ImmutableArray<DescriptorLayoutBinding> bindings)
    {
        var layoutBindings = ImmutableArray.CreateBuilder<DescriptorSetLayoutBinding>(bindings.Length);
        foreach (var binding in bindings)
            layoutBindings.Add(new DescriptorSetLayoutBinding
            {
                Binding = binding.Binding,
                DescriptorType = binding.Type,
                DescriptorCount = 1,
                StageFlags = binding.Stages,
            });

        var result = _vulkanContext.CreateDescriptorSetLayout(
            new VkDescriptorSetLayoutCreateOptions(
                layoutBindings.MoveToImmutable(),
                Flags: DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr));

        if (result is not Result<VkDescriptorSetLayout, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                "DescriptorLayoutCache.GetOrCreate: failed to create the push-descriptor layout " +
                $"({((Result<VkDescriptorSetLayout, VkApiResult>.Error)result).Value}).");

        return ok.Value;
    }

    /// <summary>
    /// Value-equal key over an ordered binding shape. <see cref="DescriptorLayoutBinding"/> is a
    /// <c>record struct</c>, so the sequence compares and hashes element-wise.
    /// </summary>
    private readonly struct ShapeKey : IEquatable<ShapeKey>
    {
        private readonly ImmutableArray<DescriptorLayoutBinding> _bindings;
        private readonly int _hash;

        public ShapeKey(ImmutableArray<DescriptorLayoutBinding> bindings)
        {
            _bindings = bindings;
            var hash = new HashCode();
            foreach (var binding in bindings)
                hash.Add(binding);
            _hash = hash.ToHashCode();
        }

        public bool Equals(ShapeKey other)
        {
            if (_bindings.Length != other._bindings.Length)
                return false;
            for (var i = 0; i < _bindings.Length; i++)
                if (!_bindings[i].Equals(other._bindings[i]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is ShapeKey key && Equals(key);

        public override int GetHashCode() => _hash;
    }
}
