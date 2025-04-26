---
uid: articles.core.dependency-injection
---

# Dependency Injection System

The Dependency Injection (DI) system forms a critical part of Sparkitect's architecture, serving as the backbone of the modding framework and enabling modular access to services and components.

## Core Concepts

### DI Framework

Sparkitect uses DryIoc as its DI/IoC (Inversion of Control) framework. This framework handles:

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

## Entrypoint System

The DI system uses a discoverable entrypoint pattern for modular configuration. This approach combines attribute-based marking with abstract base classes to define configuration and registration points.

### ConfigurationEntrypoint Base Class

The `ConfigurationEntrypoint<TAttribute>` pattern is a key architectural component:

```csharp
public interface ConfigurationEntrypoint<TDiscoveryAttribute> : BaseConfigurationEntrypoint
    where TDiscoveryAttribute : Attribute 
{
    static Type BaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
```

Classes that implement this interface and are marked with the corresponding attribute are automatically discovered during engine initialization. This pattern is designed to work with a source generator system (currently in development) that will streamline configuration by:

1. Finding all classes marked with discovery attributes
2. Generating efficient lookup code 
3. Auto-registering these components in the appropriate containers

> [!NOTE]
> All implementations of the ConfigurationEntrypoint pattern are designed to work with source generators in future updates. The current implementation uses runtime reflection, but this will be replaced by compile-time code generation.

### Available Entrypoints

The system provides several specialized entrypoints for different configuration tasks:

#### CoreConfigurator

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

Used for configuring the core DI container with essential services.

#### IoCRegistryBuilder

```csharp
[IoCRegistryBuilderEntrypoint]
public class MyRegistryBuilder : IIoCRegistryBuilder
{
    public void ConfigureRegistries(IRegistryProxy registryProxy)
    {
        registryProxy.AddRegistry<MyRegistry>("category_name");
    }
}
```

Adds registries to the registry system.

#### Registrations

```csharp
[RegistrationsEntrypoint]
public class MyRegistrations : Registrations<MyRegistry>
{
    public override string CategoryIdentifier => "category_name";
    
    public override void MainPhaseRegistration(MyRegistry registry)
    {
        // Registration logic here
    }
}
```

Handles object registration within specific registries.

## Service Registration

### Registration Methods

Components are added to DI containers through several methods:

1. **Annotated Classes**: Classes marked with specific attributes
2. **Registry Base Classes**: Classes that extend registry-specific base classes
3. **Explicit Registration Code**: Custom registration logic using DryIoc APIs

### Type-Safe Registrations

The `Registrations<TRegistry>` class provides type-safe registry operations:

```csharp
public abstract class Registrations<TRegistry> : Registrations where TRegistry : IRegistry
{
    public sealed override void MainPhaseRegistration(IRegistry registry)
    {
        MainPhaseRegistration((TRegistry) registry);
    }

    public abstract void MainPhaseRegistration(TRegistry registry);
}
```

This pattern ensures that:
- Registry types are verified at compile-time
- IDE auto-completion works correctly for specific registry operations
- Runtime type errors are prevented

## Container Lifecycle

### Container Creation and Management

1. Core container initialized at application startup by the EngineBootstrapper
2. Root container created during mod loading based on CoreConfigurator implementations
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

### When to Use the Entrypoint Pattern

- Use `CoreConfigurator` for registering core services
- Use `IIoCRegistryBuilder` to define new registry categories
- Use `Registrations<T>` for registering objects within specific registries

### Container Management

- Treat containers as immutable once created
- Create child containers for scoped operations
- Allow the Game State system to manage container lifecycles