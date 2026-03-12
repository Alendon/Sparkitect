---
uid: sparkitect.ecs.ecs-requirements
title: ECS Requirements
description: Requirements for the Entity Component System architecture
---

# ECS Requirements

Requirements and design decisions for the Entity Component System (ECS) architecture.

## Design Philosophy

The ECS follows the same design philosophy as the rest of Sparkitect: the core defines minimal
interaction contracts, and concrete behavior emerges from implementations. Nothing is hardcoded
that can be extensible.

The same applies to how Stateless Functions work: the core provides `IStatelessFunction`,
`IScheduling`, and `ExecutionGraphBuilder`. Scheduling logic (PerFrame, OnCreate, etc.) is
emergent, not baked in.

The same pattern repeats throughout the ECS:

- **Component types** are emergent. The framework defines component identity. Concrete types
  (unmanaged structs, managed objects, etc.) bring their own registries and constraints.
- **Storage** is emergent. The framework defines the capability contract. Concrete storage
  backends implement it however they need to.
- **Queries** are emergent. The framework defines access metadata. Concrete query types define
  their own iteration and access patterns.
- **Entity identity** is emergent. The framework has no built-in identity scheme. Generational
  IDs, position-based identity, named entities, and other identity schemes are capabilities
  provided by emergent implementations, not framework requirements.
- **Capabilities** follow the Stateless Function pattern. The framework defines a generic
  contract shape. Each capability type brings its own accessor, filter, and resolution logic
  as a complete self-contained package.

The core is agnostic — multiple ECS implementation approaches can coexist, be benchmarked,
and be selected per use case. Tests and benchmarks profit from this design and can actively
monitor performance across implementations.

## Systems

Systems are Stateless Functions. A system is a static method with DI-injected parameters.

- Consistent with existing Sparkitect patterns.
- Enforces pure systems (no hidden state).
- Queries and command buffers are DI parameters on the system function.
- System parameters are resolved through the existing DI framework.
- A system may have zero, one, or many queries. Nothing restricts query count.
- Developer experience mirrors Stateless Functions: attributes + static method + DI params.

### System Groups

Systems belong to System Groups. Groups act as the parent/owner for systems (analogous to
how modules own Stateless Functions).

- Groups determine inclusion/activation of their systems.
- Activation is context-based when (re)building the execution graph.
- Runtime toggling of system groups may be supported (depends on scheduling performance).

### Ordering

System ordering uses Stateless Function scheduling (`OrderAfter`/`OrderBefore`,
`ExecutionGraphBuilder`). No new ordering mechanism needed.

### Parallelization

Parallelization is a Stateless Functions extension, not an ECS-specific feature. ECS systems
are SFs, so they benefit from general SF concurrency support.

- **Resource access protocol**: A new SF foundation feature, mirroring how `IScheduling`
  works. Each system has one resource access declaration type that encapsulates all resources
  the system touches (analogous to one scheduling type per system).
- **Auto-derivation from queries**: SG automatically derives resource access declarations
  from query attributes (`ReadComponents`, `WriteComponents`). Manual override attributes
  allow declaring additional resource access for non-query dependencies (e.g., shared services).
- **Generic resource model**: The SF resource access protocol is generic (not ECS-specific).
  It follows the same pattern as SF scheduling: foundation defines a generic contract shape,
  emergent implementations provide typed variants. ECS maps its concepts to this generic model.
- **Execution**: Stateless functions stay synchronous (`void Execute()`). They are dispatched
  to multiple threads by an emergent executor. The foundation provides the resource declaration
  framework and wires everything up; actual synchronization is emergent.
- **SG bridge**: Source generators produce resource access entrypoints (same pattern as
  `ApplySchedulingEntrypoint`), declaring per-system what resources are accessed and how.
- **Conflict detection**: Emergent scheduler implementations use resource declarations to
  determine which systems can run in parallel. The execution strategy (pre-computed waves,
  greedy dispatch, etc.) is an emergent choice.
- **Unresolved write conflicts**: Produce a runtime warning and default to serialization
  (build-time errors are impossible due to modding).

## Entity Identity

Entity identity is fully emergent. The foundational ECS core does not define any identity
scheme. Different entity types have different identity needs, and identity is provided by
capabilities, not the framework.

Examples of emergent identity schemes:

- **Generational IDs** for entities that need cross-storage references: u32 index + u32
  generation packed into u64. Cheap validity checking and slot recycling.
- **Position-based identity** for entities identified by their in-world position (e.g.,
  block entities in a Minecraft-like game). A fully separate identity approach that must
  also be supported.
- **No identity** for short-lived entities (particles, projectiles) that exist only within
  their storage and are created to be deleted shortly after.

