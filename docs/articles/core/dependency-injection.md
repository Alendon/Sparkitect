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

Dependencies arrive via constructor parameters or `required` properties — see [Property Injection](#property-injection) for the property path. Constructor parameters are resolved automatically during container construction; if a dependency is missing, the build fails immediately.

`[StateService<TInterface, TModule>]` is currently the only mod-author path into the core engine DI container. See <xref:sparkitect.core.game-state-system> for module composition, lifecycle, and the full attribute reference.

## The DI Service (`IDIService`)

<xref:Sparkitect.DI.IDIService> is itself a service. Take it as a constructor dependency the same way you take any other service:

```csharp
[StateService<IMyService, MyModule>]
public class MyService(IDIService diService) : IMyService { /* ... */ }
```

Mod-facing methods:

| Method | Purpose |
|---|---|
| `CreateEntrypointContainer<T>(modIds)` | Discover and instantiate types marked with `T`'s entrypoint attribute from the named mods. |
| `BuildFactoryContainer<TKey, TBase>(container, provider, modIds, configuratorAttr)` | Build a keyed-factory container. In practice, mods call this through the SG-emitted extension produced by [`[KeyedFactoryGenerationMarker<TBase>]`](xref:Sparkitect.Modding.KeyedFactoryGenerationMarkerAttribute`1) — see [Keyed-Factory Generation](#keyed-factory-generation-for-registries) below. |

`RegisterModAssemblies` and `UnregisterMods` are engine-internal — `ModManager` calls them during mod lifecycle. Mods do not call them.

<a id="property-injection"></a>

## Property Injection

Properties marked `required` are populated after construction as a second injection phase. The `required` keyword expresses intent — must be supplied — and the builder treats the property as a constructor-equivalent dependency in the graph.

The primary use case is breaking constructor cycles: A depends on B, B depends on A — neither can construct first.

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

<a id="resolution-scopes"></a>

## Resolution Scopes & Specialization

<xref:Sparkitect.DI.Resolution.IResolutionScope> is the per-frame resolution surface every DI consumer ultimately calls into. Plugged into each scope is a single <xref:Sparkitect.DI.Resolution.IResolutionProvider> — the seam where resolution can be *specialized* before falling back to the container. A provider can interpret per-resolution metadata records (open-shape payloads carrying whatever the strategy needs) or short-circuit on service type alone.

Metadata is contributed through `IResolutionMetadataEntrypoint<TWrapperType>` implementations, source-generator-discovered via [`[ResolutionMetadataEntrypoint<TWrapperType>]`](xref:Sparkitect.DI.Resolution.ResolutionMetadataEntrypointAttribute`1). `TWrapperType` identifies the calling context (typically a factory type) and is the outer key the scope uses when looking up metadata for a resolution.

Built-in providers:

| Provider | Metadata record | Enables |
|---|---|---|
| `FacadeResolutionProvider` | `FacadeMapping` | Facade-parameter substitution (see below). |
| `EcsResolutionProvider` | `QueryParameterMetadata` (plus direct-type lookups) | Per-frame ECS query injection, frame timing, command buffers. |
| `RenderGraphResolutionProvider` | — | Placeholder for resource-shaped DI. |

### Facade Resolution

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

The generator emits a metadata entrypoint that contributes one `FacadeMapping` per facade parameter; `FacadeResolutionProvider` reads those entries and resolves the public type from the container. As a mod developer, you interact with facades indirectly through state functions — resolution happens automatically.

### Writing a Custom Resolution Feature

The seam is open: a new resolution feature defines a metadata record type, a provider that interprets it, and an entrypoint that contributes records. This is an advanced engine-feature seam, not the everyday modding API.

```csharp
public record CacheHintMetadata(TimeSpan MaxAge);

internal class CacheHintProvider : IResolutionProvider
{
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> entries, out object? service)
    {
        // Inspect entries, apply specialization. Return false to defer to the container.
        service = null;
        return false;
    }
}

[ResolutionMetadataEntrypoint<MyWrapperType>]
internal class MyWrapperMetadataSource : IResolutionMetadataEntrypoint<MyWrapperType>
{
    public void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies)
    {
        dependencies[typeof(IMyService)] = [new CacheHintMetadata(TimeSpan.FromSeconds(1))];
    }
}
```

## Container Types

The DI system uses two IoC container types.

### CoreContainer

Singleton container organized as a hierarchy:

- **Root**: Created during engine initialization with core engine services (ModManager, GameStateManager, IdentificationManager, etc.)
- **State-level**: Created during state transitions as children of the parent state's container (or Root for the first state)

Containers are immutable after `Build()`. Child containers inherit parent services and can only add new registrations. Attempting to register a service that already exists in a parent throws `InvalidOperationException`. Within the same level, use `Override` on the builder to replace an existing registration.

Functional containers (Entrypoint, Factory) are only valid on the current leaf of the hierarchy. Creating a new hierarchy level or destroying a container invalidates them.

### FactoryContainer

<xref:Sparkitect.DI.Container.IFactoryContainer`2> is a keyed map of factory instances. `TryResolve(key, …)` invokes the keyed factory; `ResolveAll()` iterates every key. Lifetime is configurator-controlled — the typical shape is one instance per key, but the contract makes no fresh-per-call guarantee.

The primary consumer is the [Registry System](xref:sparkitect.core.registry-system), which uses `IFactoryContainer<string, IRegistryBase>` to manage registry instances by key. The most common author-facing path to a keyed factory container is the SG-emitted extension produced by [`[KeyedFactoryGenerationMarker<TBase>]`](xref:Sparkitect.Modding.KeyedFactoryGenerationMarkerAttribute`1) — see [Keyed-Factory Generation](#keyed-factory-generation-for-registries) above.

## Container Hierarchy

```
Root CoreContainer (engine services)
  +-- State CoreContainer (state services)
        +-- IFactoryContainer<string, IRegistryBase> (registries)
        +-- EntrypointContainer<...> (configurators)
```

| Need | Container | Example |
|------|-----------|---------|
| Singleton shared across states | CoreContainer (root) | ILogger, <xref:Sparkitect.Modding.IModManager> |
| Service scoped to one state | CoreContainer (state-level) | IPhysicsService |
| Keyed objects by string/ID | <xref:Sparkitect.DI.Container.IFactoryContainer`2> | Registries |
| Discover configurators from mods | EntrypointContainer | <xref:Sparkitect.GameState.IStateModuleServiceConfigurator> |

## Service Lifetimes

All CoreContainer services are singletons: one instance per container, created during `Build()`. There is no transient or scoped lifetime.

Service lifetimes are tied to the state that created them. Root container services persist for the application lifetime. State container services live only as long as that state is active. When a state is destroyed, its container is disposed and all its services end.

Parent container services remain available to child containers without re-creation.

`IFactoryContainer<TKey, TBase>` itself owns its per-key factory instances and is disposed/rebuilt when the mod set changes — the registry's factory container, for example, is rebuilt by <xref:Sparkitect.Modding.IRegistryManager> on mod load/unload.

## Entrypoint System

Discoverable extension points the engine instantiates during container-construction phases. The substrate is the **EntrypointContainer**, produced by [`IDIService.CreateEntrypointContainer<T>(modIds)`](xref:Sparkitect.DI.IDIService) — a typed collection that performs no dependency resolution. It is kept separate from the IoC containers because discovery runs *during* IoC container construction, so the discovered instances cannot depend on the container they help build.

Three usage shapes ride on this substrate:

- **Bare entrypoints** — mods author classes implementing `IConfigurationEntrypoint<TDiscoveryAttribute>` directly. Examples: <xref:Sparkitect.Graphics.Vulkan.IVulkanInstanceConfigurator>, <xref:Sparkitect.GameState.IEntryStateSelector>.
- **SG-emitted entrypoints** — generators (Registry, StateModuleService, etc.) emit entrypoint classes in response to attributes like `[StateService]`, `[Registry]`, `[RegistryMethod]`. Mods do not write these directly.
- **Metadata entrypoints** — see the utility-infrastructure article (forthcoming).

The contract:

```csharp
public interface IConfigurationEntrypoint<TDiscoveryAttribute> : IBaseConfigurationEntrypoint
    where TDiscoveryAttribute : Attribute
{
    static Type IBaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}
```

Entrypoints must be non-abstract classes with a parameterless constructor — types that fail this requirement are logged and silently skipped. Dependencies arrive via the per-subsystem `Configure(...)` / `Select(...)` method parameters (e.g., [`ICoreContainerBuilder`](xref:Sparkitect.DI.ICoreContainerBuilder)), not constructor injection.

### Entrypoint Ordering

Two attribute families, applied on the entrypoint class itself:

- [`[EntrypointOrderAfter<T>]`](xref:Sparkitect.DI.Ordering.EntrypointOrderAfterAttribute`1) / [`[EntrypointOrderBefore<T>]`](xref:Sparkitect.DI.Ordering.EntrypointOrderBeforeAttribute`1) — compile-time type reference.
- `[EntrypointOrderAfter("Full.Type.Name")]` / `[EntrypointOrderBefore("Full.Type.Name")]` — string variants for cross-mod ordering when the target type lives in another mod's assembly.

**Treat ordering as a last-resort modding vector.** It is effectively a low-level injection API — you can insert behavior anywhere in the engine's startup graph — but error-prone in practice: string references silently no-op if the target isn't loaded, and ordering attributes do **not** propagate from `[StateService]`-decorated user classes to the SG-emitted configurator. Reach for it only when no higher-level attribute (`[StateService]`, `[Registry]`, …) fits. Everyday modding doesn't touch this.

For custom ordering rules beyond After/Before, implement <xref:Sparkitect.DI.Ordering.IEntrypointOrdering> on your own attribute class — same "last resort" framing applies.

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

### Keyed-Factory Generation for Registries

Type-registration registry methods can opt into source-generated keyed-factory exposure with [`[KeyedFactoryGenerationMarker<TBase>]`](xref:Sparkitect.Modding.KeyedFactoryGenerationMarkerAttribute`1) alongside `[RegistryMethod]`:

