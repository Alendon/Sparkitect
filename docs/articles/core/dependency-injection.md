---
uid: sparkitect.core.dependency-injection
title: Dependency Injection System
description: Custom DI framework with source-generated factories, container hierarchy, and mod integration
---

# Dependency Injection System

The Dependency Injection (DI) system forms a critical part of Sparkitect's architecture, serving as the backbone of the modding framework and enabling modular access to services and components.

After comparing different DI/IoC Frameworks and careful considerations, the decision was made to create a custom
lightweight DI Framework for Sparkitect directly built in.

The main reason for this, is to comply with the core modding philosophy and the ability to load and especially unload
mods at runtime.

## Container Types

The DI system utilizes three types of containers, each specialized for its use case:

- **CoreContainer**: Pure singleton container that provides implementations to interfaces.
- **EntrypointContainer**: Container holding discovered configuration entrypoints (e.g., service configurators, registry configurators, registrations) and allowing sequential processing by the engine.
- **FactoryContainer**: An object factory container based on keyed registrations and optional object caching/recycling facility.

### Core Container

Singleton Container building a container hierarchy:
- **Root**: Core container created during engine initialization, containing essential engine services (ModManager, GameStateManager, IdentificationManager, etc.)
- **State**: Containers created during state transitions, forming a hierarchical stack. Each state container is a child of its parent state's container (or Root for the first state)

Containers are immutable after creation. Child containers add new services only -- parent services are inherited and immutable. Attempting to register a service that already exists in a parent container will throw an `InvalidOperationException`. Within the same container level, services can be overridden using the explicit `Override` method.

The specialized functional containers (entrypoint and factory), can only be created on the current leaf of the container hierarchy,
already created functional containers when creating a new hierarchy level, are assumed as invalid and must be recreated.
Same applies when destroying a container.

### Choosing the Right Container

| Need | Use | Example |
|------|-----|---------|
| Singleton service shared across states | CoreContainer | ILogger, IModManager |
| Service scoped to a state/module | CoreContainer (state-level) | IPhysicsService |
| Keyed objects resolved by string/ID | FactoryContainer | IRegistry by category |
| Discover configurators from mods | EntrypointContainer | IStateModuleServiceConfigurator discovery |

**Container Hierarchy:**

```
Root CoreContainer (engine services)
  +-- State CoreContainer (state services)
        +-- FactoryContainer<IRegistryBase> (registries)
        +-- EntrypointContainer<...> (configurators)
```

Each state level can have its own functional containers (Factory, Entrypoint). When a new state is pushed, functional containers from the parent are invalidated and must be recreated.

**Key principle:** CoreContainer for services, FactoryContainer for keyed object factories, EntrypointContainer for mod-discovered configurators.

## Service Lifetimes

All services registered in a `CoreContainer` are **singletons** -- exactly one instance per container. There is no transient or scoped lifetime concept in the DI system.

Key lifetime semantics:

- **CoreContainer services**: One instance per container, created during `Build()`. The instance lives as long as the container exists.
- **FactoryContainer**: Creates a **new instance** on each `Resolve` call. This is a factory pattern, not a lifetime management strategy -- each resolution produces a fresh object.
- **Container disposal**: When a state transition destroys a state, its container is disposed. All services created by that container are effectively ended. A new state creates a new container with new service instances.
- **Parent container services**: Services from parent containers remain available to child containers. They are not re-created -- the same instance is shared through the hierarchy.

This means service lifetimes are tied to the lifecycle of the state that created them. Root container services persist for the application lifetime; state container services live only as long as that state is active.

### Entrypoint Container

Holds instances of a single `IBaseConfigurationEntrypoint` type and allows fetching all discovered implementations.
The engine uses this to execute configurators and registrations in a deterministic sequence.

**Entrypoint ordering** is controlled via `[EntrypointOrderAfter<T>]` and `[EntrypointOrderBefore<T>]` attributes. These define explicit ordering constraints between entrypoint classes. The engine resolves these constraints using Kahn's algorithm with lexicographic tiebreaking for deterministic ordering. For cross-mod ordering where the target type is not available at compile time, string-based variants `[EntrypointOrderAfter("FullTypeName")]` and `[EntrypointOrderBefore("FullTypeName")]` are also available. Without explicit ordering constraints, entrypoints are ordered lexicographically by type name.

### Factory Container

The factory container provides an object factory pattern based on keyed registrations with optional object caching and recycling. It enables:

1. Creating objects on demand with specific dependencies
2. Resolving circular dependencies through property injection
3. Efficient object reuse through caching mechanisms

Service factories have both constructor-time dependencies (required for instantiation) and property-time dependencies (applied after creation). This separation allows handling circular dependencies by creating all objects first, then applying their interdependent properties.

## Container Construction and Dependency Resolution

Containers are immutable once built. Service registration and dependency configuration happens only during the builder phase.

