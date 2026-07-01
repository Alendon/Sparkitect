using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>Pass-facing handle to a resource a pass uses; resolves to the live instance for the current frame via <see cref="Fetch"/>. Opaque: it carries no slot, backing, or declaration-internal wiring.</summary>
/// <typeparam name="T">The resource type, carried by C# generic shape; there is no per-type registry.</typeparam>
[PublicAPI]
public interface IGraphResource<out T>
{
    /// <summary>Resolves the live instance for the current frame.</summary>
    T Fetch();
}
