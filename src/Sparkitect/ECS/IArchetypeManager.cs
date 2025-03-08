using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS;

[PublicAPI]
public record Archetype(HashSet<Identification> Components, Action<IEntityCollection, Entity>? EntitySetup = null);

[PublicAPI]
public interface IArchetypeManager
{
    public void AddArchetype(Identification id, Archetype archetype);
    public void RemoveArchetype(Identification id);
    
    public bool TryGetArchetype(Identification id, [MaybeNullWhen(false)] out Archetype archetype);
}