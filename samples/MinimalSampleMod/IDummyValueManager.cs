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
}

[FacadeFor<IDummyValueManager>]
public interface IDummyValueManagerStateFacade
{
    string GetDummyFacaded(Identification id);
    
    IWorld BuildWorld();
    void DestroyWorld();
    void SimulateWorld();
}