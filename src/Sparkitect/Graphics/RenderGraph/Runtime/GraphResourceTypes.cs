using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Metadata;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

[StateService<IGraphResourceTypes, RenderGraphModule>]
internal sealed class GraphResourceTypes :
    IGraphResourceTypes,
    IGraphResourceTypesRegistryFacade,
    IGraphResourceTypesStateFacade
{
    private readonly HashSet<Identification> _tracked = [];
    private readonly Dictionary<Identification, Type> _managerByResourceId = [];

    public required IDIService DIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

    public void AddResource(Identification id) => _tracked.Add(id);

    public void PostProcess()
    {
        var modIdList = GameStateManager.LoadedMods;
        var bindings = new Dictionary<Identification, ResourceManagerBinding>();
        using var container = DIService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<ResourceManagerBinding>>(modIdList);
        container.ProcessMany(ep => ep.CollectMetadata(bindings));

        _managerByResourceId.Clear();
        foreach (var id in _tracked)
        {
            if (!bindings.TryGetValue(id, out var binding))
                throw new InvalidOperationException(
                    $"Resource id {id} is tracked via GraphResourceRegistry " +
                    "but no [ResourceManager<T>] metadata was found.");
            _managerByResourceId[id] = binding.ManagerType;
        }
    }

    public Type GetManagerTypeFor(Identification id) =>
        _managerByResourceId.TryGetValue(id, out var t)
            ? t
            : throw new KeyNotFoundException(
                $"No resource manager registered for resource id {id}.");

    public bool TryGetManagerType(Identification id, out Type managerType) =>
        _managerByResourceId.TryGetValue(id, out managerType!);
}
