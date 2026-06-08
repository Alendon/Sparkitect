using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// General resource-registration store. The canonical module-level home for resource
/// registrations keyed by <see cref="Identification"/>, with an independent dimension per
/// resource family (images and buffers ship now; factory-form families land here later).
/// Registries write into it from the module container; a graph-local manager drains it at
/// Setup via the parent-chain.
/// </summary>
[PublicAPI]
public interface IResourceRegistrationStore
{
    /// <summary>
    /// Records a shared-image registration. Keyed by <paramref name="id"/>; a later
    /// registration of the same id overwrites the prior one (last-writer wins).
    /// </summary>
    void RegisterImage(Identification id, ImageDescription description);

    /// <summary>
    /// Resolves a previously registered image description by its identification.
    /// Returns false when the id is unregistered.
    /// </summary>
    bool TryGetImage(Identification id, out ImageDescription description);

    /// <summary>
    /// Removes a previously registered image. No-op when the id is unregistered.
    /// </summary>
    void UnregisterImage(Identification id);

    /// <summary>
    /// All currently registered image descriptions, keyed by identification. The
    /// graph-local manager drains this at Setup to create shared backings.
    /// </summary>
    IReadOnlyDictionary<Identification, ImageDescription> RegisteredImages { get; }

    /// <summary>
    /// Records a shared-buffer registration. Keyed by <paramref name="id"/>; a later
    /// registration of the same id overwrites the prior one (last-writer wins).
    /// </summary>
    void RegisterBuffer(Identification id, BufferDescription description);

    /// <summary>
    /// Resolves a previously registered buffer description by its identification.
    /// Returns false when the id is unregistered.
    /// </summary>
    bool TryGetBuffer(Identification id, out BufferDescription description);

    /// <summary>
    /// Removes a previously registered buffer. No-op when the id is unregistered.
    /// </summary>
    void UnregisterBuffer(Identification id);

    /// <summary>
    /// All currently registered buffer descriptions, keyed by identification. The
    /// graph-local manager drains this at Setup to create shared backings.
    /// </summary>
    IReadOnlyDictionary<Identification, BufferDescription> RegisteredBuffers { get; }
}
