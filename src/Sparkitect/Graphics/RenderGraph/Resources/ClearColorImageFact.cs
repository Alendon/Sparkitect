using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[FactRegistry.Register("clear_color_image")]
public sealed partial class ClearColorImageFact(IImageManager? Provider)
    : DeclaredFact<ImageResource>, IHasIdentification
{
    /// <inheritdoc/>
    public ImageResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "ClearColorImageFacts.CreateInstance: no image backing provider was set. The graph's " +
                "setup context must assign ClearColorImageDescription.Provider before Declare runs.");

        return Provider.ResolveSwapchainLeaf();
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
