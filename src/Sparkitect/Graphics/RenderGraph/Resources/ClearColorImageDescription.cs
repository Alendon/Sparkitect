using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public sealed record ClearColorImageDescription : IResourceDescription<ImageResource>
{
    
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx)
    {
        tx.Increment(tx.Self<ImageResource>(), GraphMomentID.Sparkitect.Finishline);
        return tx.InstantiateFact<ClearColorImageFact>();
    }
}
