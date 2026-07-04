using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;

namespace MinimalSampleMod;

[StateService<IMinimalSampleHost, SampleModule>]
internal sealed class MinimalSampleHost : IMinimalSampleHost
{
    private ISparkitWindow? _window;
    private RenderGraph? _renderGraph;

    public required IWindowManager WindowManager { private get; init; }
    public required IRenderGraphManager RenderGraphManager { private get; init; }

    public bool IsOpen => _window?.IsOpen ?? false;

    public void Initialize()
    {
        if (_renderGraph is not null) return;

        _window = WindowManager.CreateGameWindow("MinimalSampleMod");

        _renderGraph = RenderGraphManager.CreateGraph<RenderGraph>(
            new List<Identification> { RenderPassID.MinimalSampleMod.ClearColor },
            _window);
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
