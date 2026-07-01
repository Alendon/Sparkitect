using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>How a declared fact's resolved instance is released at graph teardown. How a release is honoured is the backing provider's decision, never the graph's.</summary>
[PublicAPI]
public enum CleanupStrategy
{
    /// <summary>Nothing to release.</summary>
    None,

    /// <summary>The instance directly disposes an owned object.</summary>
    Dispose,

    /// <summary>Manager-backed: the instance signals release and its backing provider decides how to honour it.</summary>
    Release,
}
