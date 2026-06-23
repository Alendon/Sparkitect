using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Hooks;

/// <summary>
/// Lifecycle hook invoked once per pass during graph initialization.
/// </summary>
[PublicAPI]
public interface ISetupHook
{
    void Setup(ISetupContext ctx);
}
