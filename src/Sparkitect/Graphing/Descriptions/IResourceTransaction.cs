using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;


[PublicAPI]
public interface IResourceTransaction
{

    void Read<T>(ResourceRef<T> reference);


    ResourceRef<T> Increment<T>(ResourceRef<T> reference);


    ResourceRef<T> Increment<T>(ResourceRef<T> reference, Identification moment);


    void ReferenceMoment(Identification moment);


    ResourceRef<TSub> Declare<TSub>(IResourceDescription<TSub> description);

 
    ResourceRef<T> Self<T>();

    TDeclaredFact InstantiateFact<TDeclaredFact>() where TDeclaredFact : DeclaredFact, IHasIdentification;
}