The World or emergent implementations maintain lookup tables mapping global identifiers to
storage-internal entities. To have a generational ID, an entity must live in a storage with
the matching capability. Identity is a capability, not a framework concern.

### Entity Creation Return Type

Entity creation return type is conditional on identity capability:

- If the archetype includes a generational ID capability, the SG-generated create function
  returns the allocated ID (allocated at scheduling/deferred time).
- If no identity capability, the create function returns nothing (void).
- Identity is a capability concern, not a creation concern.

### Storage-Internal IDs

Each storage owns its own internal ID space, optimized for its data layout. Internal IDs are
unstable and may change when the storage reorganizes. Storage accessors work with internal IDs
on the hot path, avoiding identity lookup costs during iteration.

The stock emergent implementation uses swap-and-pop slot management: dense arrays where
the last entity fills gaps on destruction. Best iteration performance, but changes indices.

### Cross-Storage References

Cross-storage relationships can use either path:

- Global identity (generational IDs or other schemes) for stable universal references,
  requiring a lookup.
- Direct internal ID references for fast intra-storage access, requiring automatic reference
  updates when internal IDs change.

Bulk relationships (many children of one parent, for example) typically target the same storage,
making direct internal references the common fast path.

## Archetypes

Archetypes are blueprints, not runtime containers. They define the shape of an entity
(which components it has) for creation and source generation of typed helpers. They are not
a runtime organizational structure.

- Foundation defines the empty archetype object structure (definition).
- Emergent layer populates properties (declaration) — e.g., the list of component IDs.
- Archetypes have no direct links to other foundation features — all links are emergent.
- No automatic archetype-to-archetype migration at runtime. Entities are owned by storages.
- Archetype tracking on live entities is optional, for debugging validation only.
- Entity mutation and storage moves are emergent features (e.g., via command buffer).

### Archetype Declaration

- Attribute-based, SG-generated, exposed through entrypoints.
- Main registration trigger is type registration.
- Declaration shape is emergent-dependent and may vary, but should be SG-based.
- For the stock implementation, archetypes define a list of component IDs.
- In the stock implementation, storage instances map 1:1 with archetypes (one storage per
  component set shape). The foundation does not enforce this — other implementations may
  hold entities of varying shapes in a single storage.

## World

The World is a coordinator. It is standalone infrastructure, independent of GameState, that
can be created, ticked, and destroyed by any code.

The World coordinates but does not own. It does not own entity data, entity identity, or
storage internals. It is a mediator between the different ECS portions.

- Multiple worlds are possible.
- GameState integration is optional, not structural.
- The World mediates capability discovery: queries ask the World for matching capabilities,
  the World returns a list of all fitting capability instances.
- The World coordinates storage lifecycle. Storages can be added and removed dynamically
  during World operation.
- Entity creation is delegated: the foundational entity creation call requires an explicit
  storage reference (e.g., `CreateEntity(StorageHandle, ...)`). Higher-level ergonomic
  interfaces (component-set routing, storage IDs, etc.) are emergent.
- The World proactively manages capability resolution. It knows all registered filters and
  can announce or update when storage topology changes.
- The World exposes multiple access levels for capability discovery at the foundation level,
  supporting both typed queries for common use and lower-level access for advanced needs.

The foundation does not define a specific execution model (e.g., `Tick()`). Different World
implementations may have multiple tick functions or other execution strategies. The World
conceptually owns execution, but the concrete shape is emergent.

### Entity Allocation

The foundation storage contract includes entity allocation as a minimal lifecycle method.
Allocation creates an empty slot — the foundation is data-shape-agnostic. To track the
internally created entity, a pointer callback (raw ref pointer) is passed in, where the
storage writes back its internal key. The foundation does not know the shape of this key.

The foundation should guard against stack corruption from mismatched pointer types. However,
entity creation can also be fully implemented through emergent capabilities — nothing forces
use of the World's `CreateEntity`. Capabilities can be generic (encoding the storage key
type) or provide creation functions with population information.

### DI Integration

No DI refactoring is needed for ECS integration. The existing immutable container model works:

- Queries take World as a DI parameter (World registered in state container).
- At construction, queries register their filter (and optional topology-change callback) with
  the World.
- World resolves capabilities against filters when storages are added or removed.
- Capabilities never enter DI — they live in the World's internal registry.
- The immutable container constraint is irrelevant because capability resolution is World-internal.

## Structural Changes

Structural changes (create/destroy entities, add/remove components) are always deferred.
No immediate structural changes during system execution.

- Deferred mutation is a foundational principle, not an implementation detail.
- Concrete command buffer implementations are emergent. Different buffer strategies may exist
  for different mutation patterns. The foundation establishes the principle; implementations
  provide the mechanics.
