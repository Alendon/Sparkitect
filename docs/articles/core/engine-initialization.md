---
uid: articles.core.engine-initialization
---

# Engine Initialization

This document describes the initialization process of the Sparkitect engine, from application startup to the transition to the first game state.

## Overview

The engine initialization process follows these key steps:

1. Core IoC container creation
2. Root mod discovery and loading
3. Registry processing for root mods
4. Transition to the first game state

The `EngineBootstrapper` class manages this sequence, serving as the central coordination point for engine startup and shutdown.

## Initialization Sequence

### Core Container Creation

The initialization begins with the creation of a minimal Core IoC Container:

```csharp
public class EngineBootstrapper
{
    public static void Main(string[] args)
    {
        var bootstrapper = new EngineBootstrapper();
        bootstrapper.BuildCoreContainer();
        bootstrapper.LoadRootMods();
        bootstrapper.RunGame();
        
        bootstrapper.CleanUp();
    }
}
```

The Core Container contains only the essential services needed for engine initialization, primarily the ModLoader/Manager. These services are hardcoded in the engine since they are required before any mods can be loaded.

### Mod Discovery and Loading

The mod loading process occurs in multiple steps:

1. **Archive Discovery**: The engine searches for mods in the "mods" folder. Mods are distributed as uncompressed zip archives with a special extension.

2. **Dependency Resolution**: The engine checks mod dependencies defined in manifests and prepares stub implementations for optional dependencies if needed.

3. **Assembly Loading**: Mod assemblies are loaded directly from the zip streams, which remain open for the application's lifetime.

4. **Entrypoint Discovery**: The engine discovers all IoC extension points (classes marked with attributes like `[IoCBuilderEntrypoint]`).

5. **Root Container Creation**: The discovered entrypoints are used to build the root-level IoC container.

```csharp
[IoCBuilderEntrypoint]
public class MyIoCBuilder : IIoCBuilder
{
    public void ConfigureIoc(Container container)
    {
        container.Register<IService, Service>();
    }
}
```

### Registry Processing

Once mods are loaded, the Registry Manager initiates registry processing:

1. The Registry Manager creates a new child container based on all `RegistryEntrypoints`.
2. This container includes all individual Registry classes provided by mods.
3. The registration code accesses this container to perform registrations.

```csharp
[IoCRegistryBuilderEntrypoint]
public class MyIoCBuilder : IIoCRegistryBuilder
{
    public void ConfigureRegistries(Container container)
    {
        container.Register<MyRegistry, MyRegistry>();
    }
}

[RegistrationsEntrypoint]
public class MyRegistrationLogic(RegistryManager registryManager, MyRegistry myRegistry) : IRegistrations
{
    public void MainPhaseRegistration()
    {
        var numericId = registryManager.Register(modId, categoryId, objectId);
        myRegistry.Register<SomeType>(numericId);
    }
}
```

The registration process:
1. Registers a new ID for the object with the central registry facility
2. Associates relevant file names with the ID
3. Calls the actual Registry class with the ID and required parameters

### Transition to First Game State

After registry processing completes, the engine transitions control to the first game state. The game state management system determines which state to load initially and transfers control flow accordingly.

## Container Hierarchies

The engine uses a hierarchy of containers during initialization:

1. **Core Container**: Minimal services needed for bootstrapping
2. **Root Container**: Created after loading root mods, contains all services from root mods
3. **Registry Container**: Specialized container for registry processing

Once created, containers cannot be modified. Subsequent operations that need additional services create child containers rather than modifying existing ones.

## Clean-Up Process

The engine performs clean-up operations when shutting down:

1. Game states are properly terminated
2. Mod resources are released
3. Zip streams are closed
4. Other system resources are freed

The `CleanUp` method in the `EngineBootstrapper` class handles these operations.