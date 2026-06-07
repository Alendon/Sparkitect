using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Describes a physical graph image: its size, format, transient lifetime, and an
/// optional default fill. Carries no usage — usage is a property of the view consumed
/// over the physical resource, not of the physical resource itself. Shared by shared-image
/// registration and inline pass-local transients.
/// </summary>
/// <param name="Size">Image extent.</param>
/// <param name="Format">Image format.</param>
/// <param name="Transient">
/// When true, marks the backing as transient — carried forward for memory aliasing/pooling
/// and resize handling.
/// </param>
/// <param name="DefaultFill">Optional clear value applied by the manager on first use.</param>
[PublicAPI]
public readonly record struct ImageDescription(
    Extent2D Size,
    Format Format,
    bool Transient,
    ClearColorValue? DefaultFill);
