using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.RegisterModule("rendering")]
public sealed partial class RenderingModule : IStateModule
{
    public const string Key_RenderTick = "render_tick";
    public const string Key_RenderInit = "render_init";

    [StateFunction(Key_RenderTick)]
    [PerFrame]
    public static void RenderTick(FeatureContext ctx)
    {
        // Rendering tick placeholder
        _ = ctx;
    }

    [StateFunction(Key_RenderInit)]
    [OnModuleEnter]
    public static void RenderInit(FeatureContext ctx)
    {
        // Rendering init placeholder
        _ = ctx;
    }

    public static IReadOnlyList<Type> UsedServices => [];
    public static Identification Identification => StateModuleID.Sparkitect.Rendering;
}

