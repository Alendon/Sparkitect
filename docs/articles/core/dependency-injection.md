---
uid: articles.core.dependency-injection
---

# Dependency Injection System

The Dependency Injection (DI) system forms a critical part of Sparkitect's architecture, serving as the backbone of the modding framework and enabling modular access to services and components.

## Core Concepts

### DI Framework

Sparkitect uses Dryloc as its DI/IoC (Inversion of Control) framework. This framework handles:

- Service registration
- Service resolution
- Lifetime management
- Circular dependency detection

### Container Hierarchy

The DI system is structured as a hierarchy of containers:

- **Core Container**: Contains minimal engine services and is initialized at startup
- **Root Container**: Created after loading root mods, contains services from both the engine and root mods
- **Registry Containers**: Specialized containers for registry operations
- **Additional Scoped Containers**: Created as needed for specific operations

Once a container is created, it cannot be modified. Similarly, sub-containers cannot modify bindings from parent containers.

## Service Registration

### Registration Methods

Components are added to DI containers through several methods:

1. **Annotated Classes**: Classes marked with specific attributes
2. **Annotated Methods**: Methods that register services
3. **Explicit Registration Code**: Custom registration logic

### Entrypoints

The system uses multiple "entrypoints" (attribute annotations) for different registration purposes:

- **IoC Builder Entrypoints**: Add services to containers during construction

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

- **Registry Container Entrypoints**: Add registries to specialized registry containers

```csharp
[IoCRegistryBuilderEntrypoint]
public class MyIoCBuilder : IIoCRegistryBuilder
{
    public void ConfigureRegistries(Container container)
    {
        container.Register<MyRegistry, MyRegistry>();
    }
}
```

- **Registration Entrypoints**: Define registration logic for objects

```csharp
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

These entrypoints allow mods to extend or modify the IoC container during its construction phase.

### Source Generation

To simplify common DI patterns, Sparkitect provides source generators that automate registration code:

- `RegisterSingleton<TBaseInterface>` attribute
- `RegisterSingleton` attribute for single-interface classes

These source generators are opt-in, allowing developers to write custom registration code when needed for complex scenarios.

## Container Lifecycle

### Container Creation and Management

1. Core container initialized at application startup by the EngineBootstrapper
2. Root container created during mod loading based on IoC Builder Entrypoints
3. Registry containers created during registry processing
4. Additional containers created as needed for specific operations
5. Game States are responsible for managing container lifecycles

### Access to Services

Services can be accessed through:

1. Direct injection into constructors
2. Explicit resolution from a container
3. Factory patterns for creating instances with dependencies

## Integration with Modding

The DI system is tightly coupled with the modding framework:

1. Mods register their services with the DI container during loading
2. The modding system creates appropriate containers for mod loading
3. Services can be overridden by mods through appropriate registration

## Best Practices

### When to Use Source Generators

- Use source generators for standard singleton/transient registrations
- Write custom registration code for complex scenarios:
  - Modifying DI entries from other mods
  - Complex dependency chains
  - Custom lifetime management

### Container Management

- Treat containers as immutable once created
- Create child containers for scoped operations
- Allow the Game State system to manage container lifecycles