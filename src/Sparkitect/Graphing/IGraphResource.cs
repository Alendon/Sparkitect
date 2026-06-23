using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>
/// Pass-facing handle to a resource a pass uses. Resolves to the live instance for the current
/// frame via <see cref="Fetch"/>. The handle is opaque: it carries no slot, no backing, and no
/// declaration-internal wiring — those live in the ledger as facts, never on the handle.
/// </summary>
/// <typeparam name="T">The resource type, carried by C# generic shape; there is no per-type registry.</typeparam>
[PublicAPI]
public interface IGraphResource<out T>
{
    /// <summary>
    /// Resolves the live instance for the current frame. The instance is the one the declaration's
    /// facts construct; the handle never observes runtime-instance multiplication.
    /// </summary>
    T Fetch();
}
