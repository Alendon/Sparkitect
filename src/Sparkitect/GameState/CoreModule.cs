using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Core engine module included in all states. Provides fundamental engine functionality.
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("core")]
public sealed partial class CoreModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc />
    public override IReadOnlyList<Identification> Requires => [];
}