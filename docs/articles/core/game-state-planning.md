---
status: planning
title: Game State Planning
---

# Game State Planning

This document outlines the planned Game State System for Sparkitect. It focuses on the logical model of state functions and scheduling, not the final APIs. Code samples are illustrative and may evolve; they are here to clarify intent.

## Goal
Provide a clean, reliable, and extensible mechanism to define engine/game states with distinct configurations and services, and to manage deterministic transitions between them — without constraining authors to a specific gameplay structure.

## Core Concepts
- State: Registered by the Registry System (category: "State"). A State has an id (mapped to `Identification`) and a single parent. The root/bootstrap state is handled outside registries.
- Module: Registered by the Registry System (category: "State Module"). States are composed from ordered modules. Modules are packaging/building blocks and are not invoked directly.
- Unified State Function: A single atomic unit, implemented as a static method. Scheduling defines when/how it runs (per-frame or on-change). This unifies prior “Feature” and “Transition” concepts.
- Component ownership: Functions belong to modules (and optionally to the state). Modules are activated per-state; GSM (Game State Manager) gathers functions from the active module set.
- No tags/capabilities: Module presence in the state hierarchy drives activation; additional tag systems are not used.
- Registry integration: Registries define states/modules, with GSM finalizing composition after registry runs. Registries provide the API to build states/modules; GSM performs the final construction.

## Atomic Components
The system orchestrates two kinds of things: state functions (logic) and services (data/behavior). States compose functions and services through module inclusion.

### State-Bound Service
A state-bound service is instantiated only when its owning module is active in the current state. These are singletons within the state container’s lifetime.

- Service implementation is marked on the implementation class: `[StateService<TInterface>]`.
- Facade contract is declared on the interface: `[StateFacade<TFacadeInterface>]`.
- Modules/States explicitly declare which service interfaces they use. The generator verifies that a matching implementation exists and that it implements the required facade interface(s).
- Multiple states/modules may depend on the same service interface; GSM builds the child state container for the active state from the collected factories.

Example (illustrative):
```csharp
// Facade contract is attached to the API interface
[StateFacade<ITimeFacade>]
public interface ITimeApi { /* ... */ }

// Service implementation declares which API it implements for the state system
[StateService<ITimeApi>]
internal sealed class TimeService : ITimeApi, ITimeFacade { /* ... */ }

// A module (or state) explicitly declares the services it uses
public static class CoreModule
{
    public static readonly Type[] UsedServices = { typeof(ITimeApi) };
}
```

### Unified State Function
One function type covers both “per-frame work” and “on-change work”. Scheduling decides when it runs. All state functions are static; wrappers resolve inputs and invoke them.

Scheduling kinds (v1):
- PerFrame: runs every tick while active
- OnCreate: lifecycle hook - runs when module/state container is created (module creation phase)
- OnDestroy: lifecycle hook - runs when module/state container is destroyed (module destruction phase)
- OnFrameEnter: transition hook - runs when state becomes the active leaf (entering frame)
- OnFrameExit: transition hook - runs when leaving the active leaf position (exiting frame)

Note: Scheduling is expressed via attributes (e.g., `[PerFrame]`, `[OnFrameEnter]`, `[OnDestroy]`) rather than an enum. This enables v2 extensibility for mod‑provided triggers without breaking existing code.

Analyzer requirement (v1): Every `[StateFunction]` must also have exactly one scheduling attribute. A common base marker (e.g., `StateScheduleAttribute`) is used so analyzers can verify the presence of a scheduling attribute. Functions without one are invalid in v1 (v2 may allow manual invocation).

Additional properties:
- Key: unique within a module
- Ordering:
  - Same declaration scope (module or state): `OrderBefore("fn")` / `OrderAfter("fn")`.
  - Cross‑scope (module or state): `OrderBefore<TDecl>("fn")` / `OrderAfter<TDecl>("fn")` where `TDecl` is a module or state declaration type.
    - The analyzer validates `TDecl` refers to a valid declaration (markers: `IStateModule` / `IStateDescriptor`).
    - If `TDecl` is not active in the current state configuration, the cross‑scope ordering is ignored.

