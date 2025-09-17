# Game State Planning

This document describes the planned Game State System for the Sparkitect Engine.

## Goal
Provide a clean, reliable, and extensible mechanism to define engine/game states with distinct configurations and services, and to manage deterministic transitions between them.

## Core Concepts
- State: Registered by the Registry System (category: "State"). A State has a string id (mapped to an Identification) and exactly one parent. The bootstrap/root state is handled outside of the Registry System.
- State Module: Registered by the Registry System (category: "State Bundle"). A State is defined as an ordered composition of State Modules plus a parent reference.
- Component ownership: Features and Transitions are owned by the State Module and activated per State; they are not shared across states by default.
- Registry integration: Registries are stateless proxy classes. Registry containers/instances are built as needed during processing and then discarded. Category lifetime is bound to the owning module; numeric IDs are ephemeral and may be reassigned when modules reappear.

## Atomic Components
The following are the atomic elements the system orchestrates. States compose them via State Modules.

### State Bound Service
A State Bound Service is a DI service instantiated only when the State that includes its defining Module is active. These are singletons within the active State’s scope.

- The service class is decorated as a state-bound service and can expose one optional Facade interface for Features/Transitions.
- Services are added to a specific State Module explicitly by code in that module (a list of used service API types). Both the service decoration and the module usage must agree; the generator validates this.
- Public DI exposes only the "exposed" service interfaces. Facades are available only to generated wrappers for Features/Transitions.
- A dedicated source generator and a derived StateContainer builder handle multi-interface mapping so exposed interface and facade resolve to the same instance.

Sample (shape indicative):
```csharp
// Service class declares it belongs to a specific state module and exposes an optional facade
[StateService<CoreModule, IMyServiceExposed>(Facade = typeof(IMyServiceFacade))]
internal sealed class MyService : IMyServiceExposed, IMyServiceFacade
{
    // Implementation
}

// The module explicitly references the exposed API type as “used”
[StateModule("core")]
internal sealed partial class CoreModule
{
    public static readonly Type[] ExposedServices =
    {
        typeof(IMyServiceExposed)
    };
}
```

### Feature
Features implement the state’s main loop work. They are stateless and sync (no async in the first iteration). All resolution/invocation boilerplate is generated; no reflection is used.

- Declared as static methods, typically inside the State Module class. Annotated with a key and optional ordering.
- Generated wrappers resolve dependencies once per container change and then call the method each frame.
- Missing non-nullable DI parameters are hard errors; nullable parameters are treated as optional.

```csharp
[Feature("sample")]
internal static void Tick(IMyServiceExposed api, IMyServiceFacade? facade)
{
    api.DoSomethingPublic();
    facade?.DoFeatureRelatedWork();
}
```

### Transition
Transitions execute around state changes. They are static methods owned by a State Module and keyed like features. Triggers (first iteration):

- Removed: Module present in old state, not in new
- Unchanged.Before: Module present in both states (before rebuild)
- Unchanged.After: Module present in both states (after rebuild)
- Add: Module present in new state, not in old

Transitions can access the relevant side’s container; unchanged transitions split to allow coordinated teardown/build-up. All wrappers are generated; no reflection is used.

## Component Activation per State
By default, when a State includes a State Module, all of that module’s Features and Transitions are active. States can refine this by specifying an activation policy per module:

- All (default) – activate all components
- Include(keys)/Exclude(keys) – select components by keys
- Groups – modules may declare named groups of feature/transition keys, and states can reference groups

Keys must be public const string values; analyzers enforce key validity and uniqueness within a module and trigger group. Activation applies per trigger group as well (Removed, Unchanged.Before, Unchanged.After, Add).

This allows shared modules across very different states while tailoring behavior without duplicating modules.

## State Container and Hierarchy
- A State has a single container that holds all services of all included modules for that State.
- The DI system remains immutable; container changes happen only at state boundaries.
- Facades are only accessible to generated wrappers for features/transitions; public DI exposes only exposed interfaces.

Example hierarchy concept:
```
- RootState (Core Engine Components)
    - DesktopGameState (Core Engine Components, Rendering Module)
        - MainMenuState (… + Main Menu Module)
        - LocalGame (… + Game Module)
        - ClientGame (… + Game Module + Networking Module)
    - ServerGame (Core Engine Components + Game Module + Networking Module)
```

## Transition Sequence (A → B)
1. Compute module diff:
   - Removed = A\B; Unchanged = A∩B; Added = B\A
2. Determine cross-module and intra-module ordering. Ordering applies per trigger group: Removed, Unchanged.Before, Unchanged.After, Add
3. Phase 1 (old):
   - Run Removed transitions on Removed modules against the old container (restricted to old-state active keys)
   - Run Unchanged.Before transitions on Unchanged modules against the old container (restricted to old-state active keys)
4. Build:
   - Perform required registry/mod operations via Core State Module transitions
   - Build the new State container (Unchanged + Added modules)
5. Phase 2 (new):
   - Run Add transitions on Added modules against the new container (restricted to new-state active keys)
   - Run Unchanged.After transitions on Unchanged modules against the new container (restricted to new-state active keys)

All failures (including ordering cycles, missing dependencies, transition exceptions) panic.

## Ordering
- Cross-module ordering: Modules may declare BeforeModule/AfterModule relationships
- Intra-module ordering: Individual features/transitions may declare Before/After key relationships
- Per-trigger ordering: the trigger groups are ordered independently using the above rules
- Keys must be public const string; analyzers validate constraints

## Payloads (Overview)
State changes are requested via a generic API and extended with source-generated, strongly-typed helpers per state using C# 14 extensions. Payloads are composed from module-defined pieces keyed by string. Details will be finalized with the implementation.

## Determinism and Generation Rules
- No reflection in engine paths. All discovery and invocation uses source generation and entrypoint infrastructure
- Sort inputs and resolve ordering deterministically; cycles result in panic
- Generated code places output in the configured `SgOutputNamespace` when provided

