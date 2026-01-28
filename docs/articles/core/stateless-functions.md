---
uid: articles.core.stateless-functions
title: Stateless Functions
description: Attribute-based static functions with DI and scheduling
---

# Stateless Functions

Stateless Functions are attribute-marked static methods that define behavior in Sparkitect. They form the foundation for module and state logic, using dependency injection for parameters and scheduling attributes to control when they execute.

## Core Concepts

### What is a Stateless Function?

A stateless function is a static method marked with a function attribute (e.g., `[PerFrameFunction]` or `[TransitionFunction]`) and a scheduling attribute that determines when it executes. The function receives dependencies through method parameters, which are resolved from the DI container at execution time.

Key characteristics:
- **Static methods only** - no instance state
- **Attribute-driven** - function type and scheduling declared via attributes
- **DI-injected parameters** - dependencies resolved automatically
- **Source-generated wrappers** - execution infrastructure generated at compile time

### Registry Association

Each function attribute associates the function with a specific registry through its generic type parameters:

| Function Attribute | Registry | Context |
|-------------------|----------|---------|
| `PerFrameFunctionAttribute` | `PerFrameRegistry` | `PerFrameContext` |
| `TransitionFunctionAttribute` | `TransitionRegistry` | `TransitionContext` |

The registry association determines:
- Which collector receives the function during registration
- What context data is available during scheduling decisions

### Function Wrappers

Source generators create wrapper classes for each stateless function. These wrappers:
- Implement `IStatelessFunction` and `IHasIdentification`
- Provide the `Identification` for ordering references
- Handle DI resolution and invocation

## Defining Stateless Functions

### Basic Example

```csharp
[PerFrameFunction("process_input")]
[PerFrameScheduling]
public static void ProcessInput()
{
    // Executes every frame
}
```

Requirements:
1. Method must be `static`
2. Must have exactly one function attribute (`[PerFrameFunction]` or `[TransitionFunction]`)
3. Must have exactly one scheduling attribute matching the function type
4. Containing type should implement `IHasIdentification` (or use `[ParentId<T>]`)

### With Dependency Injection

```csharp
[PerFrameFunction("update_physics")]
[PerFrameScheduling]
public static void UpdatePhysics(IPhysicsService physics, ITimeService time)
{
    physics.Step(time.DeltaTime);
}
```

Parameters are resolved from the current state's DI container:
- Use interface types, not concrete implementations
- Missing dependencies throw at state creation time
- Services are resolved using facade mapping (see [Dependency Injection](dependency-injection.md))

### With Ordering

```csharp
[PerFrameFunction("render_scene")]
[PerFrameScheduling]
[OrderAfter<RenderModule.BeginFrameFunc>]
public static void RenderScene(IRenderService renderer)
{
    renderer.Draw();
}
```

Ordering attributes reference the generated wrapper type to establish execution order.

## Scheduling

### Scheduling Attribute Pattern

Scheduling attributes inherit from a generic base that encodes the relationship between scheduling implementation, function type, context, and registry:

```
SchedulingAttribute<TScheduling, TStatelessFunction, TContext, TRegistry>
```

This ensures type safety: a scheduling attribute can only be applied to functions with matching context and registry types.

### Built-in Scheduling Types

**Per-Frame Functions** (`PerFrameFunctionAttribute`):

| Scheduling Attribute | When It Executes |
|---------------------|------------------|
| `[PerFrameScheduling]` | Every frame while owner module is loaded |

**Transition Functions** (`TransitionFunctionAttribute`):

| Scheduling Attribute | When It Executes |
|---------------------|------------------|
| `[OnCreateScheduling]` | Once when module/state is created |
| `[OnDestroyScheduling]` | Once when module/state is destroyed |
| `[OnFrameEnterScheduling]` | When state becomes the active leaf |
| `[OnFrameExitScheduling]` | When state stops being the active leaf |

### Scheduling Decision Flow

Each scheduling implementation determines inclusion based on context:

1. **PerFrameScheduling**: Included if owner module is loaded in current state stack
2. **OnCreateScheduling**: Included if entering transition AND owner is in delta modules (newly added)
3. **OnDestroyScheduling**: Included if exiting transition AND owner is in delta modules (being removed)
4. **OnFrameEnterScheduling**: Included if entering transition AND owner is loaded
5. **OnFrameExitScheduling**: Included if exiting transition AND owner is loaded

## Execution Ordering

### Ordering Attributes

Control execution order between functions using ordering attributes:

| Attribute | Purpose |
|-----------|---------|
| `[OrderBefore<T>]` | Execute before function T |
| `[OrderAfter<T>]` | Execute after function T |

Both attributes accept an optional `IsOptional` property:
- `IsOptional = false` (default): Constraint required; error if target not present
- `IsOptional = true`: Constraint ignored if target not present

### Cross-Module Ordering and IsOptional

When ordering against functions in other modules, the target module may not be loaded. Use `IsOptional` to handle this:

```csharp
// Required ordering (default) - ERROR if PhysicsModule not loaded
[PerFrameFunction("render")]
[PerFrameScheduling]
[OrderAfter<PhysicsModule.RunPhysicsFunc>]
public static void Render() { }

// Optional ordering - silently ignored if PhysicsModule not loaded
[PerFrameFunction("render")]
[PerFrameScheduling]
[OrderAfter<PhysicsModule.RunPhysicsFunc>(IsOptional = true)]
public static void Render() { }
```

**When to use IsOptional:**

| Scenario | IsOptional | Reason |
|----------|------------|--------|
| Same-module ordering | `false` | Always present |
| Required module dependency | `false` | Module guaranteed loaded |
| Optional module integration | `true` | Module may not be loaded |
| Engine-provided function | `false` | Engine modules always present |

