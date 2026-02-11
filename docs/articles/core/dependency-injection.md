---
uid: sparkitect.core.dependency-injection
title: Dependency Injection
description: Custom DI framework with source-generated factories, container hierarchy, and mod integration
---

# Dependency Injection

Sparkitect includes a custom DI framework built for runtime mod loading and unloading. Standard DI/IoC frameworks assume a single composition root; Sparkitect needs containers that can be created and destroyed as game states change and mods are loaded.

## Registering a Service

Most mod developers only need one attribute:

```csharp
[StateService<ITimeManager, CoreModule>]
public class TimeManager : ITimeManager
{
    public TimeManager(ILogger logger)
    {
        // Constructor dependencies are detected automatically
    }
}
```

The source generator picks up [`[StateService<TInterface, TModule>]`](xref:Sparkitect.GameState.StateServiceAttribute`2), creates a factory class (`TimeManager_Factory`), and generates a configurator (`CoreModule_ServiceConfigurator`) that registers the factory with the container builder. You never write or see these generated types unless you need to debug registration issues.

Dependencies declared as constructor parameters are resolved automatically during container construction. If a dependency is missing, the build fails immediately.

## Circular Dependencies

Constructor injection cannot resolve circular references (A depends on B, B depends on A). Use `required` properties for the circular edge:

```csharp
[StateService<IServiceA, MyModule>]
public class ServiceA : IServiceA
{
    public ServiceA(ILogger logger) { }

    // Resolved after all services are constructed
    public required IServiceB ServiceB { get; init; }
}

[StateService<IServiceB, MyModule>]
public class ServiceB : IServiceB
{
    public ServiceB(ILogger logger) { }

    public required IServiceA ServiceA { get; init; }
}
```

The container resolves dependencies in two phases:

1. All services are instantiated with their constructor dependencies
2. `required` properties are set on all instances

This allows both services to exist before either property is assigned. Prefer constructor injection when there is no circular dependency.

<a id="facade-integration"></a>

## Facade Integration

Facades enable interface substitution during resolution. A service can expose a reduced API to specific consumers (e.g., state functions) while keeping its full interface available elsewhere.

The pattern uses [`FacadeMarkerAttribute<TFacade>`](xref:Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute`1) as the base, with two specializations:

- [`StateFacade<TFacade>`](xref:Sparkitect.GameState.StateFacadeAttribute`1): Reduced API for state functions
- [`RegistryFacade<TFacade>`](xref:Sparkitect.Modding.RegistryFacadeAttribute`1): Reduced API during registry processing

```csharp
[StateFacade<IGameStateManagerStateFacade>]
public interface IGameStateManager
{
    // Full public API
}

public interface IGameStateManagerStateFacade
{
    // Subset visible to state functions
}

internal class GameStateManager : IGameStateManager, IGameStateManagerStateFacade
{
    // Implements both
}
```

During container build, generators create facade configurators (marked `[CompilerGenerated]`) that map facade types to their public types. When a state function declares a facade type as a parameter, `ResolveMapped`/`TryResolveMapped` checks the facade map, resolves the public type from the container, and returns it cast as the facade.

As a mod developer, you interact with facades indirectly through state functions. The resolution happens automatically.

## Container Types

The DI system uses three container types.

### CoreContainer

Singleton container organized as a hierarchy:

- **Root**: Created during engine initialization with core engine services (ModManager, GameStateManager, IdentificationManager, etc.)
- **State-level**: Created during state transitions as children of the parent state's container (or Root for the first state)

Containers are immutable after `Build()`. Child containers inherit parent services and can only add new registrations. Attempting to register a service that already exists in a parent throws `InvalidOperationException`. Within the same level, use `Override` on the builder to replace an existing registration.

Functional containers (Entrypoint, Factory) are only valid on the current leaf of the hierarchy. Creating a new hierarchy level or destroying a container invalidates them.

### EntrypointContainer

Holds instances of a single [`IBaseConfigurationEntrypoint`](xref:Sparkitect.DI.IBaseConfigurationEntrypoint) type and provides access to all discovered implementations. The engine uses this to process configurators and registrations in a deterministic sequence.

This is not an IoC container. It performs no dependency resolution or constructor injection. It is a typed collection that the engine iterates over.

Entrypoint ordering is controlled via [`[EntrypointOrderAfter<T>]`](xref:Sparkitect.DI.Ordering.EntrypointOrderAfterAttribute`1) and [`[EntrypointOrderBefore<T>]`](xref:Sparkitect.DI.Ordering.EntrypointOrderBeforeAttribute`1) attributes, resolved using Kahn's algorithm with lexicographic tiebreaking. For cross-mod ordering where the target type is unavailable at compile time, string-based variants accept a full type name: `[EntrypointOrderAfter("Namespace.TypeName")]`. Without explicit constraints, entrypoints are ordered lexicographically by type name.

