---
uid: sparkitect.core.game-state-system
title: Game State System
description: Hierarchical state management with module-based composition
---

# Game State System

The Game State System manages the engine's runtime configuration through a hierarchical state machine. States are composed from modules, and both states and modules define behavior through [stateless functions](xref:sparkitect.core.stateless-functions).

## Defining Modules

Modules are reusable units of functionality. A module declares its dependencies, defines services, and contains the stateless functions that implement behavior. Modules implement [`IStateModule`](xref:Sparkitect.GameState.IStateModule) and are registered through [`ModuleRegistry`](xref:Sparkitect.GameState.ModuleRegistry):

```csharp
[ModuleRegistry.RegisterModule("my_module")]
public sealed partial class MyGameModule : IStateModule
{
    public static Identification Identification => StateModuleID.MyMod.MyModule;

    public static IReadOnlyList<Identification> RequiredModules => [
        StateModuleID.Sparkitect.Core
    ];

    [TransitionFunction("initialize")]
    [OnCreateScheduling]
    private static void Initialize(IMyService service)
    {
        // Runs once when this module is created
    }

    [PerFrameFunction("update")]
    [PerFrameScheduling]
    private static void Update(IMyService service)
    {
        // Runs every frame
    }

    [TransitionFunction("cleanup")]
    [OnDestroyScheduling]
    private static void Cleanup(IMyService service)
    {
        // Runs once when this module is destroyed
    }
}
```

The `[ModuleRegistry.RegisterModule("key")]` attribute marks the module for registration. The string key generates the module's [`Identification`](xref:Sparkitect.Modding.Identification). The class must be `partial` when defining stateless functions, since source generators extend it with method wrappers.

`RequiredModules` lists other modules this module depends on. The system validates that all dependencies are present at finalization time, after module registration.

## Defining States

A state represents a specific runtime configuration. States form a parent-child hierarchy where each state inherits modules from its parent chain. States implement [`IStateDescriptor`](xref:Sparkitect.GameState.IStateDescriptor) and are registered through [`StateRegistry`](xref:Sparkitect.GameState.StateRegistry):

```csharp
[StateRegistry.RegisterState("game_menu")]
public partial class GameMenuState : IStateDescriptor
{
    public static Identification Identification => StateID.MyMod.GameMenu;
    public static Identification ParentId => StateID.Sparkitect.Root;

    // Only list modules this state adds (not inherited ones)
    public static IReadOnlyList<Identification> Modules => [
        StateModuleID.MyMod.MenuModule,
        StateModuleID.MyMod.UiModule
    ];

    // States can optionally define their own functions
    [TransitionFunction("enter_menu")]
    [OnFrameEnterScheduling]
    public static void EnterMenu(IMenuService menu)
    {
        // Runs when this state becomes active
    }
}
```

Every state has exactly one parent (except Root). `Modules` lists only the modules this state introduces as a delta from its parent; inherited modules are included automatically.

The Root state (`StateID.Sparkitect.Root`) sits at the top of the hierarchy but is never an active frame. It provides the `CoreModule`, which all states inherit. Transitions to Root are forbidden.

States can define their own stateless functions, typically for transition-specific logic. Most behavior should live in modules for reusability.

## Module Services

