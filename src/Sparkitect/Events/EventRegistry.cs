using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Events;

/// <summary>
/// Value registry for event declarations. A mod's provider returns an <see cref="IEventDefinition{TPayload}"/>;
/// the closed generic payload type survives registration and reaches the facade with its type intact.
/// </summary>
[Registry(Identifier = "event")]
[PublicAPI]
public partial class EventRegistry(IEventManagerRegistryFacade facade) : IRegistry<EventModule>
{
    /// <inheritdoc/>
    public static string Identifier => "event";

    /// <summary>Registers an event: binds <paramref name="id"/> to <paramref name="definition"/>.</summary>
    /// <typeparam name="TPayload">The event payload type.</typeparam>
    /// <param name="id">The event identification.</param>
    /// <param name="definition">The event definition.</param>
    [RegistryMethod]
    public void RegisterEvent<[TypedIdentification] TPayload>(Identification id, IEventDefinition<TPayload> definition) => facade.RegisterEvent<TPayload>(id, definition);

    /// <inheritdoc/>
    public void Unregister(Identification id) => facade.Unregister(id);
}
