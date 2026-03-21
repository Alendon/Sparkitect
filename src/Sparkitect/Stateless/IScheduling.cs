using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base interface for scheduling metadata types.
/// Scheduling instances are collected via metadata entrypoints and
/// consumed by managers that build execution graphs.
/// </summary>
public interface IScheduling
{
    /// <summary>
    /// The owning module/state identification. Set by generated entrypoints
    /// during metadata collection.
    /// </summary>
    Identification OwnerId { get; set; }
}
