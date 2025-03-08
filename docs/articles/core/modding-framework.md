# Modding Framework

The Modding Framework is the cornerstone of Sparkitect's architecture. Unlike traditional game engines where modding is an afterthought, Sparkitect builds its entire structure around the concept of mods.

## Core Concepts

### Mod Types

Sparkitect distinguishes between two primary types of mods:

- **Root Mods**: Loaded at application startup and capable of directly influencing the engine. The engine itself functions partially as a "virtual root mod," creating a unified system where engine and mods are treated equally.

- **Game Mods**: Loaded when joining or creating a game session and unloaded when the game ends. These typically implement game-specific functionality.

### Mod Structure

Each mod is distributed as a mod archive containing:

- Mod DLL(s)
- Metadata manifest
- Dependency definitions
- Additional resources

### Mod Discovery

Mods are discovered through a simple directory structure:

- Mods are placed in a standard `mods` folder
- The engine searches this folder for mod archives with the appropriate extension

## Lifecycle Management

### Loading Process

1. Application initializes the core DI container with minimal components
2. The `EngineBootstrapper` discovers and loads Root Mods:
  - Archives are located and extracted in memory
  - Mod assemblies are loaded directly from zip streams
  - Zip streams remain open for the application's lifetime

3. The `ModManager` performs dependency resolution:
  - Checks dependencies defined in manifests
  - Prepares stub implementations for optional dependencies
  - Determines loading order based on dependencies

4. IoC entrypoints are discovered and processed:
  - Classes marked with `[IoCBuilderEntrypoint]` are located
  - These are used to build the root-level IoC container

5. Game-specific mods are loaded when creating/joining a game
  - Similar process to Root Mod loading
  - Game-specific containers are created

6. When exiting a game, Game Mods are unloaded while Root Mods remain active

### Mod Dependencies

Dependencies between mods are managed through the mod manifest:

- **Relationship Types**:
  - Required dependencies: Mod will not load without these dependencies
  - Optional dependencies: Mod can function without these but will use them if present
  - Incompatible mods: Mod will not load if these are present

- **Optional Dependency Handling**:
  - Stub implementations provided for optional dependencies
  - Allows mods to load even when direct code access to optional dependencies would normally cause exceptions
  - Additional mechanisms streamline working with optional dependencies

### Loading Order

While there is no general loading order for all mods, the Mod Manager provides access to a dependency-ordered list of mods. Individual engine components can use this order for their own processing requirements when appropriate.

## Integration with Core Systems

The Modding Framework is tightly integrated with other core systems:

- **Dependency Injection**:
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

- **Registry System**:
  ```csharp
  [RegistrationsEntrypoint]
  public class MyRegistrationLogic : IRegistrations
  {
      private readonly RegistryManager _registryManager;
      private readonly MyRegistry _myRegistry;
      
      public MyRegistrationLogic(RegistryManager registryManager, MyRegistry myRegistry)
      {
          _registryManager = registryManager;
          _myRegistry = myRegistry;
      }
      
      public void MainPhaseRegistration()
      {
          var numericId = _registryManager.Register(modId, categoryId, objectId);
          _myRegistry.Register<SomeType>(numericId);
      }
  }
  ```

- **Lifecycle Management**: Object lifecycle is directly tied to mod lifecycle

## Future Development

Version compatibility management is planned for future implementation, likely using a Major.Minor.Patch version system.