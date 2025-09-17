using JetBrains.Annotations;

namespace Sparkitect.GameState.Samples;

[PublicAPI]
[StateModule("mainmenu")]
[OrderAfterModule(typeof(RenderingModule))]
public sealed partial class MainMenuModule
{
    public const string Key_MenuTick = "menu_tick";

    [Feature(Key_MenuTick)]
    public static void MenuTick(FeatureContext ctx)
    {
        // Main menu UI/logic placeholder
        _ = ctx;
    }
}

