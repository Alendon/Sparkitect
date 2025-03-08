# Registry System

The Registry System provides a centralized mechanism for tracking and managing game objects and resources. It ensures that all components can consistently reference objects across the engine and mods.

## Core Structure

### Registry Hierarchy

The Registry System has a flat hierarchy consisting of:

- **RegistryManager**: Core component accessible through the DI container
- **Category Registries**: Specialized registries for different object types (e.g., SystemRegistry)
- **Registered Objects**: Actual game resources and objects

### Stateless Design

Registries function as stateless proxies:

- Registry instances do not maintain state themselves
- Registry Containers are typically used for a single registry process (register/unregister)
- Registry instances may be cached, but this behavior is not guaranteed
- Actual registration occurs in dedicated manager classes:
  - Example: SystemRegistry calls SystemManager for actual registration operations

## Identifier System

### Structure

Identifiers are composed of three parts:

- **ModId**: Identifies the source mod
- **CategoryId**: Identifies the registry category
- **ObjectId**: Identifies the specific object within its category

### Dual Representation

Identifiers use a dual representation system:

- **String Representation**: Stable across sessions and platforms
- **Numeric Representation**: May vary between runs and clients/servers

A translation layer handles conversion between these representations for serialization and persistence.

### Uniqueness

Duplicate ObjectIds are permitted between different ModId/CategoryId combinations. This allows mods to use simple, descriptive identifiers without worrying about collisions with other mods.

## Registration Methods

The Registry System supports multiple registration approaches:

### 1. Type Registry

- Utilizes generic register methods
- Generic type parameter determines what is registered
- Simplifies common registration patterns

```csharp
// Registration code
var numericId = registryManager.Register(modId, categoryId, objectId);
myRegistry.Register<SomeType>(numericId);
```

### 2. Function Registry

- Calls specific functions or properties
- Uses resulting value as parameter for register function
- Can leverage DI Container by specifying required services as function parameters

### 3. File Registry

- Registry objects defined in TOML files
- Source generator processes these files to generate registry code
- Files included via custom project SDK

## Source Generation Integration

Registries are designed to work with source generation:

- Registry methods annotated to opt into source generation
- Source generator creates RegisterAttribute for annotated methods
- These attributes can be applied to both types and functions to trigger code generation
- Balances complex registration logic with simplified usage patterns

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

## Resource File Management

Registry categories can define resource file requirements:

- Sets of required/optional resource files
- Typically implemented using TOML files for object definitions
- Categories may allow registration with just an ID when additional information is optional
- Method and Type Registries can also utilize additional files when needed

## Object Lifecycle

- No versioning system is planned for registered objects
- Object lifecycle is directly bound to mod lifecycle
- Resources are exposed by the Registry or Modding System
- Developers can query file streams for specific registered objects (multiple by key)
- Zip streams remain open throughout the application's lifetime for efficient access

## Registration Process

1. RegistryManager accessed through central DI container
2. DiHelper creates registry-specific sub-containers
3. Registries exposed through entrypoints
4. Registration performed via one of the three supported methods
5. Actual state changes managed by specialized manager classes

### Detailed Registration Flow

1. The Registry Manager creates a child container based on `IoCRegistryBuilderEntrypoint` classes
2. This container includes all Registry classes provided by mods
3. Registration logic accesses this container to perform registrations through `RegistrationsEntrypoint` classes
4. The Registry Manager allocates a numeric ID and associates file references with it
5. The appropriate Registry class is called with this ID and required parameters

The registration process is designed to be simple for common use cases while allowing for complex customization when needed.