**Error behavior:**
- `IsOptional = false` (default): Throws exception if target function not found during scheduling
- `IsOptional = true`: Constraint is silently ignored if target not found

**Best practice:** Declare explicit module dependencies for functions you order against. Only use `IsOptional = true` when the ordering is truly optional (e.g., "if audio module is loaded, run after audio").

### Type-Safe References

Ordering attributes use the generated wrapper type for type-safe cross-references:

```csharp
public partial class PhysicsModule : IStateModule
{
    [PerFrameFunction("prepare_physics")]
    [PerFrameScheduling]
    public static void PreparePhysics() { }

    [PerFrameFunction("run_physics")]
    [PerFrameScheduling]
    [OrderAfter<PreparePhysicsFunc>]  // References generated wrapper
    public static void RunPhysics() { }
}
```

For cross-module ordering:

```csharp
[PerFrameFunction("render")]
[PerFrameScheduling]
[OrderAfter<PhysicsModule.RunPhysicsFunc>]  // Cross-module reference
public static void Render() { }
```

> **Note:** Cross-module ordering requires consideration of module availability. If the referenced module isn't active, optional constraints (`IsOptional = true`) are silently ignored, while required constraints cause a scheduling error. See [Cross-Module Ordering and IsOptional](#cross-module-ordering-and-isoptional) above for detailed guidance.

## Source Generation

### What Gets Generated

For each stateless function, the source generator creates:

1. **Wrapper class** implementing `IStatelessFunction` and `IHasIdentification`
2. **Identification property** derived from containing type's identification + function key
3. **Invocation logic** that resolves DI parameters and calls the original method

### Generated Code Location

Generated code is placed in the `{RootNamespace}.CompilerGenerated` namespace. The actual namespace to use is determined by the mod settings parser in the source generators.

Example generated wrapper (conceptual):

```csharp
// In: MyMod.CompilerGenerated
public sealed class PreparePhysicsFunc : IStatelessFunction, IHasIdentification
{
    public static Identification Identification => ...;
    // Invocation and scheduling logic
}
```

The wrapper type name follows the pattern: `{MethodName}Func`.

## Best Practices

### Keep Functions Small and Focused

Each function should do one thing. Prefer multiple small functions over one large function:

```csharp
// Good: focused functions
[PerFrameFunction("update_time")]
[PerFrameScheduling]
public static void UpdateTime(ITimeService time) { ... }

[PerFrameFunction("process_commands")]
[PerFrameScheduling]
[OrderAfter<UpdateTimeFunc>]
public static void ProcessCommands(ICommandService commands) { ... }

// Avoid: monolithic functions
[PerFrameFunction("do_everything")]
[PerFrameScheduling]
public static void DoEverything(ITimeService time, ICommandService commands, ...) { ... }
```

### Use DI for Dependencies

Access services through method parameters, not static state:

```csharp
// Good: dependencies injected
[PerFrameFunction("update")]
[PerFrameScheduling]
public static void Update(ITimeService time, IPhysicsService physics)
{
    physics.Step(time.DeltaTime);
}

// Avoid: static service locator
[PerFrameFunction("update")]
[PerFrameScheduling]
public static void Update()
{
    var time = ServiceLocator.Get<ITimeService>();  // Don't do this
}
```

### Static Methods Only

Stateless functions must be static. They cannot access instance state:

```csharp
public partial class MyModule : IStateModule
{
    private int _counter;  // Instance field - NOT accessible from stateless functions

    [PerFrameFunction("tick")]
    [PerFrameScheduling]
    public static void Tick()
    {
        // Cannot access _counter here - method is static
    }
}
```

Use services registered in the DI container for any state that needs to persist across function calls.

### Ordering Sparingly

Use ordering constraints only when execution order matters. Independent functions can execute in any order:

```csharp
// These are independent - no ordering needed
[PerFrameFunction("update_audio")]
[PerFrameScheduling]
public static void UpdateAudio(IAudioService audio) { ... }

[PerFrameFunction("update_particles")]
[PerFrameScheduling]
public static void UpdateParticles(IParticleService particles) { ... }

// These have a dependency - ordering required
[PerFrameFunction("gather_render_commands")]
[PerFrameScheduling]
public static void GatherRenderCommands(IRenderService render) { ... }

[PerFrameFunction("execute_render")]
[PerFrameScheduling]
[OrderAfter<GatherRenderCommandsFunc>]
public static void ExecuteRender(IRenderService render) { ... }
```

## Attribute Reference

### Function Attributes

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[PerFrameFunction("key")]` | Method | Marks function for per-frame execution |
| `[TransitionFunction("key")]` | Method | Marks function for transition execution |
| `[ParentId<TOwner>]` | Method | Overrides owner identification |

### Scheduling Attributes

| Attribute | Function Type | Purpose |
|-----------|--------------|---------|
| `[PerFrameScheduling]` | PerFrameFunction | Execute every frame |
| `[OnCreateScheduling]` | TransitionFunction | Execute on module/state creation |
| `[OnDestroyScheduling]` | TransitionFunction | Execute on module/state destruction |
| `[OnFrameEnterScheduling]` | TransitionFunction | Execute when state becomes active |
| `[OnFrameExitScheduling]` | TransitionFunction | Execute when state becomes inactive |

### Ordering Attributes

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[OrderBefore<T>]` | Method | Execute before function T |
| `[OrderAfter<T>]` | Method | Execute after function T |

Both ordering attributes support `IsOptional` property (default: `false`).

## See Also

- [Game State System](game-state-system.md) - How modules and states use stateless functions
- [Dependency Injection](dependency-injection.md) - Service resolution and facade mapping
- [Registry System](registry-system.md) - How functions are registered and discovered