### FactoryContainer

Keyed factory pattern where objects are created on demand. Each `Resolve` call produces a new instance. Dependencies are handled through the same two-phase approach (constructor then property injection) as the CoreContainer.

The primary consumer is the [Registry System](xref:sparkitect.core.registry-system), which uses [`FactoryContainer<IRegistryBase>`](xref:Sparkitect.DI.FactoryContainer) to manage registry instances by key.

## Container Hierarchy

```
Root CoreContainer (engine services)
  +-- State CoreContainer (state services)
        +-- FactoryContainer<IRegistryBase> (registries)
        +-- EntrypointContainer<...> (configurators)
```

| Need | Container | Example |
|------|-----------|---------|
| Singleton shared across states | CoreContainer (root) | ILogger, <xref:Sparkitect.Modding.IModManager> |
| Service scoped to one state | CoreContainer (state-level) | IPhysicsService |
| Keyed objects by string/ID | <xref:Sparkitect.DI.FactoryContainer> | Registries |
| Discover configurators from mods | EntrypointContainer | <xref:Sparkitect.GameState.IStateModuleServiceConfigurator> |

## Service Lifetimes

All CoreContainer services are singletons: one instance per container, created during `Build()`. There is no transient or scoped lifetime.

Service lifetimes are tied to the state that created them. Root container services persist for the application lifetime. State container services live only as long as that state is active. When a state is destroyed, its container is disposed and all its services end.

Parent container services remain available to child containers without re-creation.

FactoryContainer instances are not managed by the container. Each `Resolve` call produces a new object and the caller owns its lifetime.

## Entrypoint System

The DI system uses a discoverable entrypoint pattern for modular configuration. Entrypoint types implement [`IConfigurationEntrypoint<TDiscoveryAttribute>`](xref:Sparkitect.DI.IConfigurationEntrypoint`1):

```csharp
public interface IConfigurationEntrypoint<TDiscoveryAttribute> : IBaseConfigurationEntrypoint
    where TDiscoveryAttribute : Attribute
{
    static Type IBaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
```

All entrypoints require parameterless constructors. Dependencies are provided through method parameters (e.g., [`ICoreContainerBuilder`](xref:Sparkitect.DI.ICoreContainerBuilder)), not constructor injection.

### IStateModuleServiceConfigurator (Generated)

You never write these. When you mark classes with [`[StateService<TInterface, TModule>]`](xref:Sparkitect.GameState.StateServiceAttribute`2), the source generator creates one configurator per module:

```csharp
// Automatically generated (marked [CompilerGenerated]):
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

One configurator per module, registering all services for that module.

### CoreConfigurator (Manual, Rare)

Abstract class for registering services outside the state module pattern. Provides `ConfigureIoc(ICoreContainerBuilder container)`. This is rare and not needed for typical mod development.

### Registry Configurator

Adds registries using a factory container builder. Covered in detail in the [Registry System](xref:sparkitect.core.registry-system) documentation.

```csharp
public class MyRegistryConfigurator : IRegistryConfigurator
{
    public void Configure(IFactoryContainerBuilder<IRegistryBase> builder, IReadOnlySet<string> loadedMods)
    {
        // Register registry factories
    }
}
```

## Resolving Services

### CoreContainer

```csharp
// Throws if not found
var service = container.Resolve<IMyService>();

// Returns false if not found
if (container.TryResolve<IMyService>(out var service))
{
    // Use service
}

// Non-generic resolution
if (container.TryResolve(typeof(IMyService), out var obj))
{
    var service = (IMyService)obj;
}

// All registered instances
var all = container.GetCurrentRegisteredInstances();
```

### FactoryContainer

```csharp
// Resolve by key
if (factoryContainer.TryResolve("json", out var processor))
{
    // Use processor
}

// All keyed instances
var all = factoryContainer.ResolveAll();
foreach (var (key, instance) in all)
{
    // Process each
}
```

## Dependency Graph Validation

The container builder uses QuikGraph to validate the dependency graph during construction:

- Detects circular dependencies (constructor-level only; property injection is the escape hatch)
- Determines instantiation order through topological sorting
- Missing dependencies fail the build immediately

## Generated Type Naming

| Pattern | Convention | Example |
|---------|-----------|---------|
| Service interfaces | `I` prefix | `ITimeManager` |
| Service factories | `{Class}_Factory` | `TimeManager_Factory` |
| Configurators | `{Module}_ServiceConfigurator` | `CoreModule_ServiceConfigurator` |