### Service Factory Pattern

Service factories encapsulate the logic to create service instances with their dependencies. The generated factories handle both constructor and property injection.

### Property Injection for Circular Dependencies

Constructor injection cannot resolve circular dependencies (A needs B, B needs A). Use property injection with `required` properties:

```csharp
[StateService<IServiceA, MyModule>]
public class ServiceA : IServiceA
{
    // Constructor dependencies (resolved first)
    public ServiceA(ILogger logger) { }

    // Property dependencies (resolved after construction)
    public required IServiceB ServiceB { get; init; }
}

[StateService<IServiceB, MyModule>]
public class ServiceB : IServiceB
{
    public ServiceB(ILogger logger) { }

    // Circular reference via property
    public required IServiceA ServiceA { get; init; }
}
```

The DI system resolves dependencies in two phases:
1. **Constructor phase**: All services instantiated with constructor dependencies
2. **Property phase**: Required properties are set on all instances

This allows both ServiceA and ServiceB to exist before either's property is set.

**When to use property injection:**
- Circular dependencies between services
- Optional dependencies that may not always be registered
- Late-bound dependencies where constructor order matters

**Prefer constructor injection** when there's no circular dependency - it makes dependencies explicit and fails fast on missing dependencies.

### Service Registration Attributes

Services are registered using specialized attributes that implement the `IFactoryMarker<T>` interface. The source generators automatically create factory classes and configurators (marked with `[CompilerGenerated]`) through these marker attributes:

