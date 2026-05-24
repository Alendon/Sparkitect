using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Pass-facing handle to a resource declared during <c>Setup</c>; resolves to the live view at
/// <c>Execute</c> / <c>PreExecute</c> time.
/// </summary>
[PublicAPI]
public interface IGraphResource<out TView>
{
    /// <summary>Pass-local slot index assigned by the declaration site.</summary>
    int Slot { get; }

    /// <summary>
    /// Resolves the live view for the current frame. Intended to be called from the
    /// <c>Execute</c> or <c>PreExecute</c> lifecycle phase only — handles handed out during
    /// <c>Setup</c> are logical and may resolve to nothing useful (or throw) until the owning
    /// manager has bound them to a backing. Calling <c>Fetch</c> during <c>Setup</c> is
    /// undefined behaviour at the contract level; future revisions may enforce this with a
    /// runtime gate.
    /// </summary>
    TView Fetch();
}
