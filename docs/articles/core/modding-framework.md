---
uid: articles.core.modding-framework
---

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

4. Configuration entrypoints are discovered and processed:
   - Classes marked with discovery attributes are located
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

- **Version Requirements**:
  - Semantic versioning support for dependencies
  - Specify minimum, maximum, or exact versions
  - Optional version range specifications

- **Optional Dependency Handling**:
  - Stub implementations provided for optional dependencies
  - Allows mods to load even when direct code access to optional dependencies would normally cause exceptions
  - Additional mechanisms streamline working with optional dependencies

### Mod Groups and Loading Order

Mods can be organized into logical groups for more controlled loading:

- Groups define logical collections of mods
- Each group can have its own loading sequence
- The ModManager provides `LoadedModsPerGroup` to track the loading hierarchy
- Components can use this group-based order for their own processing requirements

## Identification System

The modding framework includes a hierarchical identification system:

- **Mod Identifiers**: Unique string IDs mapped to efficient numeric IDs
- **Categories**: Logical groupings of similar objects
- **Objects**: Individual items within categories

The `IdentificationManager` provides bidirectional mapping between string identifiers and numeric IDs, optimizing runtime performance while maintaining human-readable references.

## Integration with Core Systems

The Modding Framework is tightly integrated with other core systems:

- **Dependency Injection**:
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

- **Registry System**:
  ```csharp
  [RegistrationsEntrypoint]
  public class MyRegistrations : Registrations<MyRegistry>
  {
      private readonly IIdentificationManager _identificationManager;
      
      public MyRegistrations(IIdentificationManager identificationManager)
      {
          _identificationManager = identificationManager;
      }
      
      public override string CategoryIdentifier => "category_name";
      
      public override void MainPhaseRegistration(MyRegistry registry)
      {
          var id = _identificationManager.RegisterObject("my_mod", "category_name", "my_object");
          registry.RegisterSomething(id, "additional data");
      }
  }
  ```

- **Lifecycle Management**: Object lifecycle is directly tied to mod lifecycle