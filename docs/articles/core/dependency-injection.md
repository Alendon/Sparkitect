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
- CoreContainer: Pure singleton container that provides implementations to interfaces.
- EntrypointContainer: Container holding discovered ConfigurationEntrypoints (e.g., CoreConfigurator, registry configurators, registrations) and allowing sequential processing by the engine.
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

Holds instances of a single ConfigurationEntrypoint base type and allows fetching all discovered implementations.
The engine uses this to execute configurators and registrations in a deterministic sequence (ordering rules to be added).

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

Services are registered using specialized attributes that implement the `IFactoryMarker<T>` interface. The source generators automatically create factory classes through these marker attributes:

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

Factory attributes that generate code are:
- `SingletonAttribute<T>` for singleton services
- `KeyedFactoryAttribute<T>` for keyed factories

ConfigurationEntrypoints are not generated; they’re discovered at runtime via discovery attributes and instantiated via parameterless constructors.


## Entrypoint System

The DI system uses a discoverable entrypoint pattern for modular configuration. This approach combines attribute-based marking with abstract base classes to define configuration and registration points.




### ConfigurationEntrypoint Base Class

The `ConfigurationEntrypoint<TAttribute>` pattern underpins configurator discovery. It exposes the discovery attribute type and is implemented by entrypoint base types such as `CoreConfigurator`, `IRegistryConfigurator`, and `Registrations`.

```csharp
public interface ConfigurationEntrypoint<TDiscoveryAttribute> : BaseConfigurationEntrypoint
    where TDiscoveryAttribute : Attribute 
{
    static Type BaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
```

ConfigurationEntrypoints must have parameterless constructors. DI is provided through method parameters (e.g., `ICoreContainer`), not through constructor injection.

### Available Entrypoints

The system provides several specialized entrypoints for different configuration tasks:

#### CoreConfigurator

CoreConfigurator implementations are **automatically generated** by the source generator system. When you mark classes with `[Singleton<T>]` attributes, a CoreConfigurator class is automatically created to register all the service factories.

**Automatic Generation Example:**
```csharp
// Your singleton service
[Singleton<IMyService>]
public class MyService : IMyService { }

// Automatically generated CoreConfigurator:
[CoreContainerConfiguratorEntrypoint]
[CompilerGenerated]
public class MyProjectConfigurator : CoreConfigurator
{
    public override void ConfigureIoc(ICoreContainerBuilder container)
    {
        container.Register<global::MyNamespace.MyService_Factory>();
    }
}
```

The generated configurator class name is based on the project's `RootNamespace` property, and the namespace matches the `RootNamespace`. All singleton services in the assembly are automatically registered.

**Manual Implementation (Legacy):**
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

Manual CoreConfigurator implementations are still supported but are not recommended as they require manual maintenance when services are added or removed.

#### Registry Configurator

```csharp
public class RegistryConfigurator : IRegistryConfigurator
{
    public void ConfigureRegistries(IFactoryContainerBuilder<IRegistry> registryBuilder)
    {
        // Add registry factories here
        // registryBuilder.Register(new MyRegistry_KeyedFactory());
    }
}
```

Adds registries to the registry system using a factory container builder.

#### Registrations

```csharp
[RegistrationsEntrypoint]
public class MyRegistrations : Registrations<MyRegistry>
{
    public override string CategoryIdentifier => "category_name";
    
    public override void MainPhaseRegistration(MyRegistry registry, ICoreContainer container)
    {
        // Registration logic here; resolve services through container if needed
    }
}
```

Handles object registration within specific registries.

## Service Registration

### Registration Methods

Components are added to DI containers through several methods:

1. **Source-Generated Factories**: Classes marked with attributes like `[Singleton<T>]` or `[KeyedFactory<T>]` automatically generate factory classes
2. **Registry Base Classes**: Classes that extend registry-specific base classes  
3. **Container Builder**: Explicit registration using the container builder API with generated factories

### Type-Safe Registrations

The `Registrations<TRegistry>` class provides type-safe registry operations:

```csharp
public abstract class Registrations<TRegistry> : Registrations where TRegistry : IRegistry
{
    public sealed override void MainPhaseRegistration(IRegistry registry, ICoreContainer container)
    {
        MainPhaseRegistration((TRegistry) registry, container);
    }

    public abstract void MainPhaseRegistration(TRegistry registry, ICoreContainer container);
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
- Declare dependencies through constructor parameters and required properties
- For keyed factories, use either `Key = "value"` or `KeyPropertyName = nameof(Property)` with a static property

### When to Use Configuration Entrypoints

- Use `CoreConfigurator` for registering core services
- Use `IRegistryConfigurator` to define new registry categories
- Use `Registrations<T>` for registering objects within specific registries

### Container Management

- Treat containers as immutable once created
- Properly dispose containers when finished
- Create child containers for scoped operations
- Allow the Game State system to manage container lifecycles
