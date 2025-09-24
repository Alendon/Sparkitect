---
uid: articles.core.registry-system
---

# Registry System

The Registry System provides a mechanism for tracking and managing game objects and resources. It ensures that all components can consistently reference objects across the engine and mods.

> [!NOTE]
> The Registry System is currently in early development. This documentation provides a coarse outline of its structure and intended functionality.

## Core Structure

### Registry Management Components

The Registry System includes:

- **RegistryManager**: Coordinates registry build and teardown at the request of state logic
- **IdentificationManager**: Handles the mapping between string IDs and numeric IDs
- **Registry Categories**: Type-specific registries for different kinds of game objects
- **Registry Entries**: The actual registered objects

### Per-State Triggered Registration

Registries are triggered by state transitions rather than a single global pass. Unified state functions (declared in modules) drive when and how registries are invoked:

- Enter events (e.g., `OnModuleEnter`, `OnStateEnter`) perform category discovery and object registrations required by the new configuration.
- Exit events (e.g., `OnStateExit`, `OnModuleExit`) perform explicit unregistration/cleanup of objects tied to the leaving scope.

State logic is responsible for the ordering of registry calls using function ordering rules. This provides deterministic and composable registration.

## Identifier System

### Three-Level Hierarchy

Identifiers use a three-level hierarchical structure:

- **ModId**: Identifies the source mod (string mapped to numeric ID)
- **CategoryId**: Identifies the registry category (string mapped to numeric ID)
- **ObjectId**: Identifies the specific object within the category (string mapped to numeric ID)

### Dual Representation

The identification system maintains dual representations:

- **String Identifiers**: Human-readable, stable across sessions (e.g., "core:block:stone")
- **Numeric Identifiers**: Compact, efficient runtime representation (e.g., 1:3:42)

The `IdentificationManager` handles bidirectional mapping between these representations.

## Registration Process

### Registry Definition

Registries are defined and added using an `IRegistryConfigurator`:

```csharp
public class RegistryConfigurator : IRegistryConfigurator
{
    public void ConfigureRegistries(IFactoryContainerBuilder<IRegistry> registryBuilder)
    {
        // registryBuilder.Register(new MyRegistry_KeyedFactory());
    }
}
```

> Note: Category identifiers (string IDs) must be globally unique. Duplicate category IDs or references to missing categories result in exceptions during registry processing.

### Object Registration

Objects are registered through typed `Registrations` classes. These are invoked by state logic during enter/exit events, not by a global pass:

```csharp
[RegistrationsEntrypoint]
public class MyRegistrations : Registrations<MyRegistry>
{
    public override string CategoryIdentifier => "category_name";
    
    public override void MainPhaseRegistration(MyRegistry registry)
    {
        // Access DI via base properties after Initialize() has been called
        var id = IdentificationManager.RegisterObject("my_mod", "category_name", "my_object");
        registry.RegisterSomething(id, "additional data");
    }
}
```

## Integration with Dependency Injection

The Registry System is integrated with DI:

- Registry classes are registered and resolved through DI
- Registrations receive dependencies through base properties after `Initialize(ICoreContainer)`
- Registry operations are invoked within the current state’s DI context (old/new side as applicable), not a separate global container

## Future Development

Planned enhancements to the Registry System include:

- Versioning and migration support
- Extended validation mechanisms
- Source generation for registry declarations and usage
- Enhanced file-based registration