Examples (illustrative):
```csharp
public static class UiModule
{
    public const string InitRoot = "ui_root_init";
    public const string Pump = "ui_pump";

    [StateFunction(InitRoot)]
    [OnCreate] // lifecycle - module creation
    public static void InitializeUiRoot(IStateContainer container)
    {
        // One-time UI root setup for the module's lifetime in this path
    }

    [StateFunction(Pump)]
    [PerFrame]
    public static void UiTick(IStateContainer container)
    {
        // Per-frame UI event pump (render thread coordination happens elsewhere)
    }
}

public static class GameplayModule
{
    public const string HudEnter = "hud_enter";
    public const string HudExit = "hud_exit";

    [StateFunction(HudEnter)]
    [OnFrameEnter]
    [OrderAfter<UiModule>(UiModule.InitRoot)]
    public static void SetupHud(IStateContainer container, IStateDataAccessor<GameplayHudData> hud)
    {
        // Register HUD widgets for this state (registries may be invoked here)
    }

    [StateFunction(HudExit)]
    [OnFrameExit]
    [OrderBefore<UiModule>(UiModule.InitRoot)] // runs before UI root teardown if that occurs
    public static void TeardownHud(IStateContainer oldContainer, IStateDataAccessor<GameplayHudData> hud)
    {
        // Unregister HUD widgets; explicit pairing with SetupHud
    }
}
```

## Scopes
Scopes define when on-change functions run and how they pair for teardown.

- Module scope: enter once when a module appears on the current state path; exit when it disappears.
- State scope: enter/exit on every leaf state change (even if the module set is unchanged).

Exit order reverses the enter order within the same scope (module or state).

## Component Activation per State
When a state includes a module, all of that module’s state functions are active by default.

Optional refinement (may be added later):
- Include/Exclude by function keys
- Named groups declared by modules, referenced by states

Keys should be public `const string` values; analyzers can enforce uniqueness per module.

## State Container and Hierarchy
- Each state has a dedicated container holding services of all included modules.
- Containers are immutable and chained: a child state’s container uses its parent state’s container as the DI parent. Child states may only add services; they do not remove services from ancestors.
- Facades are accessible to wrappers; public DI exposes only declared interfaces.

Composition flow:
- Registries define modules and states; GSM finalizes the current sets after registry runs.
- When entering a child state, GSM builds a new child state container whose parent is the current state container, using collected service factories and facade mappings required by wrappers.

## State Changes (Event Semantics)
We replace rigid phases with event‑driven semantics that still clarify container side and container chaining:

1) OnStateExit (old side) runs for the current leaf state. Functions resolve against the old container.

2) OnModuleExit (old side) runs for modules that will no longer be active after the change. Functions resolve against the old container.

3) Build the next state container by chaining on top of the current one (parent‑child DI). Child containers add services; ancestor services remain available.

4) OnModuleEnter (new side) runs for modules that become active. Functions resolve against the new container.

5) OnStateEnter (new side) runs for the new leaf state. Functions resolve against the new container.

Notes:
- Exit order reverses the enter order within the same scope (module or state).
- GSM performs state changes only when idle (not within the main loop tick).
- Ordering constraints (module-level and function-level) apply within each event set.

## Ordering
- Cross‑module ordering: modules may declare class‑level relationships, e.g., `OrderModuleBefore<TModule>()` / `OrderModuleAfter<TModule>()`.
- Function‑level ordering:
  - Same scope: `OrderBefore("fn")` / `OrderAfter("fn")`.
  - Cross scope (module or state): `OrderBefore<TDecl>("fn")` / `OrderAfter<TDecl>("fn")`.
- Exit ordering automatically reverses the enter ordering within the same scope.
- Cycles are errors; analyzers and runtime checks should report deterministically.

Note: Cross‑scope ordering referencing a module/state not active in the current configuration is ignored.

