using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// How a declared fact's resolved instance is released when the graph tears down. A property of the
/// fact; how a release is honoured is decided by the backing provider, never by the graph.
/// </summary>
[PublicAPI]
public enum CleanupStrategy
{
    /// <summary>Nothing to release — e.g. a composite of sub-resources plus CPU-side metadata.</summary>
    None,

    /// <summary>The fact's instance directly disposes an owned object (e.g. a view disposing its handle).</summary>
    Dispose,

    /// <summary>Manager-backed: the instance signals release and its backing provider decides how to honour it.</summary>
    Release,
}
