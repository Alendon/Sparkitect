using JetBrains.Annotations;
using Sparkitect.GameState.Samples.Modules;

namespace Sparkitect.GameState.Samples;

[PublicAPI]
public static class SampleStates
{
    public static StateDescriptor Desktop()
        => new()
        {
            Id = "desktop",
            ParentId = null,
            Modules = new[]
            {
                new StateModuleDescriptor { Id = "core", ModuleType = typeof(CoreModule) },
                new StateModuleDescriptor { Id = "rendering", ModuleType = typeof(RenderingModule) }
            }
        };

    public static StateDescriptor MainMenu()
        => new()
        {
            Id = "main_menu",
            ParentId = "desktop",
            Modules = new[]
            {
                new StateModuleDescriptor { Id = "core", ModuleType = typeof(CoreModule) },
                new StateModuleDescriptor { Id = "rendering", ModuleType = typeof(RenderingModule) },
                new StateModuleDescriptor { Id = "mainmenu", ModuleType = typeof(MainMenuModule) }
            }
        };

    public static StateDescriptor LocalGame()
        => new()
        {
            Id = "local_game",
            ParentId = "desktop",
            Modules = new[]
            {
                new StateModuleDescriptor { Id = "core", ModuleType = typeof(CoreModule) },
                new StateModuleDescriptor { Id = "rendering", ModuleType = typeof(RenderingModule) },
                new StateModuleDescriptor { Id = "game", ModuleType = typeof(GameModule) }
            }
        };

    public static StateDescriptor ClientGame()
        => new()
        {
            Id = "client_game",
            ParentId = "desktop",
            Modules = new[]
            {
                new StateModuleDescriptor { Id = "core", ModuleType = typeof(CoreModule) },
                new StateModuleDescriptor { Id = "rendering", ModuleType = typeof(RenderingModule) },
                new StateModuleDescriptor { Id = "game", ModuleType = typeof(GameModule) },
                new StateModuleDescriptor { Id = "networking", ModuleType = typeof(NetworkingModule) }
            }
        };

    public static StateDescriptor ServerGame()
        => new()
        {
            Id = "server_game",
            ParentId = null, // direct child of root/bootstrap
            Modules = new[]
            {
                new StateModuleDescriptor { Id = "core", ModuleType = typeof(CoreModule) },
                new StateModuleDescriptor { Id = "game", ModuleType = typeof(GameModule) },
                new StateModuleDescriptor { Id = "networking", ModuleType = typeof(NetworkingModule) }
            }
        };
}

