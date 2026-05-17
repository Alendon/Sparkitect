using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;

namespace MinimalSampleMod;

[StateService<IMinimalSampleHost, SampleModule>]
internal sealed class MinimalSampleHost : IMinimalSampleHost
{
    private ISparkitWindow? _window;
    private RenderGraph? _renderGraph;

    public required IVulkanContext VulkanContext { private get; init; }
    public required IWindowManager WindowManager { private get; init; }
    public required IDIService DIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }
    public required IModManager ModManager { private get; init; }

    public bool IsOpen => _window?.IsOpen ?? false;

    public void Initialize()
    {
        _window = WindowManager.MainWindow ?? WindowManager.CreateWindow("MinimalSampleMod", 800, 600);
        WindowManager.MainWindow = _window;

        _renderGraph = RenderGraph.Initialize(
            VulkanContext,
            _window,
            DIService,
            GameStateManager.CurrentCoreContainer,
            ModManager,
            new List<Identification> { RenderPassID.MinimalSampleMod.ClearColor });
    }

    public void PollEvents() => _window?.PollEvents();

    public void RunFrame() => _renderGraph?.RunFrame();

    public void Shutdown()
    {
        _renderGraph?.Dispose();
        _renderGraph = null;
        _window?.Dispose();
        _window = null;
    }
}