- Buffer execution may affect more than a single storage (e.g., moving an entity when its
  component configuration changes).
- Mutation routing relies on filters: filters describe the exact flavour of a generalized
  capability, enabling the correct storage/capability resolution for the mutation.
- Buffer playback timing is controlled by the emergent execution model (tick implementation).
  The foundation does not prescribe when buffers flush.
- The foundation does not restrict immediate vs deferred creation. It provides both immediate
  (via storage) and deferred (via buffer) paths. Whether to enforce restrictions is emergent
  policy, though the foundation may expose hooks to enable such restrictions.

## Component Queries

Queries are the interaction point between systems and capabilities.

### Core Contract

The core query contract is minimal: access metadata only.

- Declares which components are accessed and whether read or write.
- Components identified by Identification (not CLR types).
- No iteration contract, no execution contract in the core.
- All iteration and access patterns are defined by query implementations.

### Query Declaration

Queries are declared as separate partial types, extended by source generators. The partial
type with its attributes clearly communicates "I am a SG declaration, not a runtime thing
you interact with directly."

Each query type has a **strategy attribute** (`[ComponentQuery]`, `[SpatialQuery]`, etc.)
that determines which SG processes it. Component access attributes are shared across query
strategies; each strategy adds its own configuration attributes.

Component access is declared via grouped, generic attributes:

- `[ReadComponents<T1, T2, ...>]` — generates read-only accessors on entity handle.
- `[WriteComponents<T1, T2, ...>]` — generates read-write accessors on entity handle.
- `[ExcludeComponents<T1, T2, ...>]` — filters out entities with these components (no accessors).

Each attribute has hand-written arity variants for 1 through 9 generic type parameters.
Multiple attributes of the same type can be stacked and are merged by the SG to exceed the
arity limit.

```csharp
[ComponentQuery]
[WriteComponents<Position>]
[ReadComponents<Velocity, Mass>]
[ExcludeComponents<Dead>]
partial class MoveQuery;
```

For spatial queries (reusing component access attributes):

```csharp
[SpatialQuery]
[ReadComponents<Health, Armor>]
[SpatialRange(10f)]
partial class NearbyUnitsQuery;
```

### Query Lifecycle

- Queries are DI parameters, taking World via DI.
- At construction, the query registers its filter (and topology-change callback) with the World.
- Capability resolution happens later, when the World has storages.
- World notifies queries when topology changes (storage added/removed matching filter).
- Queries cache resolved capabilities after notification.

### Entity Handle

The SG-generated entity handle provides typed component accessors:

- Accessor method names match component type name exactly: `GetPosition()`, `GetVelocity()`.
- `WriteComponents` generates `ref T` return (read-write access).
- `ReadComponents` generates `ref readonly T` or `T` (value copy), decided by SG based on
  struct size (16-byte boundary). For pure data structs (no methods), `ref readonly` is safe
  regardless of `readonly struct` modifier — field access does not trigger defensive copies.

### Iteration

Entity-by-entity enumerator pattern:

```csharp
foreach (var entity in query)
{
    ref var pos = ref entity.GetPosition();
    var vel = entity.GetVelocity();
    pos.X += vel.X * dt;
}
```

Internally, the capability exposes chunk-based data access:

```csharp
Span<TComponent> GetNextChunk<TComponent>(ref ChunkHandle handle)
    where TComponent : ..., IHasIdentification
```

The query SG generates code that calls `GetNextChunk<Position>()`, `GetNextChunk<Velocity>()`
to get typed spans. The enumerator manages chunk transitions internally (calls GetNextChunk
when current span is exhausted). Component types self-identify via
`static Identification IHasIdentification.Identification`.

### Source Generation

Query SG lives in the existing Sparkitect.Generator project (same assembly as SF generator),
logically separated by namespace. It generates:

- Entity handle type with typed accessor methods.
- Enumerator yielding entity handles.
- Filter with component IDs and access modes.
- Capability resolution logic.
- Resource access metadata entrypoint (for SF concurrency extension).

Each emergent query type brings its own SG that reads its declaration format, generates the
query implementation, AND emits access metadata for the SF concurrency scheduler. The common
contract is: each query type's SG must emit access metadata.

## Storage

### Black Box with Capability Interfaces

The core defines no fixed storage approach. A storage is a black box. The framework interacts
with it exclusively through capability interfaces. Everything behind those interfaces is an
implementation detail.

Concretely:

- The framework makes no assumptions about storage internals.
- A storage could be a monolithic optimized data structure, a composition of capability
  providers, a startup-compiled specialized type, or anything else.
- Capability interfaces are the only external contract.

Different storage approaches (archetype-based, sparse sets, spatial-indexed, graph-based,
GPU-backed, etc.) require no core changes.

