using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// One entry in <see cref="IGameStateManager.StateStack"/>: the state that owns this frame,
/// the module IDs introduced by that state, and the mod IDs added at this entry point.
/// </summary>
[PublicAPI]
public sealed record StateStackEntry(
    Identification StateId,
    IReadOnlyList<Identification> AddedModuleIds,
    IReadOnlyList<string> AddedMods);
