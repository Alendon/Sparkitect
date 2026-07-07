using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Deferred resolution of an <see cref="Identification"/> for a forward reference captured during
/// registration, before its target has been registered.
/// </summary>
/// <remarks>
/// <see cref="Resolve"/> re-resolves on every call and never caches — the target's identification is
/// read at the moment it is needed, not when the reference was captured. Resolution is fail-loud:
/// <see cref="Resolve"/> propagates the underlying accessor's exception (e.g. when the target is
/// unregistered or torn down) rather than returning a sentinel.
/// </remarks>
[PublicAPI]
public interface ILazyIdentification
{
    /// <summary>Resolves the target identification now. Re-resolves on every call; throws if the target is unavailable.</summary>
    Identification Resolve();
}
