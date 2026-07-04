using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// One entry in <see cref="IGameStateManager.StateStack"/>: the state that owns this frame, its
/// complete composed module set (the authoritative snapshot for this state), the module IDs this
/// state introduces over its parent chain (the delta), and the mod IDs added at this entry point.
/// </summary>
[PublicAPI]
public sealed record StateStackEntry(
    Identification StateId,
    IReadOnlyList<Identification> ComposedModuleIds,
    IReadOnlyList<Identification> AddedModuleIds,
    IReadOnlyList<string> AddedMods);
