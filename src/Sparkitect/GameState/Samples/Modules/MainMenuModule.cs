using JetBrains.Annotations;
using Sparkitect.GameState;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.RegisterModule("main_menu")]
[OrderAfterModule(typeof(RenderingModule))]
public sealed partial class MainMenuModule : IStateModule
{
    public const string Key_MenuTick = "menu_tick";

    [Feature(Key_MenuTick)]
    public static void MenuTick(FeatureContext ctx)
    {
        // Main menu UI/logic placeholder
        _ = ctx;
    }

    public static IReadOnlyList<Type> ExposedServices => [];
}

