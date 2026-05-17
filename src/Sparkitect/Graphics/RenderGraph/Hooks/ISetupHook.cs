using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Hooks;

/// <summary>
/// Lifecycle hook invoked once per pass during graph initialization.
/// </summary>
[PublicAPI]
public interface ISetupHook
{
    void Setup();
}