```csharp
// State service registration (primary pattern for mod developers)
[StateService<ITimeManager, CoreModule>]
public class TimeManager : ITimeManager
{
    // Constructor dependencies are automatically detected
    public TimeManager(ILogger logger)
    {
        // Implementation here
    }

    // Required properties for circular dependency injection
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
- `StateServiceAttribute<TInterface, TModule>` for state-scoped services (primary pattern)
- `KeyedFactoryAttribute<TBase>` for keyed factories

**Key Type Restriction**: `KeyedFactory` keys are restricted to `string` or `Sparkitect.Modding.Identification` types. The key property (or static property referenced by `KeyPropertyName`) must return one of these types.

ConfigurationEntrypoints are not manually written; they're automatically generated (marked `[CompilerGenerated]`) or discovered at runtime via discovery attributes and instantiated via parameterless constructors.

## Entrypoint System

The DI system uses a discoverable entrypoint pattern for modular configuration. This approach combines attribute-based marking with abstract base classes to define configuration and registration points.

### IConfigurationEntrypoint Base Interface

The `IConfigurationEntrypoint<TDiscoveryAttribute>` pattern underpins configurator discovery. It exposes the discovery attribute type and is implemented by entrypoint types such as `ICoreConfigurator<T>`, `IFactoryConfigurator<TBase, T>`, and their specializations.

```csharp
public interface IConfigurationEntrypoint<TDiscoveryAttribute> : IBaseConfigurationEntrypoint
    where TDiscoveryAttribute : Attribute
{
    static Type IBaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
```

ConfigurationEntrypoints must have parameterless constructors. DI is provided through method parameters (e.g., `ICoreContainerBuilder`), not through constructor injection.

### Available Entrypoints

The system provides several specialized entrypoints for different configuration tasks:

#### IStateModuleServiceConfigurator (Generated)

`IStateModuleServiceConfigurator` implementations are **automatically generated** (marked `[CompilerGenerated]`) by the source generator system. When you mark classes with `[StateService<TInterface, TModule>]` attributes, a configurator class is automatically created to register all the service factories for that module.

**Automatic Generation Example:**
```csharp
// Your state service
[StateService<ITimeManager, CoreModule>]
public class TimeManager : ITimeManager { }

// Automatically generated configurator (marked [CompilerGenerated]):
[StateModuleServiceConfiguratorEntrypoint]
[CompilerGenerated]
internal class CoreModule_ServiceConfigurator : IStateModuleServiceConfigurator
{
    public Type ModuleType => typeof(CoreModule);

    public void Configure(ICoreContainerBuilder builder, IReadOnlySet<string> loadedMods)
    {
        builder.Register<TimeManager_Factory>();
    }
}
```

The generated configurator class is internal and marked with `[CompilerGenerated]`. One configurator is generated per module, registering all services for that module.

**Most mod developers only need `[StateService<TInterface, TModule>]`** and never interact with configurators directly.

#### CoreConfigurator (Manual, Rare)

The abstract `CoreConfigurator` class is a separate, manually-used entrypoint type that inherits from `IConfigurationEntrypoint<CoreContainerConfiguratorEntrypointAttribute>`. It provides a `ConfigureIoc(ICoreContainerBuilder container)` method for special cases where services need to be registered outside the standard state module pattern. This is rare and not needed for typical mod development.

#### Registry Configurator

```csharp
public class MyRegistryConfigurator : IRegistryConfigurator
{
    public void Configure(IFactoryContainerBuilder<IRegistryBase> builder, IReadOnlySet<string> loadedMods)
    {
        // Add registry factories here
        // builder.Register(new MyRegistry_KeyedFactory());
    }
}
```

Adds registries to the registry system using a factory container builder. The `IRegistryConfigurator` interface inherits from `IFactoryConfigurator<IRegistryBase, RegistryConfiguratorAttribute>`.

**Note**: Registry-specific configurators and registrations are covered in detail in the [Registry System](xref:sparkitect.core.registry-system) documentation.

## Service Registration

Components are added to DI containers through source-generated factories:

1. **State Services**: Classes marked with `[StateService<TInterface, TModule>]` automatically generate factory classes and module-specific configurators (all marked `[CompilerGenerated]`)
2. **Keyed Factories**: Classes marked with `[KeyedFactory<TBase>]` generate factories for key-based resolution
3. **Container Builder**: Explicit registration using the container builder API with generated factories (rare, for special cases)

## Container Lifecycle

### Container Creation and Management

1. Core containers are built using a builder pattern, with immutability after creation
2. Base container initialized at application startup by the EngineBootstrapper
3. Root container created during mod loading based on `IStateModuleServiceConfigurator` implementations
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
var allServices = container.GetCurrentRegisteredInstances();
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
3. Services can be overridden within the same container level through the explicit `Override` method on the container builder

## Naming Conventions

The DI system follows consistent naming patterns:

| Pattern | Convention | Example |
|---------|-----------|---------|
| Service interfaces | `I` prefix | `ITimeManager`, `IModManager` |
| Service factories | `{ClassName}_Factory` | `TimeManager_Factory` |
| Configurators | `{ModuleName}_ServiceConfigurator` | `CoreModule_ServiceConfigurator` |
| Entrypoint attributes | `{Purpose}Attribute` | `StateModuleServiceConfiguratorEntrypointAttribute` |

For Vulkan wrapper type naming conventions, see [Vulkan Graphics](xref:sparkitect.vulkan.vulkan-graphics).

## Best Practices

### Creating Services

- Use `[StateService<TInterface, TModule>]` for state-scoped services (primary pattern)
- Use `[KeyedFactory<TBase>]` for services that need key-based resolution
- Declare dependencies through constructor parameters and required properties
- For keyed factories, use either `Key = "value"` or `KeyPropertyName = nameof(Property)` with a static property
- Remember that `KeyedFactory` keys must be `string` or `Identification` types

### When to Use Configuration Entrypoints

- Configurators are typically auto-generated - you write service attributes, generators create configurators
- Manual `CoreConfigurator` implementations are rare and only needed for special cases outside the state module pattern
- Use `IRegistryConfigurator` to define new registry categories (see [Registry System](xref:sparkitect.core.registry-system))

### Container Management

- Treat containers as immutable once created
- Properly dispose containers when finished
- State containers are created as children of the Root container during state transitions
- Allow the Game State system to manage container lifecycles

## Facade Integration

The DI system provides a unified, attribute-driven facade mechanism for subsystems that need to separate internal facades from public APIs. This is primarily used by the Game State system.

### Facade Pattern

Facades enable interface substitution during resolution. The pattern uses `FacadeMarkerAttribute<TFacade>` as the base, with two specializations:

- **`StateFacade<TFacade>`**: Used on services that expose a reduced API to state functions
- **`RegistryFacade<TFacade>`**: Used on services that expose a reduced API during registry processing

```csharp
// Public interface with facade declaration
[StateFacade<IGameStateManagerStateFacade>]
public interface IGameStateManager
{
    // Public API methods
}

// Facade interface (what state functions see)
public interface IGameStateManagerStateFacade
{
    // State-specific methods
}

// Implementation provides both
internal class GameStateManager : IGameStateManager, IGameStateManagerStateFacade
{
    // Implements both interfaces
}
```

### How Facades Work

1. **Facade Declaration**: Interfaces marked with `[StateFacade<TFacade>]` or `[RegistryFacade<TFacade>]` declare their facade contract
2. **Implementation**: Services implement both the public interface and the facade interface
3. **Facade Map**: During container build, facade mappings are created (facade type -> public type)
4. **Mapped Resolution**: State function wrappers declare facade types as parameters. During resolution, `ResolveMapped`/`TryResolveMapped` checks the facade map: if the requested type exists in the map, the mapped public type is resolved from the container and returned cast as the facade type

This pattern enables:
- **Separation of Concerns**: State functions see only state-relevant methods
- **Type Safety**: Compile-time validation that implementations provide required facades
- **Encapsulation**: Internal state management methods aren't exposed to public DI

Generators create facade configurators (marked `[CompilerGenerated]`) that build the mapping at container creation time. As a mod developer, you typically interact with facades indirectly through state functions - the resolution happens automatically.
