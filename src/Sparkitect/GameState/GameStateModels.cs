using Sparkitect.DI.Container;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

internal sealed record StateMetadata(
    Identification Id,
    Identification ParentId,
    IReadOnlyList<Identification> ModuleIds,
    Type DescriptorType);

internal sealed record ModuleMetadata(
    Identification Id,
    IReadOnlyList<Identification> RequiredModules,
    Type ModuleType);

internal sealed record ActiveStateFrame(
    Identification StateId,
    ICoreContainer Container,
    IReadOnlyList<string> AddedMods,
    IReadOnlyList<IStatelessFunction> TransitionEnterMethods,
    IReadOnlyList<IStatelessFunction> TransitionExitMethods,
    IReadOnlyList<IStatelessFunction> PerFrameMethods);
