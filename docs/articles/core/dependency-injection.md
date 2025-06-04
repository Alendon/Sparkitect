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

### Service Registration Attributes

Services are registered using specialized attributes that automatically generate factory classes through source generators:

```csharp
// Singleton service registration
[Singleton<IModManager>]
internal class ModManager : IModManager
{
    // Constructor dependencies are automatically detected
    public ModManager(ICliArgumentHandler cliArgumentHandler, IIdentificationManager identificationManager)
    {
        // Implementation here
    }
    
    // Required properties for dependency injection
    public required ISomeService SomeService { get; init; }
}

// Keyed factory registration with direct key
[KeyedFactory<IProcessor>(Key = "json")]
internal class JsonProcessor : IProcessor
{
    public JsonProcessor(ILogger logger) { }
}

// Keyed factory registration with property key
[KeyedFactory<IProcessor>(KeyPropertyName = nameof(ProcessorKey))]
internal class XmlProcessor : IProcessor  
{
    public static string ProcessorKey => "xml";
    public XmlProcessor(ILogger logger) { }
}
```

The source generators automatically create factory classes that handle dependency resolution, instantiation, and property injection based on these attributes.


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
    public override void ConfigureIoc(ICoreContainerBuilder container)
    {
        container.Register<MyService_Factory>();
    }
}
```

Used for configuring the core DI container with essential services by registering service factories.

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

1. **Source-Generated Factories**: Classes marked with attributes like `[Singleton<T>]`, `[KeyedFactory<T>]`, or `[EntrypointFactory<T>]` automatically generate factory classes
2. **Registry Base Classes**: Classes that extend registry-specific base classes  
3. **Container Builder**: Explicit registration using the container builder API with generated factories

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

Services can be accessed through the `ICoreContainer` interface:

```csharp
// Required service resolution - throws exception if not found
var service = container.Resolve<IMyService>();

// Optional service resolution - returns false if not found  
if (container.TryResolve<IMyService>(out var service))
{
    // Use service here
}

// Non-generic resolution by type
if (container.TryResolve(typeof(IMyService), out var serviceObj))
{
    var service = (IMyService)serviceObj;
}

// Get all registered instances
var allServices = container.GetRegisteredInstances();
```

### Factory Container Access

For keyed services, use the `IFactoryContainer<TBase>` interface:

```csharp
// Resolve specific keyed service
if (factoryContainer.TryResolve("json", out var jsonProcessor))
{
    // Use processor
}

// Resolve all keyed services
var allProcessors = factoryContainer.ResolveAll();
foreach (var (key, processor) in allProcessors)
{
    // Process each registered processor
}
```

## Integration with Modding

The DI system is tightly coupled with the modding framework:

1. Mods register their services with the DI container during loading
2. The modding system creates appropriate containers for mod loading
3. Services can be overridden by mods through appropriate registration

## Best Practices

### Creating Services

- Use `[Singleton<T>]` for single-instance services
- Use `[KeyedFactory<T>]` for services that need key-based resolution  
- Use `[EntrypointFactory<T>]` for discoverable entrypoint implementations
- Declare dependencies through constructor parameters and required properties
- For keyed factories, use either `Key = "value"` or `KeyPropertyName = nameof(Property)` with a static property

### When to Use the Entrypoint Pattern

- Use `CoreConfigurator` for registering core services
- Use `IIoCRegistryBuilder` to define new registry categories
- Use `Registrations<T>` for registering objects within specific registries

### Container Management

- Treat containers as immutable once created
- Properly dispose containers when finished
- Create child containers for scoped operations
- Allow the Game State system to manage container lifecycles