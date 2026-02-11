---
uid: sparkitect.core.modding-framework
title: Modding Framework
description: Mod structure, loading, lifecycle management, and the identification system
---

# Modding Framework

The Modding Framework is the cornerstone of Sparkitect's architecture. Unlike traditional game engines where modding is an afterthought, Sparkitect builds its entire structure around the concept of mods.

## Core Concepts

### Mod Types

Sparkitect's design envisions two primary types of mods:

- **Root Mods**: Intended to be loaded at application startup, capable of directly influencing the engine. The engine itself functions as a "virtual root mod," creating a unified system where engine and mods are treated equally.

- **Game Mods**: Planned to be loaded when joining or creating a game session and unloaded when the game ends, for game-specific functionality.

**Current Status:** The distinction between Root and Game mods is conceptual and planned for future implementation. Currently, all mods are treated uniformly. The ModManifest structure does not yet differentiate mod types.

### Mod Structure

Each mod is distributed as a mod archive containing:

- Mod DLL(s)
- Metadata manifest
- Dependency definitions
- Additional resources

## SDK Project Configuration

Mod projects reference the Sparkitect SDK and configure mod metadata through MSBuild properties in the `.csproj` file:

```xml
<Project Sdk="Sparkitect.Sdk/0.1.0">
  <PropertyGroup>
    <ModName>My Awesome Mod</ModName>
    <ModId>mycompany.mymod</ModId>
    <ModVersion>1.0.0</ModVersion>
    <ModAuthor>Your Name</ModAuthor>
    <ModDescription>A brief description of the mod</ModDescription>
    <IsRootMod>true</IsRootMod>
  </PropertyGroup>
</Project>
```

### SDK Properties Reference

| Property | Required | Description |
|----------|----------|-------------|
| `ModName` | Yes | Display name shown to users |
| `ModId` | Yes | Unique identifier (e.g., `company.modname`) |
| `ModVersion` | Yes | Semantic version (e.g., `1.0.0`) |
| `ModAuthor` | No | Author or team name |
| `ModDescription` | No | Short description of the mod |
| `IsRootMod` | No | Whether this is a root mod (default: `false`) |
| `ModAutoDetectDependencies` | No | Auto-include external dependencies (default: `true`) |
| `DisableLogEnrichmentGenerator` | No | Disable Serilog log enrichment (default: `false`) |

The SDK automatically:
- Builds mod archives with the correct structure
- Includes the mod manifest with metadata
- Runs source generators for DI, registries, and state functions

### Mod Discovery

Mods are discovered through a simple directory structure:

- Mods are placed in a standard `mods` folder
- The engine searches this folder for mod archives with the `.sparkmod` extension

## Lifecycle Management

### Loading Process

1. Application initializes the core DI container with minimal components
2. The `EngineBootstrapper` discovers and loads Root Mods:
   - Archives are located and extracted in memory
   - Mod assemblies are loaded from memory after extraction from zip archives
   - Zip archives remain open for resource access during the application's lifetime

3. The `ModManager` performs dependency resolution:
   - Checks dependencies defined in manifests
   - Optional dependencies are handled through CLR lazy loading isolation (see [Optional Dependencies](xref:sparkitect.core.optional-dependencies))
   - Determines loading order based on dependencies

4. Configuration entrypoints are discovered and processed:
   - Classes marked with discovery attributes are located
   - These are used to build the root-level IoC container

5. *(Planned)* Game-specific mods are loaded when creating/joining a game
   - Similar process to Root Mod loading
   - Game-specific containers are created

6. *(Planned)* When exiting a game, Game Mods are unloaded while Root Mods remain active

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
  - Optional dependencies use CLR lazy loading isolation to safely reference types from mods that may not be present
  - The optional dependency analyzer enforces correct usage patterns at compile time
  - See [Optional Dependencies](xref:sparkitect.core.optional-dependencies) for patterns and best practices

### Mod Groups and Loading Order

Mods can be organized into logical groups for more controlled loading:

- Groups define logical collections of mods
- Each group can have its own loading sequence
- The ModManager provides `LoadedModsPerGroup` to track the loading hierarchy
- Components can use this group-based order for their own processing requirements

## Identification System

The modding framework includes a hierarchical identification system for tracking objects across mods.

### IIdentificationManager Interface

The `IIdentificationManager` service provides registration and lookup of identifications:

```csharp
// Inject via DI
public MyService(IIdentificationManager identManager)
{
    _identManager = identManager;
}
```

### Registration

Register mods, categories, and objects to obtain numeric IDs:

```csharp
// Register a mod (returns numeric mod ID)
ushort modId = identManager.RegisterMod("my_mod");

// Register a category (returns numeric category ID)
ushort categoryId = identManager.RegisterCategory("items");

// Register an object (returns full Identification struct)
Identification id = identManager.RegisterObject("my_mod", "items", "iron_sword");
```

> [!NOTE]
> The `modId` and `categoryId` parameters accept `OneOf<string, ushort>`, meaning you can pass either string identifiers or numeric IDs. String arguments work through implicit conversion.

