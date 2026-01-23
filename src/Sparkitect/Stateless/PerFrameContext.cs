namespace Sparkitect.Stateless;

/// <summary>
/// Context for per-frame stateless functions. Minimal - PerFrame functions
/// have straightforward scheduling with no special contextual requirements.
/// </summary>
public sealed class PerFrameContext
{
    /// <summary>
    /// Singleton instance - per-frame context has no state.
    /// </summary>
    public static readonly PerFrameContext Instance = new();

    private PerFrameContext() { }
}
