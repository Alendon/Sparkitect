using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;

namespace Sparkitect.Graphing.Descriptions;

public interface DeclaredFact
{


    
    public CleanupStrategy CleanupStrategy { get; }
}

[PublicAPI]
public interface DeclaredFact<T> : DeclaredFact
{

    /// </summary>
    public T CreateInstance(IInstanceContext ctx);
    
}
