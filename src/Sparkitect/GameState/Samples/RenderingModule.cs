using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples;

[PublicAPI]
[StateModule("rendering")]
[OrderAfterModule(typeof(CoreModule))]
public sealed partial class RenderingModule
{
    public const string Key_RenderTick = "render_tick";

    [Feature(Key_RenderTick)]
    public static void RenderTick(FeatureContext ctx)
    {
        // Rendering tick placeholder
        _ = ctx;
    }
}

