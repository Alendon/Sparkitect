using System.Collections.Concurrent;
using Serilog;

namespace Sparkitect.Utils;

/// <summary>
/// Thread-safe implementation of <see cref="IObjectTracker{T}"/> that maintains strong references to tracked objects.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
public sealed class ObjectTracker<T> : IObjectTracker<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, CallerContext> _tracked = new();

    public IObjectTracker<T>.Handle Track(T obj)
    {
        return Track(obj, default);
    }

    public IObjectTracker<T>.Handle Track(T obj, CallerContext callsite)
    {
        _tracked.TryAdd(obj, callsite);
        return new IObjectTracker<T>.Handle(this, obj);
    }

    public void Untrack(T obj)
    {
        _tracked.TryRemove(obj, out _);
    }

    public ICollection<T> GetTracked()
    {
        return _tracked.Keys;
    }

    public IEnumerable<(T Object, CallerContext Callsite)> GetTrackingEntries()
    {
        foreach (var kvp in _tracked)
            yield return (kvp.Key, kvp.Value);
    }

    public void DumpToLog(string context = "")
    {
        var prefix = string.IsNullOrEmpty(context) ? "ObjectTracker" : $"ObjectTracker[{context}]";
        Log.Debug("{Prefix}: {Count} objects tracked", prefix, Count);
        foreach (var (obj, callsite) in GetTrackingEntries())
        {
            Log.Debug("{Prefix}:   {Type} from {Callsite}",
                prefix, obj.GetType().Name, callsite);
        }
    }

    public int Count => _tracked.Count;
}
