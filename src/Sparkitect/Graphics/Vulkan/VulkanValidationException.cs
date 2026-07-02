namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Thrown when the Vulkan validation layer reports an ERROR-severity message. A validation error is a
/// defect, not a recoverable condition, so it surfaces as a distinct exception that must reach the
/// top-level handler unswallowed. The message carries the validation text (including VUID).
/// </summary>
public sealed class VulkanValidationException : Exception
{
    /// <summary>Creates the exception carrying the validation-layer <paramref name="message"/>.</summary>
    public VulkanValidationException(string message) : base(message)
    {
    }
}
