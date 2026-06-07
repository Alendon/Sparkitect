using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Descriptor-bindable storage-image view over a graph <see cref="Image"/>. Owns one
/// <see cref="VkImageView"/> per image backing (frame-aliased): the binding it describes
/// always targets the view for the image's current backing. Implements
/// <see cref="IDescriptorBindingSource"/> so the descriptor — not the view — builds the
/// <see cref="WriteDescriptorSet"/>: the static <see cref="DescriptorType"/> is read at Setup
/// for layout derivation, and <see cref="DescribeBinding"/> hands the descriptor a
/// <see cref="DescriptorBindingPayload.StorageImage"/> at Execute.
/// </summary>
/// <remarks>
/// Unlike the layout-only read/write sibling views, this view owns its own image views.
/// It supplies only the descriptor payload at <see cref="ImageLayout.General"/>; the
/// matching write view owns the compute-write layout transition.
/// </remarks>
[ResourceManager<ImageResourceManager>]
[GraphResourceRegistry.RegisterResource("storage_image_view")]
[PublicAPI]
public sealed partial class StorageImageView : IHasIdentification, IDescriptorBindingSource, IDisposable
{
    private readonly Image _image;
    private readonly VkImageView[] _viewsPerBacking;

    internal StorageImageView(Image image, VkImageView[] viewsPerBacking)
    {
        _image = image;
        _viewsPerBacking = viewsPerBacking;
    }

    public Image UnderlyingImage => _image;

    public DescriptorType DescriptorType => DescriptorType.StorageImage;

    public DescriptorBindingPayload DescribeBinding() =>
        new DescriptorBindingPayload.StorageImage(
            _viewsPerBacking[_image.CurrentBackingIndex],
            ImageLayout.General);

    public void Dispose()
    {
        foreach (var view in _viewsPerBacking)
            view.Dispose();
    }
}
