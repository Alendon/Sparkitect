using System.Collections.Concurrent;

namespace Sparkitect.Utils;

/// <summary>
/// Thread-safe implementation of <see cref="IObjectTracker{T}"/> that maintains strong references to tracked objects.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
public sealed class ObjectTracker<T> : IObjectTracker<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _tracked = new();

    public IObjectTracker<T>.Handle Track(T obj)
    {
        _tracked.TryAdd(obj, 0);
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

    public int Count => _tracked.Count;
}