### Storage Contract

The foundation storage contract (IStorage) includes minimal lifecycle methods:

- Entity allocation (empty slot creation).
- Dispose.

These are the minimal methods the World needs to manage storage lifecycle. Everything else
is expressed through capabilities.

### One Entity = One Storage

At the foundation level, entity creation targets a single storage via explicit storage
reference. The foundation exposes a single-storage create function at the core. This is
a mechanical constraint of the creation API, not an enforced policy. The framework does
not have machinery to prevent an entity from appearing in multiple storages, but the
intended design is one entity per storage.

### Storage Capabilities

Capabilities follow the same layering pattern as Stateless Functions. The framework defines
a generic capability contract shape (analogous to `IScheduling<TFunction, TContext, TRegistry>`).
Each capability type brings its own complete package: accessor type, filter type, and
resolution logic.

**Discovery:** Filter-driven resolution. Filters resolve themselves against the World's
capability index. The World provides the capability index; filters know how to find what
they need. Storages don't need to announce metadata — filters handle resolution.

**Registration:** Capabilities register themselves (their defining interfaces), not storages.
Storages are an implementation detail behind capabilities. For specific entity types where
heavily optimized storage implementations are desired, storage registration may be used
for direct wiring. But generally, storages are not exposed.

**Component-agnostic:** The foundational capability contract is not aware of components or
Identifications. What a capability exposes is entirely up to its implementation. A capability
might work with component data, spatial coordinates, relationship graphs, or functionality
that has nothing to do with components.

**Hot-path optimization:** How a capability hands off data to queries is an emergent choice.
The foundation does not mandate batch access, span returns, or any specific data shape.
The stock implementation uses chunk-based `Span<T>` returns via `GetNextChunk<T>()`.

### Specialized Pairings

Optimized query + accessor pairings can exist for specific storage types. Source generation
can produce these for compile-time known combinations. Runtime/mod-defined types use the
general capability path.

### Stock Emergent Implementation

The first implementation uses SoA columnar layout with NativeMemory-backed columns:

- One storage instance per archetype (1:1 mapping). Foundation does not enforce this.
- Swap-and-pop slot management: dense arrays, last entity fills gaps on destruction.
- Each component type gets a contiguous column indexed by slot.
- Chunked iteration accessor for smarter locking and better concurrency.

## Source Generation

Source generation plays the same bridging role in the ECS as it does in Stateless Functions.
It reads declarations and generates the typed bridge code that connects developer intent to
the capability protocol.

Source generation is a large portion of the emergent implementation and is seen as equally
important to the remaining emergent code. Much of the emergent implementation will be fully
done through source generators to lower boilerplate, improve developer experience, enable
performance optimizations, and keep the system open and flexible. Others can manually write
their own variants or source generators for their specific needs.

### Entity Creation

SG produces typed creation methods from archetype declarations, auto-routing to the correct
storage instance. The return type is conditional on identity capability (see Entity Identity).

## Component Identity

Components are data identified by Sparkitect's Identification system (`mod:category:item`).
Consistent with the rest of the engine, naturally supports mod-defined components. Component
types implement `IHasIdentification`, providing their Identification via the static interface
member — the same pattern used throughout the engine.

Component identity is defined at the framework level as a political choice: it sets a
concrete and sensible cornerstone even though there is currently no strict technical
requirement forcing it. At the foundation level, component identity is deliberately
**unwired** from other ECS foundation parts (capabilities, storages, World). The wiring
happens at the emergent level.

### No Framework Type Restrictions

The framework imposes no restrictions on the CLR type of a component. A component is data
with an Identification. Type constraints (unmanaged structs, blittable types, etc.) are a
storage concern, not a framework concern. A storage that needs unmanaged structs for SoA
layout enforces that. A storage that handles managed references is free to accept them.

### Emergent Component Types

Concrete component types (e.g., `UnmanagedComponentValue`, `ManagedComponent`,
`SpatialComponent`) are emergent features. They bring their own registries, validation, and
type constraints. Same pattern as how Stateless Function scheduling types bring their own
registration. The framework does not enforce a single component registry.

### Component Registration

Each emergent component type has its own classic registry (e.g., `UnmanagedComponentRegistry`).
Components register through type-specific registries. This follows the standard engine
registry pattern.

## Not Yet Decided

- Minimal capability contract surface (will be determined backwards from emergent
  implementation requirements).
- Concrete command buffer contract shape (leaning emergent, principle established).
- SF resource access protocol concrete interface shape (follows SF scheduling pattern).
- Parallel execution strategy (pre-computed waves, greedy dispatch, etc.) — emergent choice.
- Relationship and spatial query implementation details.
- System Group activation mechanism specifics.
