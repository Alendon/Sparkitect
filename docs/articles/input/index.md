---
uid: sparkitect.input
title: Input Module
description: Named, device-neutral input actions consumed through push or pull
---

# Input Module

Sparkitect input is a named, device-neutral action layer. Core `Sparkitect.Input` declares actions and their default bindings but owns no storage or sampling itself; `Sparkitect.WindowInput` is the first concrete implementation, keyboard-backed.

## Declaring an Action

```csharp
[ActionRegistry.RegisterAction("left_paddle")]
public static ActionDescription<float, InputAxis<Key>> LeftPaddle() =>
    new(new InputAxis<Key>(Key.W, Key.S));
```

## Device-Neutral Default-Binding Shapes

`Key` is a raw source value. [`InputAxis<TKey>`](xref:Sparkitect.Input.InputAxis`1) is a two-value -1..+1 axis. [`InputVector2<TKey>`](xref:Sparkitect.Input.InputVector2`1) is a four-value WASD-shaped composite. Core owns only the shape — an input implementation interprets it through its own registered binding adapter.

## Consuming: Push

```csharp
_leftPaddlePush = ActionID.PongMod.LeftPaddle.Push(InputActions, v => _leftIntent = v);
```

Push delivers every processed value every frame, including a repeated identical value; a non-contributing result is never delivered. Disposing the returned `IPushBinding` stops delivery.

## Consuming: Pull

`IInputActions.Pull<T>` resolves an on-demand [`IPullBinding<T>`](xref:Sparkitect.Input.IPullBinding`1); `Read()` returns the same already-processed result the implementation produced this frame without resampling.

## Settings-Backed Rebinding

Every action's default binding is itself a declared [Setting](xref:sparkitect.settings) — `ActionRegistry.RegisterAction` declares it through `ISettingsManager`. Rebinding an action means writing that setting through the normal Settings API, not a separate input-specific verb.

The extension seam for teaching an implementation new binding shapes (`RegisterBindingAdapter`, `Rebind`, `FindBindingsReferencing` on `IWindowInputBindings`) is documented on the interface's XML docs; no sample currently exercises a second binding adapter.

## See Also

- [Settings](xref:sparkitect.settings) for the ordered-source resolution rebinding writes through
- [Registry System](xref:sparkitect.core.registry-system) for the generic registration pattern actions build on
