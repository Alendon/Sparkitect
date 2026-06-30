using JetBrains.Annotations;

namespace Sparkitect.Graphing.Descriptions;


[PublicAPI]
public interface IResourceDescription<T>
{
   
    DeclaredFact<T> Declare(IResourceTransaction tx);
}
