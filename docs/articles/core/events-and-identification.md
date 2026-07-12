---
uid: sparkitect.core.events-and-identification
title: Events and Typed Identification
description: Typed identification wrappers, lazy forward references, and the synchronous event bus
---

# Events and Typed Identification

Typed identifications carry a compile-time payload type alongside a registered object's identity, and the event bus uses them to key synchronous publish/subscribe channels. Lazy identifications defer resolution of a forward reference until it is actually needed.

## Typed Identification (`Identification<T>`)

[`Identification<T>`](xref:Sparkitect.Modding.Identification`1) is a phantom-typed wrapper over the bare 8-byte [`Identification`](xref:Sparkitect.Modding.Identification) — still 8 bytes total, since the payload type `T` is erased at runtime and carries no field. It implicitly converts one-way to the bare `Identification`, so it drops into every existing structural site (dictionary keys, `IIdentificationManager`, `IHasIdentification`) unchanged:

```csharp
public readonly struct Identification<T> : IEquatable<Identification<T>>
{
    internal readonly Identification Id;
    public Identification(Identification id) => Id = id;
    public static implicit operator Identification(Identification<T> typed) => typed.Id;
}
```

Registries that opt a type parameter into `[TypedIdentification]` generate typed ID accessors (e.g. `SettingID.Sparkitect.VulkanValidation` resolves to `Identification<bool>`) instead of the bare `Identification`, so a mismatched payload type is a compile error rather than a runtime one.

## Lazy Identification (`ILazyIdentification`)

[`ILazyIdentification`](xref:Sparkitect.Modding.ILazyIdentification) defers resolution of an `Identification` for a forward reference captured before its target has registered. [`LazyIdentification.Of<T>()`](xref:Sparkitect.Modding.LazyIdentification.Of``1) builds one:

```csharp
public static class LazyIdentification
{
    public static ILazyIdentification Of<T>() where T : IHasIdentification => new OfImpl<T>();
}
```

`Resolve()` re-resolves on every call and never caches — the target's identification is read at the moment it is needed, not when the reference was captured. Resolution is fail-loud: `Resolve()` throws when the target is unavailable rather than returning a sentinel. The primary consumer is [`RequestWithModChange`](xref:sparkitect.core.game-state-system#transition-with-mod-loading), which accepts a state that may live in a mod not yet loaded.

## Event Bus (`IEventManager`)

[`IEventManager`](xref:Sparkitect.Events.IEventManager) is a synchronous publish/subscribe bus keyed by typed `Identification<TPayload>`. Events are declared through [`EventRegistry`](xref:Sparkitect.Events.EventRegistry) like any other registry category:

```csharp
[EventRegistry.RegisterEvent("window_created")]
public static IEventDefinition<ISparkitWindow> WindowCreated => new EventDefinition<ISparkitWindow>();
```

Subscribers get the payload synchronously on every publish:

```csharp
EventBinding subscription = eventManager.Subscribe(
    EventID.Sparkitect.WindowCreated,
    window => RegisterWindowKeyboard(window));
```

[`EventBinding`](xref:Sparkitect.Events.EventBinding) is a disposable subscription handle — disposing it unsubscribes the handler. Publish is allocation-free on the hot path: `eventManager.Publish(id, payload)` invokes every live subscriber synchronously with no async dispatch and no queuing.

## See Also

- [Game State System](xref:sparkitect.core.game-state-system) for `RequestWithModChange`'s use of `ILazyIdentification`
- [Registry System](xref:sparkitect.core.registry-system) for the generic registration pattern events and typed IDs both build on
- [Input](xref:sparkitect.input) for named actions, which register their delivery channel on this event bus
