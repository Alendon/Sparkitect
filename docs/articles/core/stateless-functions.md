---
uid: sparkitect.core.stateless-functions
title: Stateless Functions
description: Generic infrastructure for attribute-driven static functions with DI and scheduling
---

# Stateless Functions

Stateless functions are static methods whose only interaction with the outside world is through DI-injected parameters. By requiring all dependencies to be explicit method parameters, no function can carry hidden state that would prevent mods from replacing or extending behavior. This makes stateless functions the primary building block for moddable execution logic.

The stateless function infrastructure is a generic module. Consumers define their own function types, scheduling policies, and registries on top of it. Two engine systems consume it today: the [Game State System](xref:sparkitect.core.game-state-system) provides per-frame and transition function types, and the [ECS](xref:sparkitect.ecs.systems) provides the ECS system-function category. The infrastructure is designed for any system to add new categories.

## Defining a Function

A stateless function is a static method with three attributes: a function type, a scheduling policy, and (implicitly) an owning type with identification.

```csharp
public partial class PhysicsModule : IStateModule
{
    [PerFrameFunction("run_physics")]
    [PerFrameScheduling]
    public static void RunPhysics(IPhysicsService physics, ITimeService time)
    {
        physics.Step(time.DeltaTime);
    }
}
```

[`[PerFrameFunction]`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute) is a function type attribute provided by the Game State System. [`[PerFrameScheduling]`](xref:Sparkitect.GameState.PerFrameSchedulingAttribute) determines when the function runs. Method parameters are resolved from the current DI container using [facade mapping](xref:sparkitect.core.dependency-injection#facade-resolution).

Requirements:

1. Method must be `static`
2. Exactly one function type attribute
3. Exactly one scheduling attribute matching that function type
4. Containing type must implement [`IHasIdentification`](xref:Sparkitect.Modding.IHasIdentification), or the method must use [`[ParentId<T>]`](xref:Sparkitect.Stateless.ParentIdAttribute`1). If neither is provided, the source generator skips the function and analyzer SPARK0404 reports an error.

### Parent ID Override

When a function lives in a type that is not its logical owner, use `[ParentId<T>]` to specify who owns it:

```csharp
public partial class BackgroundColorState
{
    [PerFrameFunction("background_color_update")]
    [PerFrameScheduling]
    [ParentId<PongModule>]
    public static void UpdateBackgroundColor(IBackgroundColorService bg)
    {
        bg.Update();
    }
}
```

The generated wrapper uses `PongModule`'s identification as the parent, not `BackgroundColorState`.

## Built-in Function Types

The Game State System provides two function types:

| Function Attribute | Scheduling Options | Used For |
|---|---|---|
| [`[PerFrameFunction]`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute) | [`[PerFrameScheduling]`](xref:Sparkitect.GameState.PerFrameSchedulingAttribute) | Logic that runs every frame |
| [`[TransitionFunction]`](xref:Sparkitect.Stateless.TransitionFunctionAttribute) | [`[OnCreateScheduling]`](xref:Sparkitect.GameState.OnCreateSchedulingAttribute), [`[OnDestroyScheduling]`](xref:Sparkitect.GameState.OnDestroySchedulingAttribute), [`[OnFrameEnterScheduling]`](xref:Sparkitect.GameState.OnFrameEnterSchedulingAttribute), [`[OnFrameExitScheduling]`](xref:Sparkitect.GameState.OnFrameExitSchedulingAttribute) | Logic that runs during state transitions |

Each scheduling attribute controls when the function is included in execution. The GSM evaluates scheduling based on which modules are loaded and what transition is occurring. See [Game State System](xref:sparkitect.core.game-state-system) for the specific inclusion rules.

The ECS adds a third category built on the same infrastructure:

| Function Attribute | Scheduling | Used For |
|---|---|---|
| [`[EcsSystemFunction]`](xref:Sparkitect.ECS.Systems.EcsSystemFunctionAttribute) | [`[EcsSystemScheduling]`](xref:Sparkitect.ECS.Systems.EcsSystemSchedulingAttribute) | A system method that runs each frame over its query, scoped to an owning system group |

`[OrderAfter<T>]` and `[OrderBefore<T>]` apply to ECS system functions the same way, but ordering is resolved within a single system group: a constraint that references a system in a different group throws `InvalidOperationException`. See [Systems and Groups](xref:sparkitect.ecs.systems) for the group tree and execution model.

```csharp
public partial class AudioModule : IStateModule
{
    [TransitionFunction("init_audio")]
    [OnCreateScheduling]
    public static void InitAudio(IAudioService audio)
    {
        audio.Initialize();
    }

    [TransitionFunction("cleanup_audio")]
    [OnDestroyScheduling]
    public static void CleanupAudio(IAudioService audio)
    {
        audio.Shutdown();
    }
}
```

## Execution Ordering

Control execution order between functions with [`[OrderBefore<T>]`](xref:Sparkitect.Stateless.OrderBeforeAttribute`1) and [`[OrderAfter<T>]`](xref:Sparkitect.Stateless.OrderAfterAttribute`1):

```csharp
public partial class PhysicsModule : IStateModule
{
    [PerFrameFunction("prepare_physics")]
    [PerFrameScheduling]
    public static void PreparePhysics() { }

    [PerFrameFunction("run_physics")]
    [PerFrameScheduling]
    [OrderAfter<PreparePhysicsFunc>]
    public static void RunPhysics(IPhysicsService physics) { }
}
```

The `<T>` parameter references the generated wrapper type (see [Source Generation](#source-generation)). For cross-module references, use the fully qualified wrapper: `[OrderAfter<PhysicsModule.RunPhysicsFunc>]`.

Both attributes accept an `IsOptional` property (default: `false`):

- `IsOptional = false`: throws `InvalidOperationException` if the target function is not present during scheduling
- `IsOptional = true`: constraint is silently ignored if the target is not present

Use `IsOptional = true` when ordering against functions from optional module dependencies. For required dependencies or same-module ordering, the default is correct since the target is guaranteed to be present.

Circular ordering constraints are detected at resolution time and result in an error. Only add ordering when execution order actually matters; independent functions can run in any order.

## Source Generation

For each stateless function, the source generator creates a wrapper class as a nested type within the containing type. The wrapper implements [`IStatelessFunction`](xref:Sparkitect.Stateless.IStatelessFunction) and `IHasIdentification`, handling DI resolution and invocation.

The wrapper type name follows the pattern `{IdentifierPascalCase}Func`, derived from the attribute's identifier string (not the method name). For example, `[PerFrameFunction("prepare_physics")]` generates `PreparePhysicsFunc`.

```csharp
// Generated as MyMod.PhysicsModule.PreparePhysicsFunc
public class PreparePhysicsFunc : IStatelessFunction, IHasIdentification
{
    public Identification Identification => ...;
    public Identification ParentIdentification => ...;
    // DI parameter fields, Initialize(), Execute()
}
```

The generator works generically: it discovers function attributes by checking inheritance from [`StatelessFunctionAttribute`](xref:Sparkitect.Stateless.StatelessFunctionAttribute), not by looking for specific types. New function categories are automatically supported without generator changes.

Beyond the wrapper, the generator also produces registry registration entries, identification property constants, and a scheduling entrypoint class. The scheduling entrypoint is described in the next section.

## How Function Types Are Built

This section walks through how the GSM's function types are constructed on top of the generic infrastructure. The same pattern applies to any new function category.

### Function Attribute and Registry

A function category starts with two things: a function attribute and a registry.

The function attribute extends [`StatelessFunctionAttribute<TContext, TRegistry>`](xref:Sparkitect.Stateless.StatelessFunctionAttribute`2), which binds the function to a specific context type and registry:

```csharp
// Concrete function type: binds to PerFrameContext and PerFrameRegistry
public sealed class PerFrameFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<PerFrameContext, PerFrameRegistry>(identifier);
```

The registry extends [`StatelessFunctionRegistryBase`](xref:Sparkitect.Stateless.StatelessFunctionRegistryBase) and implements `IRegistry<TModule>`. It is a thin wrapper that inherits `Register<T>`/`Unregister` from the base; the `[Registry]` attribute supplies the identifier:

```csharp
[Registry(Identifier = "perframe_function", External = true)]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry<CoreModule>;
```

The `External = true` flag means the registry is not backed by a data collection but serves as a typed entry point for the stateless function registration pipeline.

The source generator reads `TRegistry` from the function attribute to determine which registry receives the `Register<WrapperType>(id)` call.

### Scheduling

Scheduling is split into two halves: an attribute (compile-time marker) and an implementation (the metadata that drives graph inclusion).

The scheduling attribute extends [`SchedulingAttribute<TScheduling>`](xref:Sparkitect.Stateless.SchedulingAttribute`1), whose single type parameter names the scheduling metadata type. `SchedulingAttribute<TScheduling>` derives from `MetadataAttribute<TScheduling>`, so scheduling instances are collected through the same metadata mechanism as any other ordered category:

```csharp
public sealed class PerFrameSchedulingAttribute : SchedulingAttribute<PerFrameScheduling>;
```

The scheduling implementation implements [`IScheduling`](xref:Sparkitect.Stateless.IScheduling), which carries only an `OwnerId`, set by the generated metadata entrypoint during collection. The implementation is constructed with the ordering attributes declared on the method and exposes a context-specific `BuildGraph` that decides whether to include the function and, if so, adds the node and ordering edges:

```csharp
public sealed class PerFrameScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public PerFrameScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context, Identification functionId)
    {
        // Inclusion check: is the owner module loaded, or is the owner the active leaf state?
        if (!context.IsModuleLoaded(OwnerId) && context.StateStack[^1].StateId != OwnerId)
            return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);
        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
```

`BuildGraph` is not part of `IScheduling`; each scheduling type declares its own overload against its context type. The same shape produces different behavior by varying the inclusion check. `OnCreateScheduling` checks that the transition is an enter transition and that the owner is among the modules being added:

```csharp
public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
{
    if (!context.IsEnterTransition) return;
    if (!context.DeltaModules.Contains(OwnerId) && context.StateStack[^1].StateId != OwnerId)
        return;

    builder.AddNode(functionId);
    // ... apply ordering
}
```

`OnDestroyScheduling` inverts the direction (`if (context.IsEnterTransition) return`). `OnFrameEnterScheduling` checks `IsEnterTransition` but uses `IsModuleLoaded` instead of `DeltaModules`. Each type emerges from the same `IScheduling` metadata with different inclusion logic.

### Generated Metadata Entrypoint

The source generator produces one metadata entrypoint per parent type. It constructs each scheduling instance with its ordering attributes, sets its `OwnerId`, and adds it to the collected metadata map. This is the same `ApplyMetadataEntrypoint<T>` mechanism the [metadata generator](xref:sparkitect.tooling.source-generation) emits for any ordered category, specialized here as `ApplyMetadataEntrypoint<IScheduling>`:

```csharp
// Generated: contributes scheduling metadata keyed by function id
public override void CollectMetadata(Dictionary<Identification, IScheduling> metadata)
{
    // Ordering attributes collected from the method declaration
    OrderAfterAttribute[] orderAfter = [ new OrderAfterAttribute<PreparePhysicsFunc>() ];
    OrderBeforeAttribute[] orderBefore = [];

    metadata[IdentificationHelper.Read<RunPhysicsFunc>()] =
        new PerFrameScheduling(orderAfter, orderBefore)
        {
            OwnerId = IdentificationHelper.Read<PhysicsModule>()
        };
}
```

This is how ordering attributes flow from method declarations into the scheduling instance. The generator reads `[OrderAfter<T>]` and `[OrderBefore<T>]` from each method, constructs their instances, and passes them to the scheduling constructor. Each ordering attribute later calls `Apply` during `BuildGraph`, adding an edge to the execution graph.

### The Execution Pipeline

A consumer turns the collected scheduling metadata into an ordered, instantiated function list in four steps. The GSM runs this during state-frame creation:

1. **Collect metadata**: resolve every `ApplyMetadataEntrypoint<IScheduling>` from the loaded mods and call `CollectMetadata`, producing a `Dictionary<Identification, IScheduling>` of all scheduling instances.
2. **Build the graph**: call [`IStatelessFunctionManager.CreateGraphBuilder()`](xref:Sparkitect.Stateless.IStatelessFunctionManager) for the pass, then call `BuildGraph` on each scheduling instance of the target category, conditionally adding nodes and ordering edges.
3. **Resolve ordering**: `builder.Resolve()` topologically sorts the graph. Required ordering constraints with missing targets throw `InvalidOperationException`; optional constraints with missing targets are dropped; cycles are reported as errors.
4. **Instantiate wrappers**: `FunctionManager.InstantiateWrappers(sortedIds, scope)` creates each wrapper in sorted order and initializes it against the resolution scope, resolving all method parameters. It returns the `IStatelessFunction` list, ready for the consumer to call `Execute()` on each.

The consumer picks the context and which scheduling subtypes participate, so the same manager drives per-frame, transition-enter, and transition-exit passes from a single metadata map.

## See Also

- [Game State System](xref:sparkitect.core.game-state-system) for how the GSM uses stateless functions for per-frame and transition logic
- [Dependency Injection](xref:sparkitect.core.dependency-injection) for service resolution and facade mapping
- [Registry System](xref:sparkitect.core.registry-system) for how function registries work
