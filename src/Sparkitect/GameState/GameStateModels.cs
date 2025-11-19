using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Internal metadata for a registered state
/// </summary>
internal sealed record StateMetadata(
    Identification Id,
    Identification ParentId,
    IReadOnlyList<Identification> ModuleIds,
    Type DescriptorType);

/// <summary>
/// Internal metadata for a registered module
/// </summary>
internal sealed record ModuleMetadata(
    Identification Id,
    IReadOnlyList<Identification> RequiredModules,
    Type ModuleType);

/// <summary>
/// Represents an active state frame in the state stack
/// </summary>
internal sealed record ActiveStateFrame(
    Identification StateId,
    ICoreContainer Container,
    IReadOnlyList<IStateMethod> PerFrameMethods,
    IReadOnlyList<IStateMethod> OnCreateMethods,
    IReadOnlyList<IStateMethod> OnDestroyMethods,
    IReadOnlyList<IStateMethod> OnFrameEnterMethods,
    IReadOnlyList<IStateMethod> OnFrameExitMethods);
