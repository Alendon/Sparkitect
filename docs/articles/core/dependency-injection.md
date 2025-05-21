---
uid: articles.core.dependency-injection
---

# Dependency Injection System

The Dependency Injection (DI) system forms a critical part of Sparkitect's architecture, serving as the backbone of the modding framework and enabling modular access to services and components.

After comparing different DI/IoC Frameworks and careful considerations, the decision was made to create a custom
lightweight DI Framework for Sparkitect directly built in.

The main reason for this, is to comply with the core modding philosophy and the ability to load and especially unload
mods at runtime.

## Container Types

The DI system currently utilizes 3 different types of containers, specialized for their use cases.
Changing this later to a more general variation or allowing more customization is intended to be possible but currently not planned.

Container Types:
- CoreContainer: Pure Singleton Container. Providing implementations to interfaces. The need for more special cases in this container is currently not identified.
- EntrypointContainer: Providing a way for mods to expose "Entrypoints", eg mod code execution before/outside of the registry system. This is intended to be the primary way to invoke mod code from the engine/other mods
- FactoryContainer: An object factory container based on keyed registrations and optional object caching/recycling facility.

### Core Container

Singleton Container building a container hierarchy, the default hierarchy is the following:
- Base: Minimal Base Core container, containing all base implementations for modding and engine pre initialization.
- Root: Actual base container, gets created after all root mods are loaded and contains root mod definitions
- Game: Container created when starting a game, containing game mods definitions. 

The containers are created by a hierarchy. Implementations can be overriden inside the same level in the hierarchy but
not on levels above. EG the Engine exposes a INetworkManager on the Root Level, a root mod could override this but not a game mod.

The specialized functional containers (entrypoint and factory), can only be created on the current leaf of the container hierarchy,
already created functional containers when creating a new hierarchy level, are assumed as invalid and must be recreated.
Same applies when destroying a container.

Once a container is created, it cannot be modified. Similarly, sub-containers cannot modify bindings from parent containers.

### Entrypoint Container

Manages Entrypoints of a singular base type and allows fetching all registered implementations.
The idea is that the used entrypoint base type, exposes one or multiple functions which can then be executed sequentially.

The registry system uses this behaviour to execute the code for adding new registry types and object registrations.

### Factory Container

The factory container provides an object factory pattern based on keyed registrations with optional object caching and recycling. It enables:

1. Creating objects on demand with specific dependencies
2. Resolving circular dependencies through property injection
3. Efficient object reuse through caching mechanisms

Service factories have both constructor-time dependencies (required for instantiation) and property-time dependencies (applied after creation). This separation allows handling circular dependencies by creating all objects first, then applying their interdependent properties.

## Container Construction and Dependency Resolution

Containers are immutable once built. Service registration and dependency configuration happens only during the builder phase.

### Service Factory Pattern

Services are registered with explicit factory classes that provide constructor and property dependency information:

```csharp
// Service factory containing dependency metadata and creation logic
[ServiceFactory<ModManager>]
internal class ModManagerFactory : IServiceFactory
{
    // Define service interfaces
    public Type ServiceType => typeof(IModManager);
    public Type ImplementationType => typeof(ModManager);
    
    // Explicitly declare constructor dependencies
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => 
    [
        (typeof(ICliArgumentHandler), false),
        (typeof(IIdentificationManager), false)
    ];
    
    // Explicitly declare property dependencies
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];
    
    // Use UnsafeAccessorAttribute to access private constructor
    public object CreateInstance(ICoreContainerBuilder container)
    {
        if (!container.TryResolveInternal<ICliArgumentHandler>(out var cliArgumentHandler) ||
            !container.TryResolveInternal<IIdentificationManager>(out var identificationManager))
            throw new DependencyResolutionException("Failed to resolve required dependencies for ModManager");
        
        return Constructor(cliArgumentHandler, identificationManager);
        
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        static extern ModManager Constructor(
            ICliArgumentHandler cliArgumentHandler,
            IIdentificationManager identificationManager);
    }
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        if (instance is not ModManager)
            throw new InvalidCastException($"Service of type {instance.GetType().Name} could not be cast to ModManager");
            
        // No properties to apply here
    }
}
```

In the future, these factory classes will be primarily generated by Source Generators.


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

1. **Service Factories**: Classes implementing `IServiceFactory` with the `[ServiceFactory<T>]` attribute
2. **Registry Base Classes**: Classes that extend registry-specific base classes
3. **Container Builder**: Explicit registration using the container builder API

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

1. Core containers are built using a builder pattern, with immutability after creation
2. Base container initialized at application startup by the EngineBootstrapper
3. Root container created during mod loading based on CoreConfigurator implementations
4. Registry containers created during registry processing
5. Game States are responsible for managing container lifecycles

### Dependency Graph Management

The container builder uses QuikGraph to:
1. Validate dependencies during construction
2. Detect circular dependencies
3. Determine optimal instantiation order through topological sorting

### Access to Services

Services can be accessed through:

1. Explicit resolution from a container via `Resolve<T>()`
2. Optional resolution with `TryResolve<T>(out T service)`
3. Service factories for controlled creation with dependencies

## Integration with Modding

The DI system is tightly coupled with the modding framework:

1. Mods register their services with the DI container during loading
2. The modding system creates appropriate containers for mod loading
3. Services can be overridden by mods through appropriate registration

## Best Practices

### Creating Service Factories

- Create a dedicated factory class for each service implementation
- Explicitly declare all dependencies in the factory
- Use the `[ServiceFactory<T>]` attribute to mark service factories
- Use UnsafeAccessorAttribute for constructor and property access
- Use pattern matching for type safety in factory methods

### When to Use the Entrypoint Pattern

- Use `CoreConfigurator` for registering core services
- Use `IIoCRegistryBuilder` to define new registry categories
- Use `Registrations<T>` for registering objects within specific registries

### Container Management

- Treat containers as immutable once created
- Properly dispose containers when finished
- Create child containers for scoped operations
- Allow the Game State system to manage container lifecycles