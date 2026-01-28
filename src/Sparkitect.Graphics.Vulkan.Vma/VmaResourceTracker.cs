using System.Collections.Concurrent;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Thread-safe implementation of <see cref="IVmaResourceTracker"/> that tracks VMA resource allocations.
/// </summary>
public sealed class VmaResourceTracker : IVmaResourceTracker
{
    private readonly ConcurrentDictionary<object, string> _tracked = new();

    public void Track(
        object resource,
        string callerFile = "",
        string callerMember = "",
        int callerLine = 0)
    {
        var callsite = $"{System.IO.Path.GetFileName(callerFile)}:{callerLine} ({callerMember})";
        _tracked.TryAdd(resource, callsite);
    }

    public void Untrack(object resource)
    {
        _tracked.TryRemove(resource, out _);
    }

    public IEnumerable<(object Resource, string Callsite)> GetTrackingEntries()
    {
        foreach (var kvp in _tracked)
            yield return (kvp.Key, kvp.Value);
    }

    public int Count => _tracked.Count;
}
