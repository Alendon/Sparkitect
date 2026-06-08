namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Per-graph device storage-buffer resource manager. Owns the device-local buffer backings and
/// resolves <c>FromRegistered</c> declarations against them. A single manager serves the whole
/// device buffer family.
/// </summary>
public interface IBufferResourceManager : IGraphResourceManager<StorageBufferView, BufferRequest>
{
    /// <summary>
    /// Drain the module resource-registration store, creating one shared device backing per
    /// registered <c>(Identification, BufferDescription)</c>. Invoke once during render-graph
    /// setup; <c>FromRegistered</c> declarations resolve against the backings created here.
    /// </summary>
    void DrainRegisteredBuffers();
}
