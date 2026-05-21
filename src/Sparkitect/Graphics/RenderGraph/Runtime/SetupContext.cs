using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Setup-time dispatcher: resolves a resource type's manager via
/// <see cref="IGraphResourceTypes"/> and forwards <c>Declare</c> calls. The
/// active-pass bracket gives every pass a fresh slot counter starting at 0.
/// </summary>
internal sealed class SetupContext : ISetupContext
{
    private readonly IGraphResourceTypes _resourceTypes;
    private readonly IReadOnlyDictionary<Type, IGraphResourceManager> _managersByType;
    private Identification _activePassId;
    private int _nextSlotForActivePass;

    internal SetupContext(
        IGraphResourceTypes resourceTypes,
        IReadOnlyDictionary<Type, IGraphResourceManager> managersByType)
    {
        _resourceTypes = resourceTypes;
        _managersByType = managersByType;
    }

    internal void PushPass(Identification passId)
    {
        _activePassId = passId;
        _nextSlotForActivePass = 0;
    }

    internal void PopPass()
    {
        _activePassId = default;
        _nextSlotForActivePass = 0;
    }

    public IGraphResource<TResource> Declare<TResource>(IResourceRequest<TResource> request)
        where TResource : IHasIdentification
    {
        var resourceId = TResource.Identification;
        if (!_resourceTypes.TryGetManagerType(resourceId, out var managerType))
            throw new InvalidOperationException(
                $"No [ResourceManager<…>] binding registered for resource type {typeof(TResource).FullName} (id={resourceId}).");
        if (!_managersByType.TryGetValue(managerType, out var manager))
            throw new InvalidOperationException(
                $"Resource manager {managerType.FullName} declared by {typeof(TResource).FullName} is not registered with the render graph.");
        if (manager is not IGraphResourceManagerFor<TResource> typed)
            throw new InvalidOperationException(
                $"Resource manager {manager.GetType().FullName} does not implement IGraphResourceManagerFor<{typeof(TResource).Name}>.");

        var slot = _nextSlotForActivePass++;
        return typed.DeclareUntyped(_activePassId, slot, request);
    }
}