**Registration flow:**
1. Mods are registered automatically during mod loading
2. Categories are registered when registries are added
3. Objects are registered during registry processing

### Lookup

Resolve string identifiers to IDs and vice versa:

```csharp
// String to Identification
if (identManager.TryGetObjectId("my_mod", "items", "iron_sword", out Identification id))
{
    // Use id...
}

// Identification to strings
if (identManager.TryResolveIdentification(id, out string? mod, out string? category, out string? objectKey))
{
    Console.WriteLine($"{mod}:{category}:{objectKey}");  // my_mod:items:iron_sword
}

// Check if mod/category exists
bool modExists = identManager.TryGetModId("my_mod", out ushort modId);
bool catExists = identManager.TryGetCategoryId("items", out ushort catId);
```

### Identification Struct

The `Identification` struct contains the numeric IDs (8 bytes total):

```csharp
public readonly struct Identification
{
    public readonly ushort ModId;      // Numeric identifier for the source mod
    public readonly ushort CategoryId; // Numeric identifier for the registry category
    public readonly uint ItemId;       // Numeric identifier for the specific object
}
```

Numeric IDs are compact and efficient for runtime comparisons, while string identifiers remain available for debugging and serialization.

### When to Use IIdentificationManager

| Scenario | Approach |
|----------|----------|
| Registering objects via [RegistryMethod] | Automatic - ID passed to your method |
| Looking up objects by string ID | Use TryGetObjectId |
| Debug logging of IDs | Use TryResolveIdentification |
| Custom registration logic | Use RegisterObject directly |

For most mod development, you'll interact with Identification through registry attributes (see [Registry System](xref:sparkitect.core.registry-system)). Direct IIdentificationManager usage is needed for advanced scenarios like dynamic registration or ID lookup.

## Generated Identification Pattern

The SDK generates strongly-typed identification classes for compile-time safety. Instead of using string literals, reference generated static properties:

### Pattern: `{Category}ID.{ModName}.{Object}`

```csharp
// Generated by SDK based on registrations
public static class StateModuleID
{
    public static class MyMod
    {
        public static Identification CoreModule => ...;
        public static Identification PhysicsModule => ...;
    }
}

public static class StateID
{
    public static class MyMod
    {
        public static Identification MainMenu => ...;
        public static Identification Gameplay => ...;
    }
}
```

### Usage

```csharp
// Type-safe module references
public static IReadOnlyList<Identification> RequiredModules => [
    StateModuleID.Sparkitect.Core,
    StateModuleID.MyMod.PhysicsModule
];

// Type-safe state transitions
stateManager.Request(StateID.MyMod.Gameplay);
```

### Benefits

- Compile-time validation (no typos in string IDs)
- IntelliSense discovery of available IDs
- Refactoring support (rename updates all references)
- Clear mod ownership (IDs namespaced by mod)

## YAML Resource Files

Mods can register resources using `.sparkres.yaml` files for declarative registration without code:

### File Format

```yaml
# {modname}.sparkres.yaml
# Format: RegistryClass.RegistryMethod:
#   - key: "resource_path"

Sparkitect.Graphics.Vulkan.ShaderModuleRegistry.RegisterShaderModule:
  - pong: "pong.spv"
  - ui: "ui.spv"

MyMod.ItemRegistry.RegisterItem:
  - iron_sword: "items/iron_sword.json"
```

### How It Works

1. Place `.sparkres.yaml` files in your mod project
2. SDK includes them in the mod archive
3. At runtime, the registry system discovers and processes them
4. Each entry calls the corresponding registry method with the key and resource path

### When to Use

| Scenario | Use YAML | Use Code |
|----------|----------|----------|
| Simple resource registration | Yes | - |
| Registration needs computed data | - | Yes |
| Batch registration of assets | Yes | - |
| Registration with complex logic | - | Yes |

YAML registration is ideal for assets (shaders, textures, data files) where the registration is purely declarative.

## Integration with Core Systems

The Modding Framework is tightly integrated with other core systems:

- **Dependency Injection**:
  Services are registered using the `[StateService]` attribute. Configurators are auto-generated (marked `[CompilerGenerated]`).

  ```csharp
  // Services are registered using the StateService attribute
  [StateService<IMyService, MyModule>]
  public class MyService : IMyService
  {
      public MyService(ILogger logger)
      {
          // Constructor dependencies are automatically detected
      }
  }
  ```

  See [Dependency Injection](xref:sparkitect.core.dependency-injection) for details on service registration and the `[StateService]` attribute.

- **Registry System**:
  Registrations use generated attributes from `[RegistryMethod]` definitions:

  ```csharp
  // Use generated registration attributes from [RegistryMethod] definitions
  [MyRegistry.RegisterSomething("my_object")]
  public static MyObjectData MyObject => new MyObjectData
  {
      Name = "My Object",
      SomeProperty = "value"
  };
  ```

  See [Registry System](xref:sparkitect.core.registry-system) for details on defining registries and registration patterns.

- **Lifecycle Management**: Object lifecycle is directly tied to mod lifecycle
