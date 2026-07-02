using JetBrains.Annotations;

namespace Sparkitect.Utils;

/// <summary>
/// Interface for tracking object lifetimes.
/// Maintains strong references to tracked objects to detect leaks and monitor allocations.
/// </summary>
/// <typeparam name="T">The type of objects to track.</typeparam>
[PublicAPI]
public interface IObjectTracker<T>
{
    /// <summary>
    /// Registers an object for tracking.
    /// </summary>
    /// <param name="obj">The object to track.</param>
    Handle Track(T obj);

    /// <summary>
    /// Registers an object for tracking with callsite information.
    /// </summary>
    /// <param name="obj">The object to track.</param>
    /// <param name="callsite">The caller context capturing where the object was created.</param>
    Handle Track(T obj, CallerContext callsite);

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
    /// Gets all tracked objects with their callsite information.
    /// </summary>
    /// <returns>Enumerable of tuples containing the tracked object and its callsite.</returns>
    IEnumerable<(T Object, CallerContext Callsite)> GetTrackingEntries();

    /// <summary>
    /// Dumps the current tracking state to the log.
    /// </summary>
    /// <param name="context">Optional context string to include in log messages.</param>
    void DumpToLog(string context = "");

    /// <summary>
    /// Gets the count of currently tracked objects.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Disposable-style token returned by <see cref="Track(T)"/>. Calling <see cref="Free"/> untracks
    /// the object without needing a reference to the tracker at the release site.
    /// </summary>
    public readonly struct Handle
    {
        private readonly IObjectTracker<T> _tracker;
        private readonly T value;

        /// <summary>Creates a handle bound to a tracker and the tracked object.</summary>
        /// <param name="tracker">The tracker that issued this handle.</param>
        /// <param name="value">The tracked object.</param>
        public Handle(IObjectTracker<T> tracker, T value)
        {
            _tracker = tracker;
            this.value = value;
        }

        /// <summary>Untracks the object this handle refers to.</summary>
        public void Free()
        {
            _tracker.Untrack(value);
        }
    }
}
