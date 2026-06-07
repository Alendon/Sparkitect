using JetBrains.Annotations;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Declaration shapes for a <see cref="StorageImageView"/>: a registered shared physical image
/// resolved by <see cref="Identification"/>, or an inline transient. The storage view's layout is
/// always <see cref="Silk.NET.Vulkan.ImageLayout.General"/>, so no usage is carried.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record StorageImageViewRequest : IResourceRequest<StorageImageView>
{
    public sealed partial record FromRegistered(Identification Id) : StorageImageViewRequest;
    public sealed partial record FromTransient(ImageDescription Description) : StorageImageViewRequest;
}
