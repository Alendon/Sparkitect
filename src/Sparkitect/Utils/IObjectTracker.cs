namespace Sparkitect.Utils;

/// <summary>
/// Interface for tracking object lifetimes.
/// Maintains strong references to tracked objects to detect leaks and monitor allocations.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
public interface IObjectTracker<T>
{
    /// <summary>
    /// Registers an object for tracking.
    /// </summary>
    /// <param name="obj">The object to track.</param>
    Handle Track(T obj);

    /// <summary>
    /// Unregisters an object from tracking.
    /// </summary>
    /// <param name="obj">The object to untrack.</param>
    void Untrack(T obj);

    /// <summary>
    /// Gets all currently tracked objects.
    /// </summary>
    /// <returns>Read-only collection of tracked objects.</returns>
    ICollection<T> GetTracked();

    /// <summary>
    /// Gets the count of currently tracked objects.
    /// </summary>
    int Count { get; }

    public readonly struct Handle
    {
        private readonly IObjectTracker<T> _tracker;
        private readonly T value;

        public Handle(IObjectTracker<T> tracker, T value)
        {
            _tracker = tracker;
            this.value = value;
        }

        public void Free()
        {
            _tracker.Untrack(value);
        }
    }
}
