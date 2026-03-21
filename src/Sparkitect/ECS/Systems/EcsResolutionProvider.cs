using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;

namespace Sparkitect.ECS.Systems;

internal class EcsResolutionProvider : IResolutionProvider
{
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        // Skeletal: full query resolution logic arrives in Phase 30
        service = null;
        return false;
    }
}
