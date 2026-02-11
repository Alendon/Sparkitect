---
uid: sparkitect.getting-started.overview
title: Engine Overview
description: Architectural patterns and lifecycle in Sparkitect
---

# Engine Overview

This page covers the recurring patterns and lifecycle you will encounter when working with Sparkitect. For philosophy, target use cases, and a getting-started tutorial, see the [Introduction](xref:sparkitect.getting-started.intro).

## Everything is a Mod

Mods use the same patterns and mechanisms as the engine itself. There is no separate "engine API" vs "mod API": both register services through the same attributes, define logic through the same function types, and populate registries through the same generated infrastructure. The engine's own [`CoreModule`](xref:Sparkitect.GameState.CoreModule) services are registered the same way a mod registers its services.

This means anything the engine does, a mod can extend or replace.

## Attribute-Driven Discovery

The engine discovers your code through attributes paired with base classes or interfaces:

```csharp
[StateService<IMyService, MyModule>]
public class MyService : IMyService
{
    // Your implementation
}
```

The attribute marks the class for discovery. The base class or interface declares what it provides. The engine collects all marked types across loaded mods and processes them during state transitions.

You will see this pattern throughout the engine:

| Pattern | Attribute | Details |
|---------|-----------|---------|
| Service registration | [`[StateService<TInterface, TModule>]`](xref:Sparkitect.GameState.StateServiceAttribute`2) | [Dependency Injection](xref:sparkitect.core.dependency-injection) |
| Module registration | `[ModuleRegistry.RegisterModule("key")]` | [Game State System](xref:sparkitect.core.game-state-system) |
| Transition logic | [`[TransitionFunction]`](xref:Sparkitect.Stateless.TransitionFunctionAttribute) | [Stateless Functions](xref:sparkitect.core.stateless-functions) |
| Per-frame logic | [`[PerFrameFunction]`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute) | [Stateless Functions](xref:sparkitect.core.stateless-functions) |

You write the attribute and implementation. Source generators handle the rest.

## Source Generation

Sparkitect uses Roslyn source generators to create infrastructure code at compile time. The frame loop executes with zero reflection: generated wrapper classes hold pre-resolved dependencies and call your methods directly.

```csharp
// You write:
[TransitionFunction("process")]
[OnCreateScheduling]
private static void Process(IMyService service)
{
    // Your logic
}

// A generator creates a wrapper (marked [CompilerGenerated]) that:
// - Resolves IMyService from the container at construction
// - Calls Process() with the cached reference during execution
```

State transitions still use reflection for type discovery, but this happens outside the frame loop where performance is not critical.

Invalid configurations are caught at build time through analyzers rather than at runtime. Generated code is always marked with `[CompilerGenerated]`. If you see that attribute on a type, it is infrastructure you do not need to write or modify.

## Two-Phase Lifecycle

The engine alternates between two phases:

**Transition**: The engine is reconfiguring. Services are registered, containers are built, modules are activated. Game logic is not running.

**Simulation**: The main frame loop is running. All containers and configuration are frozen (immutable). Per-frame functions execute against read-only containers.

Immutability during simulation means container reads require no locks. This does not eliminate concurrency concerns in your own code, but it guarantees that the engine's configuration cannot change underneath you mid-frame.

## Builder Pattern

Containers follow a builder-to-container lifecycle that enforces the two-phase separation:

```csharp
// Internal engine lifecycle (mod authors do not construct builders directly)
var builder = new CoreContainerBuilder(parentContainer);
builder.Register<MyService_Factory>();  // Accumulate during transition
var container = builder.Build();         // Freeze

// Simulation: read-only access
var service = container.Resolve<IMyService>();
```

Builders are available during transitions. Once `Build()` is called, the resulting container has no mutation methods. This separation is enforced at the type level.

## See Also

- [Core Systems](xref:sparkitect.core) for the foundational engine modules
- [Dependency Injection](xref:sparkitect.core.dependency-injection) for service registration and resolution
- [Game State System](xref:sparkitect.core.game-state-system) for state management and lifecycle
- [Modding Framework](xref:sparkitect.core.modding-framework) for mod structure and loading
