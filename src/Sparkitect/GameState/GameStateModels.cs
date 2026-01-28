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

/// <summary>
/// Represents an active state in the state stack with its cached dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Wrapper lists (TransitionEnterMethods, TransitionExitMethods, PerFrameMethods) are created
/// once at state frame entry via <c>StatelessFunctionManager.GetSorted</c> and cached for the
/// lifetime of the state frame. This avoids per-frame allocation overhead from Activator.CreateInstance.
/// </para>
/// <para>
/// Lifecycle:
/// <list type="bullet">
///   <item><description>Created: When state is entered (CreateStateFrame)</description></item>
///   <item><description>Persisted: Through child state transitions (child gets own frame, parent unchanged)</description></item>
///   <item><description>Destroyed: When state is exited (PopState disposes container, wrappers become unreachable)</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed record ActiveStateFrame(
    Identification StateId,
    ICoreContainer Container,
    IReadOnlyList<string> AddedMods,
    IReadOnlyList<IStatelessFunction> TransitionEnterMethods,
    IReadOnlyList<IStatelessFunction> TransitionExitMethods,
    IReadOnlyList<IStatelessFunction> PerFrameMethods);
