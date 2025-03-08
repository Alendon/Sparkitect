using Sparkitect.Modding;

namespace Sparkitect.ECS;

public interface IEntityManager
{
    public void AddArchetype(Identification archetypeId, bool isConstant, Span<Identification> componentIds);
    public void AddArchetypeComponents(Identification archetypeId, Span<Identification> componentIds);
    
    public bool HasArchetypeComponent(Identification archetypeId, Identification componentId);
    public Identification[] GetArchetypeComponents(Identification archetypeId);
    public bool IsArchetypeConstant(Identification archetypeId);
    
    public void RemoveArchetype(Identification archetypeId);
    public void RemoveArchetypeComponents(Identification archetypeId, Span<Identification> componentIds);
    
    

}