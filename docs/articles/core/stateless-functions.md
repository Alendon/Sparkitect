---
uid: sparkitect.core.stateless-functions
title: Stateless Functions
description: Generic infrastructure for attribute-driven static functions with DI and scheduling
---

# Stateless Functions

Stateless functions are static methods whose only interaction with the outside world is through DI-injected parameters. By requiring all dependencies to be explicit method parameters, no function can carry hidden state that would prevent mods from replacing or extending behavior. This makes stateless functions the primary building block for moddable execution logic.

The stateless function infrastructure is a generic module. Consumers define their own function types, scheduling policies, and registries on top of it. The [Game State System](xref:sparkitect.core.game-state-system) is the current consumer, providing per-frame and transition function types, but the infrastructure is designed for any system to add new categories.

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

[`[PerFrameFunction]`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute) is a function type attribute provided by the Game State System. [`[PerFrameScheduling]`](xref:Sparkitect.GameState.PerFrameSchedulingAttribute) determines when the function runs. Method parameters are resolved from the current DI container using [facade mapping](xref:sparkitect.core.dependency-injection#facade-integration).

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

The registry extends [`StatelessFunctionRegistryBase`](xref:Sparkitect.Stateless.StatelessFunctionRegistryBase) and implements `IRegistry`. It is a thin wrapper that provides the registry identifier and inherits `Register<T>`/`Unregister` from the base:

```csharp
[Registry(Identifier = "perframe_function", External = true)]
public sealed partial class PerFrameRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "perframe_function";
}
```

The `External = true` flag means the registry is not backed by a data collection but serves as a typed entry point for the stateless function registration pipeline.

The source generator reads `TRegistry` from the function attribute to determine which registry receives the `Register<WrapperType>(id)` call.

### Scheduling

Scheduling is split into two halves: an attribute (compile-time marker) and an implementation (runtime decision logic).

The scheduling attribute extends [`SchedulingAttribute<TScheduling, TFunc, TContext, TRegistry>`](xref:Sparkitect.Stateless.SchedulingAttribute`4). The four type parameters enforce at compile time that the scheduling can only be applied to a compatible function type:

```csharp
// Attribute: marks a method as using PerFrameScheduling
public sealed class PerFrameSchedulingAttribute
    : SchedulingAttribute<PerFrameScheduling, PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>;
```

The scheduling implementation implements [`IScheduling<TFunc, TContext, TRegistry>`](xref:Sparkitect.Stateless.IScheduling`3). Its constructor receives the ordering attributes (`OrderAfterAttribute[]`, `OrderBeforeAttribute[]`) that were declared on the method. Its `BuildGraph` method decides whether to include the function and, if so, adds the node and ordering edges to the execution graph:

```csharp
public sealed class PerFrameScheduling
    : IScheduling<PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public PerFrameScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(
        IExecutionGraphBuilder builder, PerFrameContext context,
        Identification functionId, Identification ownerId)
    {
        // Inclusion check: is the owner module loaded, or is the owner the active leaf state?
        if (!context.IsModuleLoaded(ownerId) && context.StateStack[^1].StateId != ownerId)
            return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);
        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
```

The same pattern produces different behavior by varying the inclusion check. For example, `OnCreateScheduling` checks that the transition is an enter transition and that the owner is among the modules being added:

```csharp
public void BuildGraph(
    IExecutionGraphBuilder builder, TransitionContext context,
    Identification functionId, Identification ownerId)
{
    if (!context.IsEnterTransition) return;
    if (!context.DeltaModules.Contains(ownerId) && context.StateStack[^1].StateId != ownerId)
        return;

    builder.AddNode(functionId);
    // ... apply ordering
}
```

`OnDestroyScheduling` inverts the direction (`if (context.IsEnterTransition) return`). `OnFrameEnterScheduling` checks `IsEnterTransition` but uses `IsModuleLoaded` instead of `DeltaModules`. Each scheduling type emerges from the same `IScheduling` contract with different logic in `BuildGraph`.

### Generated Scheduling Entrypoint

The source generator produces one scheduling entrypoint class per parent type per registry. This class extends `ApplySchedulingEntrypoint<TFunc, TContext>` and its `BuildGraph` method instantiates the scheduling objects with ordering attributes, then calls each one:

```csharp
// Generated: one per parent type per registry
[ApplySchedulingEntrypointAttribute<PerFrameFunctionAttribute>]
internal class PhysicsModule_PerFrame_SchedulingEntrypoint
    : ApplySchedulingEntrypoint<PerFrameFunctionAttribute, PerFrameContext>
{
    public override void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context)
    {
        // For each function in this parent type:
        {
            // Ordering attributes collected from the method declaration
            OrderAfterAttribute[] param_0 = [
                new OrderAfterAttribute<PreparePhysicsFunc>()
            ];
            OrderBeforeAttribute[] param_1 = [];

            // Scheduling impl constructed with ordering params
            var scheduling = new PerFrameScheduling(param_0, param_1);
            scheduling.BuildGraph(builder, context,
                IdentificationHelper.Read<RunPhysicsFunc>(),
                IdentificationHelper.Read<PhysicsModule>());
        }
    }
}
```

This is how ordering attributes flow from method declarations to the scheduling implementation. The generator reads `[OrderAfter<T>]` and `[OrderBefore<T>]` attributes from each method, constructs their instances as arrays, and passes them to the scheduling constructor. The scheduling implementation then calls `Apply` on each ordering attribute during `BuildGraph`, which adds edges to the execution graph.

### The Execution Pipeline

When a consumer calls [`IStatelessFunctionManager.GetSorted<TFunc, TContext, TRegistry>(...)`](xref:Sparkitect.Stateless.IStatelessFunctionManager), the following happens:

1. **Discover entrypoints**: All `ApplySchedulingEntrypoint<TFunc, TContext>` implementations are discovered from loaded mods via the entrypoint container
2. **Build the graph**: Each entrypoint's `BuildGraph` is called, which instantiates scheduling objects per function and conditionally adds nodes and ordering edges to the `IExecutionGraphBuilder`
3. **Resolve ordering**: The builder resolves the graph via topological sort. Required ordering constraints with missing targets throw `InvalidOperationException`. Optional constraints with missing targets are silently dropped. Cycles are detected and reported as errors.
4. **Instantiate wrappers**: For each function ID in sorted order, the wrapper type is instantiated and `Initialize()` is called with the DI container and facade map, resolving all method parameters
5. **Return**: The sorted list of initialized `IStatelessFunction` wrappers is returned, ready for the consumer to call `Execute()` on each

The consumer controls what `TContext` to pass and when to call `GetSorted`, making the entire scheduling and resolution pipeline generic. The GSM calls it during state frame creation; a networking system could call it when a connection event occurs.

## See Also

- [Game State System](xref:sparkitect.core.game-state-system) for how the GSM uses stateless functions for per-frame and transition logic
- [Dependency Injection](xref:sparkitect.core.dependency-injection) for service resolution and facade mapping
- [Registry System](xref:sparkitect.core.registry-system) for how function registries work
