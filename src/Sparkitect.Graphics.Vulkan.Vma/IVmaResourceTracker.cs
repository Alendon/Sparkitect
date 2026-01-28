namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Interface for tracking VMA resource lifetimes.
/// Used for debugging memory leaks in VmaBuffer and VmaImage allocations.
/// </summary>
public interface IVmaResourceTracker
{
    /// <summary>
    /// Registers a resource for tracking.
    /// </summary>
    /// <param name="resource">The VMA resource to track.</param>
    /// <param name="callerFile">The source file where the resource was created.</param>
    /// <param name="callerMember">The method where the resource was created.</param>
    /// <param name="callerLine">The line number where the resource was created.</param>
    void Track(
        object resource,
        [System.Runtime.CompilerServices.CallerFilePath] string callerFile = "",
        [System.Runtime.CompilerServices.CallerMemberName] string callerMember = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Unregisters a resource from tracking.
    /// </summary>
    /// <param name="resource">The VMA resource to untrack.</param>
    void Untrack(object resource);

    /// <summary>
    /// Gets all tracked resources with their allocation callsite information.
    /// </summary>
    IEnumerable<(object Resource, string Callsite)> GetTrackingEntries();

    /// <summary>
    /// Gets the count of currently tracked resources.
    /// </summary>
    int Count { get; }
}
