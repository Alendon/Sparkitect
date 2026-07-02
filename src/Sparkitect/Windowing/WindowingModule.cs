using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Windowing;

/// <summary>State module that provides windowing and input. Depends on the core module.</summary>
[ModuleRegistry.RegisterModule("windowing")]
[PublicAPI]
public partial class WindowingModule : IStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];
}
