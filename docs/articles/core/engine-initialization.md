---
uid: articles.core.engine-initialization
---

# Engine Initialization

This document describes the initialization process of the Sparkitect engine, from application startup to the transition to the first game state.

## Overview

The engine initialization process follows these key steps:

1. Core IoC container creation
2. CLI argument processing
3. Root mod discovery and loading
4. Registry processing
5. Transition to the first game state

The `EngineBootstrapper` class manages this sequence, serving as the central coordination point for engine startup and shutdown.

## Initialization Sequence

### Core Container Creation

The initialization begins with the creation of a minimal Core IoC Container:

```csharp
public class EngineBootstrapper
{
    public void Initialize(string[] args)
    {
        BuildCoreContainer();
        InitializeCliArguments(args);
        LoadRootMods();
        ProcessRegistries();
        RunGame();
    }
    
    public void CleanUp()
    {
        // Clean up resources, close streams, etc.
    }
}
```

The Core Container contains only the essential services needed for engine initialization, primarily the ModLoader/Manager and CLI argument handler. These services are hardcoded in the engine since they are required before any mods can be loaded.

### CLI Argument Processing

Before loading mods, the engine processes command-line arguments:

1. Arguments are parsed and categorized
2. System configurations are applied based on arguments
3. Special flags (like debug mode) are activated if specified

This allows for runtime configuration of the engine before any mods are loaded.

### Mod Discovery and Loading

The mod loading process occurs in multiple steps:

1. **Archive Discovery**: The engine searches for mods in the "mods" folder.

2. **Dependency Resolution**: The engine checks mod dependencies defined in manifests and prepares stub implementations for optional dependencies if needed.

3. **Assembly Loading**: Mod assemblies are loaded directly from the zip streams, which remain open for the application's lifetime.

4. **Configuration Entrypoint Discovery**: The engine discovers all configuration entrypoints (classes marked with attributes like `[CoreContainerConfiguratorEntrypoint]`).

5. **Root Container Creation**: The discovered entrypoints are used to build the root-level IoC container.

```csharp
[CoreContainerConfiguratorEntrypoint]
public class MyConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(IContainer container)
    {
        container.Register<IService, Service>();
    }
}
```

### Registry Invocation

After mods are loaded, registries are invoked by unified state functions (enter/exit events) within the root/entry states rather than as a global pass. Typical flow:

1. State logic discovers and registers registry categories via `IRegistryConfigurator`.
2. State logic performs object registrations through `Registrations` classes under the current state’s DI context.
3. Corresponding exit functions explicitly unregister/cleanup objects tied to the leaving scope.

This provides deterministic, per‑state control of registry work while keeping category and identifier semantics unchanged.

### Transition to First Game State

After registry processing completes, the engine transitions control to the first game state. The game state management system determines which state to load initially and transfers control flow accordingly.

## Container Hierarchies

The engine uses a hierarchy of containers during initialization:

1. **Core Container**: Minimal services needed for bootstrapping
2. **Root Container**: Created after loading root mods, contains all services from root mods
3. **Registry Container**: Specialized container for registry processing
4. **State Container**: Built per state; child state containers use their parent state’s container as DI parent (children add services, parents remain unchanged)

Once created, containers cannot be modified. Subsequent operations that need additional services create child containers rather than modifying existing ones.

## Clean-Up Process

The engine performs clean-up operations when shutting down:

1. Game states are properly terminated
2. Mod resources are released
3. Zip streams are closed
4. Other system resources are freed

The `CleanUp` method in the `EngineBootstrapper` class handles these operations.