```csharp
[RegistryMethod]
[KeyedFactoryGenerationMarker<IDummyValueProvider>]
public void RegisterProvider<TProvider>(Identification id)
    where TProvider : class, IDummyValueProvider, IHasIdentification { /* ... */ }
```

The marker is restricted to the **type-registration** shape above. The Registry Generator emits a static `BuildRegister{Method}Container(IDIService di, ICoreContainer container, IResolutionProvider? provider, IEnumerable<string> modIds)` extension on the Registry class returning <xref:Sparkitect.DI.Container.IFactoryContainer`2>. A mod calls the generated extension to obtain the container, then resolves entries by <xref:Sparkitect.Modding.Identification>.

Diagnostics: `SPARK0260` rejects the marker on non-type-registration methods; a missing-constraint diagnostic enforces the `class, TBase, IHasIdentification` constraint set.

**Real-world usage** — `RenderGraph.Initialize` builds a pass factory and resolves passes by id:

```csharp
var passFactory = RenderPassRegistry.BuildRegisterPassContainer(
    diService,
    hostContainer,
    resolutionProvider,
    modIds);

if (!passFactory.TryResolve(passId, out var pass))
    throw new InvalidOperationException(
        $"No render pass factory resolved {passId} — DI binding missing.");
```

The call lives on the `RenderPassRegistry` *type* (.NET 10 extension-on-type), the consumer owns the container's lifetime, and pairs it with whatever <xref:Sparkitect.DI.Resolution.IResolutionProvider> matches its needs — `RenderGraph` uses its own `RenderGraphResolutionProvider`; plain DI consumers can pass `null`. This combination is the easy modding path: a typed registration plus the keyed-factory marker turns "register a type" into "instantiate it via DI with one call."

## Resolving Services

Factory containers are the DI surface intended for manual resolution. They appear in many places across the engine — the registry system holds its registries in one, individual features stand up their own for keyed instance pools, and the source generator emits one per typed registration that opts in.

For mods, the natural path is the keyed-factory generation on **typed registry methods**: a registration like `Register<TThing>(Identification id)` records `TThing` under `id`, and the keyed-factory marker exposes a `BuildRegister{Method}Container(...)` extension returning `IFactoryContainer<Identification, TBase>`. `TryResolve(id, out var thing)` constructs a `TThing` instance with full DI; the registration method body still runs for any custom bookkeeping. See [Keyed-Factory Generation](#keyed-factory-generation-for-registries) for the end-to-end example.

```csharp
// Resolve by key
if (factoryContainer.TryResolve(id, out var instance))
{
    // Use instance
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
