using JetBrains.Annotations;

namespace Sparkitect.Events;

/// <summary>
/// Default empty implementation of <see cref="IEventDefinition{TPayload}"/>. Sealed, parameterless,
/// no members — the out-of-the-box definition events use. Extension = add members later non-breaking.
/// </summary>
/// <typeparam name="TPayload">The payload type carried by this event.</typeparam>
[PublicAPI]
public sealed class EventDefinition<TPayload> : IEventDefinition<TPayload>;
