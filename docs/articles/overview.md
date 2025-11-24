---
uid: articles.overview
title: Engine Overview
description: Core architectural principles and patterns in Sparkitect
---

# Engine Overview

Sparkitect is a modular 3D game engine built on .NET 10 where **everything is a mod**. This document introduces the fundamental architectural principles and patterns that apply across all engine systems.

## Core Philosophy: Integration Over Duplication

The engine is designed around **integration over duplication** - systems compose together through shared mechanisms rather than running in parallel. This means:

- **Unified discovery**: All systems use the same attribute-driven discovery pattern
- **Shared containers**: Dependency injection, registries, and state management all use the builder pattern with immutable containers
- **Mod-first design**: Mods extend systems using the exact same patterns the engine uses internally

There is no distinction between "engine code" and "mod code" at the architectural level. Both follow the same patterns, use the same discovery mechanisms, and compose through the same container hierarchies.

This approach provides:
- **Consistency**: Same patterns everywhere reduces cognitive load
- **Extensibility**: New systems integrate through existing mechanisms
- **Composition**: Systems naturally compose through shared patterns
- **Mod-friendly**: Mods are first-class citizens, not an afterthought

## Two-Phase Design: Transition and Simulation

The engine operates in two distinct phases with fundamentally different characteristics:

### Transition Phase

During transitions, the engine is **reconfiguring its runtime state**:

- **Configuration is mutable**: Services can be registered, containers built, registries populated
- **Heavy initialization happens**: Dependency resolution, module activation, state frame creation
- **No simulation runs**: Game logic is paused

This is when state changes occur: entering a new state, loading modules, switching game sessions.

### Simulation Phase

During simulation, the engine is **actively running game logic**:

- **Everything is immutable**: All configuration is frozen, no container mutations
- **Performance-critical**: No allocations, deterministic execution, lock-free access
- **Concurrent access is safe**: Immutability enables thread-safe reads without locks

This is the main frame loop where per-frame functions execute.

### Why This Design?

This separation provides critical benefits:

**Performance**: By freezing configuration during simulation, the engine avoids locks and enables cache-friendly access patterns. Heavy work is confined to transitions where frame timing isn't critical.

**Safety**: Immutability during simulation prevents entire classes of bugs. No race conditions on shared state, no accidental configuration mutations mid-frame.

**Predictability**: You know exactly when configuration can change (transitions only) versus when it's guaranteed stable (simulation). This makes reasoning about system behavior much simpler.

**Debuggability**: When everything is immutable during frames, debugging is straightforward - no spooky action at a distance where one system modifies another's configuration.

## Foundational Patterns

Sparkitect uses several recurring patterns across all systems. Understanding these patterns helps you work with any part of the engine.

### Attribute-Driven Discovery

The engine discovers mod-provided implementations through **paired attributes and base classes**:

```csharp
[DiscoveryAttribute]
public class MyImplementation : BaseClass
{
    // Your code here
}
```

The attribute marks your class for discovery. The base class (or interface) declares what the implementation does. The engine finds all marked classes across loaded mods and processes them deterministically.

**Key insight**: You only write the attribute and implementation. Source generators create all the infrastructure code (marked with `[CompilerGenerated]`) to wire everything together.

This pattern appears everywhere:
- State modules and functions
- Service registration
- Registry definitions
- Facade mappings

### Builder Pattern for Containers

All containers follow a strict lifecycle that enforces the two-phase design:

1. **Builder Phase** (Transition): Accumulate registrations, validate dependencies
2. **Build**: Transition from builder to container (immutability barrier)
3. **Runtime Phase** (Simulation): Read-only access, resolution only

```csharp
// Transition: Build container
var builder = new CoreContainerBuilder(parentContainer);
builder.Register<MyService_Factory>();  // Accumulate
var container = builder.Build();         // Freeze

// Simulation: Use container
var service = container.Resolve<IMyService>();  // Read-only
```

Builders are used during transitions. Containers are used during simulation. This separation is enforced at the type level - you can't mutate a container.

### Source Generation

Sparkitect uses extensive source generation to achieve **zero reflection at runtime** in performance-critical paths.

**The mental model is simple**: You write attributes on your code, generators create all the infrastructure.

```csharp
// You write this:
[SomeAttribute<IMyInterface, MyModule>]
public class MyClass : IMyInterface
{
    // Your implementation
}

// Generators create (marked [CompilerGenerated]):
// - Factory classes
// - Registration configurators
// - Wiring code
```

**Why source generation?**

- **Zero reflection**: Direct method calls instead of runtime lookups. Critical for frame-loop performance.
- **Compile-time validation**: Invalid configurations are compiler errors, not runtime crashes. Analyzers enforce patterns at build time.
- **Type safety**: All relationships are checked by the C# compiler.
- **Determinism**: Generated code has predictable, inspectable output.

You don't need to understand generator implementation to use the engine. Write your attributes and implementations, generators handle the rest. Generated code is marked with `[CompilerGenerated]` - if you see that attribute, you're looking at infrastructure, not something you need to write.

## Core Patterns in Practice

When working with the engine, you'll use these patterns through attributes and base classes. The specifics of each system are documented in their respective articles.

### Service Definition Pattern

Services use attributes to declare their interface and scope. Source generators create factories and registration infrastructure.

### State Logic Pattern

Static methods with scheduling attributes define when logic executes. Dependency injection happens through method parameters, resolved by generated wrappers.

### Registry Pattern

Registries are DI-instantiated classes with methods that handle registration. Generators create attributes that mods use to register objects declaratively.

## How Systems Integrate

Systems compose through the patterns above:

1. **State transitions** trigger the builder phase (transition)
2. **Builders** collect registered services, modules, and registries
3. **Build** creates immutable containers
4. **Main loop** executes frame functions (simulation)

Each system integrates via discoverable entrypoints:
- Want to add services? Mark classes with service attributes
- Want to add state logic? Create state modules with function attributes
- Want to register objects? Implement registry methods and use generated attributes

New systems integrate through existing mechanisms, not by inventing new ones. This is the "integration over duplication" philosophy in practice.

## Working with the Engine

As a mod developer, you'll interact with these patterns through:

1. **Marking classes with attributes**: Service attributes, registry attributes, etc.
2. **Implementing base classes**: State modules, state descriptors, etc.
3. **Writing static methods**: State functions with scheduling attributes
4. **Using generated attributes**: Registry registration attributes

All infrastructure code is generated with `[CompilerGenerated]` - you write the domain logic, generators handle the wiring.

For working examples, refer to the samples directory in the project. Each major system also has detailed documentation covering specific APIs and usage patterns.

## Next Steps

- **[Core Module Documentation](core/index.md)**: Deep dive into foundational systems
- **[Dependency Injection](core/dependency-injection.md)**: Service registration and resolution
- **[Game State System](core/game-state-system.md)**: State management and lifecycle
- **[Registry System](core/registry-system.md)**: Object registration and identification
- **[Modding Framework](core/modding-framework.md)**: Mod structure and loading