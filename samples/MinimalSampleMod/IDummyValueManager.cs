using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[StateFacade<IDummyValueManagerStateFacade>]
public interface IDummyValueManager
{
    void AddDummyValue(Identification id, string value);
    string GetDummyValue(Identification id);
    
    IWorld? GetWorld();
    void AddDummyProvider<TProvider>(Identification id) where TProvider : class, IDummyValueProvider, IHasIdentification;
}

[FacadeFor<IDummyValueManager>]
public interface IDummyValueManagerStateFacade
{
    string GetDummyFacaded(Identification id);
    
    IWorld BuildWorld();
    void DestroyWorld();
    void SimulateWorld();
}