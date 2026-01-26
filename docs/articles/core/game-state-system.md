---
uid: articles.core.game-state-system
title: Game State System
description: Hierarchical state management with module-based composition
---

# Game State System

The Game State System manages the engine's runtime configuration through a hierarchical state machine. States are composed from modules, and both states and modules define behavior through static functions.

## Core Concepts

### States

A **state** represents a specific runtime configuration of the engine. States form a parent-child hierarchy (tree structure) where each state inherits the modules and configuration of its parent.

States don't contain logic directly - they declare which modules they include and optionally define transition-specific functions.

**Key characteristics:**
- Parent-child hierarchy (each state has exactly one parent, except Root)
- Composed from modules
- Transitions happen only between parent and immediate child
- Each active state has its own DI container (child of parent's container)

### Modules

A **module** is a reusable unit of functionality that can be included in multiple states. Modules contain the actual logic - state functions, service definitions, and dependencies on other modules.

**Key characteristics:**
- Declare dependencies on other modules
- Define state functions (lifecycle hooks and frame logic)
- Can define services scoped to that module
- Activated when included in the current state path

### State Functions

**State functions** are static methods that define behavior in modules and states. They use the [Stateless Function](stateless-functions.md) system for attribute-based scheduling, dependency injection, and execution ordering.

See [Stateless Functions](stateless-functions.md) for the complete attribute reference.

## Defining Modules

Modules implement `IStateModule` and are marked with a registration attribute:

```csharp
[ModuleRegistry.RegisterModule("my_module")]
public partial class MyGameModule : IStateModule
{
    // Module identification
    public static Identification Identification => StateModuleID.MyMod.MyModule;

    // Dependencies on other modules
    public static IReadOnlyList<Identification> RequiredModules => [
        StateModuleID.Sparkitect.Core
    ];

    // Stateless functions defined below
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

**Module Registration:**
- `[ModuleRegistry.RegisterModule("key")]` marks the module for registration
- The string key is used to generate the module's `Identification`
- The class must be `partial` when defining state functions, as source generators extend the class with method wrappers
- `Identification` is typically defined in generated ID extension files

**Module Dependencies:**
- `RequiredModules` lists other modules this module depends on
- The system validates that all dependencies are present when the module is activated
- Dependencies are checked at finalization time after module registration

## Defining States

States implement `IStateDescriptor` and are marked with a registration attribute:

```csharp
[StateRegistry.RegisterState("game_menu")]
public partial class GameMenuState : IStateDescriptor
{
    // State identification
    public static Identification Identification => StateID.MyMod.GameMenu;

    // Parent state
    public static Identification ParentId => StateID.Sparkitect.Root;

    // Modules included in this state
    public static IReadOnlyList<Identification> Modules => [
        StateModuleID.MyMod.MenuModule,
        StateModuleID.MyMod.UiModule
    ];

    // Optional: state-specific functions
    [TransitionFunction("enter_menu")]
    [OnFrameEnterScheduling]
    public static void EnterMenu(IMenuService menu)
    {
        // Runs when this state becomes active
    }
}
```

**State Hierarchy:**
- Every state has exactly one parent (except Root)
- `ParentId` defines the parent-child relationship
- States can only transition to their immediate parent or immediate children
- The Root state (`StateID.Sparkitect.Root`) is the hierarchy root but is never active

**Module Composition:**
- `Modules` lists the modules this state introduces (delta from parent)
- States automatically inherit all modules from their parent chain
- You only list the new modules this state adds, not inherited ones

**State-Specific Functions:**
- States can optionally define their own state functions
- These are typically used for transition-specific logic
- Most logic should be in modules for reusability

## State Functions

State functions use the [Stateless Function](stateless-functions.md) system. Each function requires a function attribute and a scheduling attribute.

```csharp
[PerFrameFunction("process_input")]
[PerFrameScheduling]
public static void ProcessInput(IInputService input, IPhysicsService physics)
{
    var commands = input.GetPlayerCommands();
    physics.ApplyCommands(commands);
}
```

For details on dependency injection, ordering attributes, and scheduling types, see [Stateless Functions](stateless-functions.md).

### Lifecycle Sequence

When transitioning between states, functions execute in this order:

**Transition to Parent** (popping child state):
1. Child state: `[OnFrameExit]` functions
2. Child module(s): `[OnDestroy]` functions (reverse of creation order)
3. Parent state: `[OnFrameEnter]` functions

**Transition to Child** (pushing child state):
1. Parent state: `[OnFrameExit]` functions
2. Child module(s): `[OnCreate]` functions
3. Child state: `[OnFrameEnter]` functions

Within each category, ordering constraints determine execution order.

## Module Services

Modules can define services scoped to that module using the `[StateService]` attribute:

```csharp
[StateService<IPhysicsService, PhysicsModule>]
public class PhysicsService : IPhysicsService
{
    public PhysicsService(ILogger logger)
    {
        // Constructor dependencies injected
    }

    // Service implementation
}
```

These services:
- Are created when the module is activated
- Are available to state functions in that module
- Are destroyed when the module is deactivated
- Are registered automatically via source-generated configurators (marked `[CompilerGenerated]`)

See [Dependency Injection](dependency-injection.md) for more details on service registration.

## State Transitions

States transition through the `IGameStateManager` service:

```csharp
[PerFrameFunction("handle_input")]
[PerFrameScheduling]
public static void HandleInput(IInputService input, IGameStateManager stateManager)
{
    if (input.IsMenuRequested())
    {
        // Request transition to menu state (must be parent or child)
        stateManager.Request(StateID.MyMod.MenuState);
    }
}
```

**Transition Rules:**
- Can only transition to immediate parent or immediate child
- Transitions are queued and execute between frames (not during frame execution)
- Cannot transition to Root state (Root is never active)
- Attempting invalid transitions throws an exception

**Transition with Mod Loading:**

For transitions that require loading additional mods:

```csharp
stateManager.RequestWithModChange(
    () => StateID.MyMod.MultiplayerState,  // Target state (must be child)
    ["multiplayer_content_mod"],            // Mods to load
    payloadData                             // Optional payload
);
```

This loads the specified mods, processes their registrations, then transitions to the child state.

## Main Loop

The main loop executes while a state is active:

1. Check for pending transitions → execute if present
2. Execute all `[PerFrame]` functions from the current state and its modules
3. Check for pending transitions → execute if present
4. Repeat until shutdown

Only the **leaf state** (bottom of the stack) has its `[PerFrame]` functions executed. Parent states remain on the stack but don't execute per-frame logic.

## Best Practices

### Module Design

- Keep modules focused on a single domain (physics, rendering, UI, etc.)
- Declare explicit dependencies between modules
- Put reusable logic in modules, transition logic in states
- Use services for shared state, state functions for behavior

### State Function Design

- Keep functions small and focused
- Use ordering only when necessary (prefer independent functions)
- Prefer constructor injection for services over property injection
- State functions are static - no instance state allowed

### State Hierarchy Design

- Shallow hierarchies are easier to reason about
- Group related states under common parents
- Use parent states for shared module sets
- Remember: you can only transition to immediate relatives

### Error Handling

- Missing dependencies are caught at state creation time, not during execution
- Invalid transitions throw exceptions immediately
- Circular module dependencies are detected at finalization

## Integration with Other Systems

The Game State System integrates with:

- **Dependency Injection**: State containers are children of Root container ([details](dependency-injection.md))
- **Registry System**: Registries are typically added/processed in `[OnCreate]` functions ([details](registry-system.md))
- **Modding System**: States and modules are discovered across loaded mods ([details](modding-framework.md))

## Next Steps

- Review sample implementations in the `samples/` directory
- See [Dependency Injection](dependency-injection.md) for service patterns
- See [Registry System](registry-system.md) for object registration patterns