### Multi‑Hop Transitions
Transitions across non‑adjacent states traverse via the lowest common ancestor (LCA):

1) Ascend: From the current leaf up to (but excluding) the LCA, run `OnStateExit` (old side) and `OnModuleExit` (old side); within each scope execute in reverse entry order.
2) Descend: From the LCA down to the target (excluding LCA, including target), at each step:
   - Build a new child container whose parent is the container of the previous state on the path.
   - Run `OnModuleEnter` (new side) for modules becoming active at this step.
   - Run `OnStateEnter` (new side) for the entered state.
3) GSM performs state changes only when idle; only already‑registered states are valid targets.

Example (illustrative):
```csharp
public static class NetworkingModule
{
    public const string Connect = "connect";
    public const string Disconnect = "disconnect";

    [StateFunction(Connect)]
    [OnStateEnter]
    public static void ConnectServer(/* deps */) { /* ... */ }

    [StateFunction(Disconnect)]
    [OnStateExit]
    public static void DisconnectServer(/* deps */) { /* ... */ }
}

public static class EcsModule
{
    public const string Init = "ecs_init";

    [StateFunction(Init)]
    [OnModuleEnter]
    [OrderAfter<NetworkingModule>(NetworkingModule.Connect)]
    public static void InitializeWorld(/* deps */) { /* ... */ }
}
```

## Payloads (Overview)
State change requests accept an optional payload. Source-generated helpers can offer strongly-typed builders per state. Details are intentionally deferred; payload composition should be deterministic and validate at the edges.

## Determinism and Generation Rules
- No reflection in engine paths. Discovery and invocation use source generation and entrypoint infrastructure.
- Sort inputs and resolve ordering deterministically; cycles are errors.
- Generated code uses the configured `SgOutputNamespace` when provided.

## State Data (Planning)
Certain data belongs to the state rather than a service. State data is explicitly registered and resolved by wrappers for state functions.

Example (illustrative):
```csharp
public interface IStateData { }

public sealed class GameplayHudData : IStateData
{
    public bool IsVisible { get; set; }
}

// Wrappers inject accessors to state data
public interface IStateDataAccessor<T> where T : IStateData
{
    T Get();
}

public static class Hud
{
    [StateFunction("hud_enter")]
    [OnStateEnter]
    public static void Setup(IStateDataAccessor<GameplayHudData> data)
    {
        data.Get().IsVisible = true;
    }

    [StateFunction("hud_exit")]
    [OnStateExit]
    public static void Teardown(IStateDataAccessor<GameplayHudData> data)
    {
        data.Get().IsVisible = false;
    }
}
```

## Main Loop (Single-Threaded)
The main loop is a single channel and executes per-frame functions in a deterministic, single-threaded order.

- Functions may trigger their own concurrent work internally (e.g., start worker tasks). The scheduler remains single-threaded.
- Synchronization points are expressed via ordering on a completion function (e.g., `EcsComplete` runs after `EcsWorker` and may join internal work).

Example (illustrative):
```csharp
public static class EcsPerFrame
{
    [StateFunction("ecs_worker")]
    [PerFrame]
    public static void Worker(/* deps */) { /* trigger internal parallel work */ }

    [StateFunction("ecs_complete")]
    [PerFrame]
    [OrderAfter<EcsPerFrame>("ecs_worker")]
    public static void Complete(/* deps */) { /* join/wait internal work */ }
}
```

## Extensible Triggers & Ordering (v2)
Future versions may allow mods to “register” additional triggers or custom ordering/conditional rules via entrypoints. This remains an extension point and is not required for v1.

## Initial State Selection (Planning)
After the root state completes, an entrypoint provides the initial state `Identification`. Discovery is global; GSM filters and chooses exactly one (or errors). CLI override may be supported.

Example (illustrative):
```csharp
[InitialStateEntrypoint]
public static class InitialSelection
{
    public static void Select(InitialStateOut outParam)
    {
        // decide based on environment/args
        outParam.Value = /* Identification of the starting state */;
    }
}
```
