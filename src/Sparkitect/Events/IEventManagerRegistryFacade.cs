using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.Events;

/// <summary>
/// Register-time facade for event bus mutation. Accessible only within registry contexts.
/// </summary>
[PublicAPI]
[FacadeFor<IEventManager>]
public interface IEventManagerRegistryFacade
{
    /// <summary>Registers an event with its definition. Called by the event registry.</summary>
    /// <typeparam name="TPayload">The event payload type.</typeparam>
    /// <param name="id">The event identification.</param>
    /// <param name="definition">The event definition.</param>
    void RegisterEvent<TPayload>(Identification id, IEventDefinition<TPayload> definition);

    /// <summary>Unregisters an event and clears its subscribers.</summary>
    /// <param name="id">The event identification.</param>
    void Unregister(Identification id);
}
