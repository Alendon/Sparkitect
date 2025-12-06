using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Configuration for swapchain creation.
/// Null values indicate the swapchain should auto-select optimal settings.
/// </summary>
[PublicAPI]
public readonly record struct SwapchainConfig
{
    /// <summary>
    /// Preferred surface format. If null or unavailable, selects B8G8R8A8_SRGB or first available.
    /// </summary>
    public Format? PreferredFormat { get; init; }

    /// <summary>
    /// Preferred color space. If null, selects SrgbNonlinear.
    /// </summary>
    public ColorSpaceKHR? PreferredColorSpace { get; init; }

    /// <summary>
    /// Preferred present mode. If null or unavailable, falls back to Fifo (always available).
    /// </summary>
    public PresentModeKHR? PreferredPresentMode { get; init; }

    /// <summary>
    /// Minimum number of swapchain images. Actual count may be higher based on surface capabilities.
    /// Default is 2 (double buffering). Use 3 for triple buffering.
    /// </summary>
    public uint MinImageCount { get; init; }

    /// <summary>
    /// Default configuration: double-buffered, SRGB format, Mailbox present mode (or Fifo fallback).
    /// </summary>
    public static SwapchainConfig Default => new()
    {
        PreferredFormat = Format.B8G8R8A8Srgb,
        PreferredColorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr,
        PreferredPresentMode = PresentModeKHR.MailboxKhr,
        MinImageCount = 2
    };
}
