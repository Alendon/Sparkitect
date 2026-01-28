namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// No-op implementation of <see cref="IVmaResourceTracker"/> that performs no tracking.
/// Used when resource tracking is disabled (e.g., release builds).
/// </summary>
public sealed class NullVmaResourceTracker : IVmaResourceTracker
{
    /// <summary>
    /// Singleton instance of the null tracker.
    /// </summary>
    public static NullVmaResourceTracker Instance { get; } = new();

    private NullVmaResourceTracker()
    {
    }

    public void Track(
        object resource,
        string callerFile = "",
        string callerMember = "",
        int callerLine = 0)
    {
    }

    public void Untrack(object resource)
    {
    }

    public IEnumerable<(object Resource, string Callsite)> GetTrackingEntries()
        => Enumerable.Empty<(object, string)>();

    public int Count => 0;
}
