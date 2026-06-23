using JetBrains.Annotations;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Declaration shapes for a <see cref="StorageBufferView"/>: a registered shared device buffer
/// resolved by <see cref="Identification"/>. The buffer is written by the staging pass and read by
/// the compute pass, so it crosses a pass boundary and is referenced symbolically.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record BufferRequest : IResourceRequest<StorageBufferView>
{
    public sealed partial record FromRegistered(Identification Id) : BufferRequest;
}
