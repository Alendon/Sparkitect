namespace Sparkitect.Utils;

/// <summary>
/// No-op implementation of <see cref="IObjectTracker{T}"/> that performs no tracking.
/// Used when object tracking is disabled.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
public sealed class NullObjectTracker<T> : IObjectTracker<T>
{
    public static NullObjectTracker<T> Instance { get; } = new();

    private NullObjectTracker()
    {
    }

    public IObjectTracker<T>.Handle Track(T obj)
    {
        return new IObjectTracker<T>.Handle(this, obj);
    }

    public void Untrack(T obj)
    {
    }

    public ICollection<T> GetTracked() => Array.Empty<T>();

    public int Count => 0;
}