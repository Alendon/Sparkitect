using JetBrains.Annotations;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

/// <summary>
/// Core engine module included in all states. Provides fundamental engine functionality.
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("core")]
public sealed partial class CoreModule : IStateModule
{
    /// <inheritdoc />
    public static IReadOnlyList<Identification> RequiredModules => [];

    /// <inheritdoc />
    public static Identification Identification => StateModuleID.Sparkitect.Core;
}