namespace Sparkitect.RenderGraph;

/// <summary>
/// Foundation contract for an opaque render-graph resource handle.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IGraphResource{T}"/> is the generic handle protocol that pass
/// Setup captures and pass Execute consumes. The foundation treats <typeparamref name="T"/>
/// as opaque — it places no constraint on the bound value type. Stock layers
/// (Phases 53+) close this interface over concrete resource view types (for
/// example, <c>IGraphResource&lt;StorageBufferView&gt;</c>); manual-contract
/// authors close it over mod-defined view types.
/// </para>
/// <para>
/// The foundation does NOT ship a concrete implementation. Concrete handles
/// are constructed by source-generator-emitted code or by hand-written manual
/// pass categories. The exact resolution mechanism behind <see cref="Fetch"/> is
/// defined by the closed <typeparamref name="T"/> and the emergent layer that
/// owns its resource manager (Phase 54+).
/// </para>
/// <para>
/// <see cref="Fetch"/> is unconditional at the foundation layer. There is no
/// "am I bound" check and no "am I in a fetch-legal lifecycle phase" check.
/// Protocol-level misuse detection (unbound fetch, fetch outside an execute-time
/// code path) is an emergent-layer concern delivered by graph compilation
/// (Phase 63), frame execution (Phase 65), and the companion analyzers (Phase 61).
/// </para>
/// <para>
/// The render-graph foundation is Vulkan-agnostic. This interface defines no
/// Vulkan, descriptor, or synchronization semantics.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// The bound value type exposed by <see cref="Fetch"/>. Opaque at the foundation
/// level; stock and mod layers close it over concrete resource view types.
/// </typeparam>
public interface IGraphResource<T>
{
    /// <summary>
    /// Returns the value currently bound to this handle.
    /// </summary>
    /// <returns>
    /// The bound value of type <typeparamref name="T"/>. Resolution is delegated
    /// to whatever mechanism the closed <typeparamref name="T"/> and its
    /// associated resource manager provide; the foundation performs no
    /// validation.
    /// </returns>
    T Fetch();

    /// <summary>
    /// The opaque identity of this handle — a stable <c>(pass id, slot index)</c>
    /// pair used by emergent managers as a tracking key across frames and
    /// lifecycle phases.
    /// </summary>
    GraphHandleId Id { get; }
}