Modules define scoped services using [`[StateService]`](xref:Sparkitect.GameState.StateServiceAttribute`2):

```csharp
[StateService<IPhysicsService, PhysicsModule>]
public class PhysicsService : IPhysicsService
{
    public PhysicsService(ILogger logger)
    {
        // Constructor dependencies injected
    }
}
```

These services are created when the module is activated, available to stateless functions in that module, and destroyed when the module is deactivated. Registration is automatic through source-generated configurators. See [Dependency Injection](xref:sparkitect.core.dependency-injection) for service lifetime details.

## Entry State Selection

After engine initialization, the system needs to know which state to enter first. One mod in the active root mod set provides this through [`IEntryStateSelector`](xref:Sparkitect.GameState.IEntryStateSelector). Most mods never need to implement this; it is analogous to the "game project" in a classic engine that decides what launches on startup.

```csharp
[EntryStateSelectorEntrypoint]
public class EntryStateSelector : IEntryStateSelector
{
    public Identification SelectEntryState(ICoreContainer container)
    {
        return StateID.MyMod.MainMenu;
    }
}
```

The [`[EntryStateSelectorEntrypoint]`](xref:Sparkitect.GameState.EntryStateSelectorEntrypointAttribute) attribute marks the class for automatic discovery. `SelectEntryState` receives the root DI container and returns the first state to activate. The returned state cannot be Root.

## State Transitions

States transition through [`IGameStateManager`](xref:Sparkitect.GameState.IGameStateManager):

```csharp
[PerFrameFunction("check_menu_request")]
[PerFrameScheduling]
public static void CheckMenuRequest(IMenuService menu, IGameStateManager stateManager)
{
    if (menu.IsMenuRequested())
    {
        stateManager.Request(StateID.MyMod.MenuState);
    }
}
```

`Request` queues a transition that executes between frames. Only immediate parent or child transitions are valid; attempting anything else throws an exception. Root cannot be a transition target.

### Transition with Mod Loading

[`RequestWithModChange`](xref:Sparkitect.GameState.IGameStateManager.RequestWithModChange(System.Func{Sparkitect.Modding.Identification},System.Collections.Generic.IReadOnlyList{Sparkitect.Modding.ModFileIdentifier},System.Object)) loads additional mods and then transitions to a child state. Unlike `Request`, this executes synchronously (the transition completes before the call returns):

```csharp
stateManager.RequestWithModChange(
    () => StateID.MyMod.MultiplayerState,  // Called after mods load
    [new ModFileIdentifier("multiplayer_content_mod", SemVersion.Parse("1.0.0"))],
    payloadData                             // Optional payload
);
```

The first parameter is a `Func<Identification>` because the target state may come from a mod that hasn't loaded yet. Each [`ModFileIdentifier`](xref:Sparkitect.Modding.ModFileIdentifier) contains an `Id` (string) and `Version` (`SemVersion`) for unambiguous mod selection.

### Lifecycle and Transition Execution

When transitioning between states, all eligible transition functions are resolved into a single topologically-sorted execution graph. The scheduling attribute determines which functions are eligible (included in the graph). Execution order is controlled by [`[OrderBefore<T>]`](xref:Sparkitect.Stateless.OrderBeforeAttribute`1) and [`[OrderAfter<T>]`](xref:Sparkitect.Stateless.OrderAfterAttribute`1) constraints.

**Transition to child** (pushing):

- [`[OnCreateScheduling]`](xref:Sparkitect.GameState.OnCreateSchedulingAttribute): included if the owner module is in the delta modules being added, or the owner is the target state
- [`[OnFrameEnterScheduling]`](xref:Sparkitect.GameState.OnFrameEnterSchedulingAttribute): included if the owner module is loaded in the current state stack, or the owner is the target state

**Transition to parent** (popping):

- [`[OnFrameExitScheduling]`](xref:Sparkitect.GameState.OnFrameExitSchedulingAttribute): included if the owner module is loaded in the current state stack, or the owner is the target state
- [`[OnDestroyScheduling]`](xref:Sparkitect.GameState.OnDestroySchedulingAttribute): included if the owner module is in the delta modules being removed, or the owner is the target state

All eligible functions from both scheduling types are combined into one graph, sorted, and executed together. An `[OnCreateScheduling]` function can be ordered after an `[OnFrameEnterScheduling]` function when both are eligible; they coexist in the same execution graph.

## Main Loop

The main loop runs while a state is active:

1. Check for pending transitions, execute if present
2. Execute all [`[PerFrameScheduling]`](xref:Sparkitect.GameState.PerFrameSchedulingAttribute) functions
3. Check for pending transitions, execute if present
4. Repeat until shutdown

Only the leaf state (bottom of the stack) has its per-frame functions executed. Parent states remain on the stack but do not run per-frame logic.

## See Also

- [Stateless Functions](xref:sparkitect.core.stateless-functions) for the generic function infrastructure, ordering attributes, and scheduling details
- [Dependency Injection](xref:sparkitect.core.dependency-injection) for service registration and container hierarchy
- [Registry System](xref:sparkitect.core.registry-system) for object registration patterns
