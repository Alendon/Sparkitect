using JetBrains.Annotations;

namespace Sparkitect.Events;

/// <summary>
/// Marker interface for event definitions. Extension anchor for event-specific behaviour (delivery
/// config, throttling, subscriber limits) landed non-breakingly in future phases; currently empty.
/// </summary>
/// <typeparam name="TPayload">The payload type carried by this event.</typeparam>
[PublicAPI]
public interface IEventDefinition<TPayload>;
