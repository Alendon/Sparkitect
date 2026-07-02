using JetBrains.Annotations;

namespace Sparkitect.Utils;

/// <summary>
/// No-op implementation of <see cref="IObjectTracker{T}"/> that performs no tracking.
/// Used when object tracking is disabled.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
[PublicAPI]
public sealed class NullObjectTracker<T> : IObjectTracker<T>
{
    /// <summary>Shared no-op tracker instance.</summary>
    public static NullObjectTracker<T> Instance { get; } = new();

    private NullObjectTracker()
    {
    }

    /// <inheritdoc/>
    public IObjectTracker<T>.Handle Track(T obj)
    {
        return new IObjectTracker<T>.Handle(this, obj);
    }

    /// <inheritdoc/>
    public IObjectTracker<T>.Handle Track(T obj, CallerContext callsite)
    {
        return new IObjectTracker<T>.Handle(this, obj);
    }

    /// <inheritdoc/>
    public void Untrack(T obj)
    {
    }

    /// <inheritdoc/>
    public ICollection<T> GetTracked() => Array.Empty<T>();

    /// <inheritdoc/>
    public IEnumerable<(T Object, CallerContext Callsite)> GetTrackingEntries()
        => Enumerable.Empty<(T, CallerContext)>();

    /// <inheritdoc/>
    public void DumpToLog(string context = "")
    {
    }

    /// <inheritdoc/>
    public int Count => 0;
}