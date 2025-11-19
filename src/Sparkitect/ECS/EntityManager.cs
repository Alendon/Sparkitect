using Sparkitect.GameState;
using Sparkitect.GameState.Samples.Modules;
using Sparkitect.Modding;

namespace Sparkitect.ECS;

[StateService<IEntityManager, EcsModule>]
public class EntityManager : IEntityManager
{
    public void AddArchetype(Identification archetypeId, bool isConstant, Span<Identification> componentIds)
    {
        throw new NotImplementedException();
    }

    public void AddArchetypeComponents(Identification archetypeId, Span<Identification> componentIds)
    {
        throw new NotImplementedException();
    }

    public bool HasArchetypeComponent(Identification archetypeId, Identification componentId)
    {
        throw new NotImplementedException();
    }

    public Identification[] GetArchetypeComponents(Identification archetypeId)
    {
        throw new NotImplementedException();
    }

    public bool IsArchetypeConstant(Identification archetypeId)
    {
        throw new NotImplementedException();
    }

    public void RemoveArchetype(Identification archetypeId)
    {
        throw new NotImplementedException();
    }

    public void RemoveArchetypeComponents(Identification archetypeId, Span<Identification> componentIds)
    {
        throw new NotImplementedException();
    }
}