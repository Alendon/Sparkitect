using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Base interface for scheduling metadata types.
/// Scheduling instances are collected via metadata entrypoints and
/// consumed by managers that build execution graphs.
/// </summary>
[PublicAPI]
public interface IScheduling
{
    /// <summary>
    /// Deferred reference to the owning module/state identification. Set by generated entrypoints
    /// during metadata collection — kept lazy because collection runs across every scheduling
    /// category sharing this base, including entries the collecting consumer never reads, before
    /// the owner's own registry pass has necessarily run. Resolved only at genuine point of use.
    /// </summary>
    ILazyIdentification OwnerId { get; set; }
}
