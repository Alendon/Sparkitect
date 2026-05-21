using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

[StateService<IPassTypes, RenderGraphModule>]
internal sealed class PassTypes : IPassTypes, IPassTypesRegistryFacade
{
    private readonly HashSet<Identification> _tracked = [];

    public void AddPass(Identification id) => _tracked.Add(id);

    public IReadOnlyCollection<Identification> RegisteredPassIds => _tracked;
}
