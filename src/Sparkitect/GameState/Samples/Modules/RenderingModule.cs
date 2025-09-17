using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.Register("rendering")]
[OrderAfterModule(typeof(CoreModule))]
public sealed partial class RenderingModule : IStateModule
{
    public const string Key_RenderTick = "render_tick";

    [Feature(Key_RenderTick)]
    public static void RenderTick(FeatureContext ctx)
    {
        // Rendering tick placeholder
        _ = ctx;
    }
}

