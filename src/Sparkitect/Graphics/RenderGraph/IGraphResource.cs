using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>Pass-facing handle to a resource declared during Setup; resolves to the live view at Execute time.</summary>
[PublicAPI]
public interface IGraphResource<out TView>
{
    int Slot { get; }
    TView Fetch();
}
