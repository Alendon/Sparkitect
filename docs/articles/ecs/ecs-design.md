---
uid: sparkitect.ecs.ecs-design
title: ECS Design Model
description: Semantic model of the ECS framework covering core contracts, emergent features, and their relationships
---

# ECS Design Model

This document models the ECS as a layered system. The framework core defines minimal structural
contracts — zero-policy scaffolding. Emergent features build on top, providing all concrete
behavior. This is the textual prototype of the ECS, modeling the design before any code is
written.

> Each section states what layer it belongs to and what contracts it uses or provides. "Core"
> means it ships with the framework. "Emergent" means it is a concrete implementation that
> plugs into core contracts.

## Layer Overview

```
┌─────────────────────────────────────────────────────┐
│                  Game / Mod Code                    │
│         Systems, entity creation, queries           │
├─────────────────────────────────────────────────────┤
│              Emergent Features                      │
│   Component Storage, Spatial Storage, Relationships │
│   Generational IDs, Command Buffers, Tick Model,    │
│   Component Queries, Source-Generated Bridges,      │
│   Concurrent Scheduling, Parallel Executor, ...     │
├─────────────────────────────────────────────────────┤
│              Framework Core (Zero-Policy)           │
│   World (coordinator), Storage Contract,            │
│   Capability Contract, Component Identity           │
├─────────────────────────────────────────────────────┤
│           Existing Engine Infrastructure            │
│   Stateless Functions, DI, Registry,                │
│   Identification, Scheduling (per-category),        │
│   Source Generation                                 │
└─────────────────────────────────────────────────────┘
```

The framework core is deliberately smaller than a typical ECS. Entity identity, command
buffers, execution models, and iteration strategies are all emergent. The core defines only
the coordination protocol and extension points.

The existing Stateless Functions scheduling infrastructure is extended with per-category
graph builders, enabling concurrent execution for SF categories that need it (like ECS
systems). This is not ECS-specific — any SF category can provide its own graph builder
and scheduler pair.

---

## Framework Core

The framework core is the minimal set of contracts that all emergent features depend on.
It defines coordination and extension points. It never defines concrete data layouts,
iteration strategies, identity schemes, or behavioral policy.

The core follows the same design mentality as Stateless Functions: establish a type-safe
protocol via generics, refuse to encode specific behavior, and let emergent implementations
fill in all actual decisions. The foundation never changes when new emergent types are added.

### World

**Layer:** Core
**Provides:** Storage lifecycle coordination, capability discovery mediation, entity creation
delegation
**Uses:** DI (registered in state container, injected into queries)

The World is a coordinator, independent of GameState. It can be created and destroyed by
any code. Multiple worlds are possible.

Responsibilities:

- Mediates capability discovery: queries register filters with the World, World resolves
  capabilities against filters when storage topology changes, and notifies queries.
- Coordinates storage lifecycle. Storages can be added and removed dynamically.
- Delegates entity creation: `CreateEntity(StorageHandle, ...)` routes to a specific storage.
  The foundation provides empty slot allocation; the storage writes back its internal key
  via a pointer callback.
- Proactively manages capability resolution: knows all registered filters, announces or
  updates when storage topology changes.
- Exposes multiple access levels for capability discovery (typed queries for common use,
  lower-level access for advanced needs).

Not responsible for:

- Entity identity (emergent capability concern).
- Storage internals (storages manage their own data and internal IDs).
- Component type validation (storage concern).
- Query iteration (query implementation concern).
- Execution model specifics (concrete `Tick()` shape is emergent).
- Mutation playback (command buffer design is emergent).
- Behavioral policy of any kind.

The World coordinates but does not own. Entity data, entity identity, and storage internals
all live in their respective implementations. The World is a mediator.

### Storage Contract

**Layer:** Core
**Provides:** Extension point for data backends, minimal lifecycle
**Uses:** Capability contract (exposes capabilities)

A storage is a black box to the framework. The only external interface is through capability
contracts plus minimal lifecycle methods.

What the contract defines:

- A storage holds entities and their data.
- Entity creation targets a single storage via explicit storage reference.
- A storage exposes capabilities that queries can match against.
- A storage manages its own internal IDs, locking, and data layout.
- Storages can be added and removed dynamically during World operation.
- Minimal lifecycle: entity allocation (empty slot) and Dispose.

What the contract does not define:

- Memory layout (SoA, AoS, sparse, etc.).
- Internal ID scheme.
- How capabilities are composed internally.
- Whether the storage is monolithic, composed, or compiled.

Internal implementation is explicitly unconstrained. A storage could be a hand-optimized
monolithic type, a composition of capability providers, or a startup-compiled specialization.
The aspiration is to eventually support compilation/composition approaches that combine
monolithic performance with compositional flexibility.

### Capability Contract

**Layer:** Core (contract pattern), specific capabilities are emergent
**Provides:** Generic extension protocol for storage features
**Uses:** Storage (implements capabilities), World (discovery mediation)

The capability contract follows the same layering pattern as Stateless Functions. The
framework defines a non-generic base marker (`ICapability`); each emergent capability type
defines its own interface extending the marker with typed methods and interaction points.
The capability interface itself defines the shape and all interaction points — there is no
separate accessor concept.

**Identity:** Capabilities are identified by their closed generic CLR interface type.
`IComponentIteration<Position>` and `IComponentIteration<Velocity>` are distinct
capabilities. The CLR enforces uniqueness — a storage cannot implement the same closed
generic interface twice.

**Metadata:** Each capability has one readonly metadata instance per storage, supplied at
storage registration time alongside the storage reference. Metadata is not part of the
capability interface — it is registration-time data held by World. The foundation defines
`ICapabilityMetadata` as a non-generic marker; emergent implementations define concrete
metadata shapes (e.g., what component IDs are available, what spatial regions are indexed).

**Filtering:** The foundation provides the coordination protocol: register filter
requirements, coordinate matching, notify consumers. The emergent layer owns the shapes —
what metadata looks like and what matching means for each capability type.

- Queries register a composite filter (list of `ICapabilityRequirement`) with World at
  construction. Each requirement targets one capability type and defines matching logic
  against that capability's metadata.
- World handles intersection — a storage matches only when all requirements pass.
- The requirement contract uses `ICapabilityRequirement<TMeta>` (generic, typed matching)
  with a non-generic `ICapabilityRequirement` base for runtime collections, following the
  same `IConfigurationEntrypoint<T>` pattern used elsewhere in the engine.
- Filter conditions are expressive per capability type: "must have this capability",
  "must have this capability with these properties", "must have this capability without
  these properties".

**Discovery flow:**

1. Query registers its composite filter and topology-change callback with World.
2. When storage topology changes, World checks registered filters against the storage's
   capabilities and notifies matching queries.
3. Queries cache resolved capability instances. On the hot path, queries interact directly
   with cached capability interfaces — no World interaction, no resolution overhead.

World also exposes pull-based capability lookup for non-query ad-hoc usage.

**Component-agnostic:** The foundational capability contract knows nothing about components,
Identifications, or data shapes. What a capability exposes is entirely up to its emergent
implementation. A capability might work with component data, spatial coordinates, relationship
graphs, or functionality unrelated to components.

### Query Contract

**Layer:** Core
**Provides:** Access metadata declaration, capability matching
**Uses:** Capability contract (discovery via filter), DI (parameter binding via World)

The core query contract is minimal: access metadata only.

- Declares which components are accessed and whether read or write.
- Components identified by Identification (not CLR types).
- No iteration contract, no execution contract in the core.
- Bound to a World instance via DI. At construction, registers filter with World.
- Capability resolution is deferred: filter + callback registered at construction,
  capabilities resolved when World notifies of topology changes.

Concrete query types are emergent features that define their own iteration patterns, entity
handle types, and access strategies. Queries are source-generated per system: the generated
query provides the enumerator and entity handle specifically required by that system.

Each query type's SG emits concurrency descriptions for the ECS scheduling infrastructure,
bridging component access declarations into the category-specific graph builder.

### Component Identity

**Layer:** Core (political choice)
**Provides:** Component identification via Identification (`mod:category:item`)
**Uses:** Identification system, Registry (for emergent component types)

Component identity is defined at the framework level as a political choice: it sets a
concrete and sensible cornerstone for the ECS even though there is no strict technical
requirement forcing it at the foundation level.

At the foundation level, component identity is deliberately **unwired** from other ECS
foundation parts. Capabilities do not know about components. Storages do not know about
components. The World does not know about components. All wiring between components and
the rest of the ECS happens at the emergent level.

- A component is data with an Identification. Nothing more at the foundation level.
- Component types implement `IHasIdentification` for their Identification.
- No CLR type restrictions from the framework.
- Type constraints are a storage concern.
- Component registries are a component-type concern (emergent) — each emergent component
  type has its own classic registry.

---

## Engine Infrastructure Extension: Per-Category Scheduling

**Layer:** Existing Engine Infrastructure (Stateless Functions extension)
**Provides:** Abstracted graph builder and scheduler per SF category
**Uses:** Existing SF scheduling infrastructure (extending, not replacing)

Concurrent execution is achieved by extending the existing scheduling infrastructure, not
by adding a parallel resource access protocol. Each SF category gets its own graph builder
and scheduler pair, enabling category-specific execution strategies.

### Current State

The current scheduling infrastructure uses a single `IExecutionGraphBuilder` with
`AddNode`/`AddEdge`/`Resolve()` that produces a flat topologically sorted list. This is
sufficient for sequential execution (PerFrame, Transition functions).

### Extension

The graph builder becomes fully generic per SF category — no shared base API. The current
builder becomes one concrete implementation for existing sequential SF categories. New SF
categories (like ECS systems) define their own builder interface with whatever methods their
scheduling logic requires.

`StatelessFunctionManager` is refactored to separate shared SF lifecycle helpers (wrapper
registration, entrypoint container creation, function instantiation) from category-specific
graph building and resolution. Each SF category controls its own builder creation and plan
consumption.

### Per-Category Builder + Scheduler

Each SF category provides two components:

- **Graph builder:** Enforces structural constraints and hard rules. Limits complexity
  that the scheduler has to deal with. For concurrent categories, accepts concurrency
  descriptions (opaque resource keys, access modes, consumer-defined conflict function)
  alongside ordering constraints.
- **Scheduler:** Consumes the builder's output and applies its execution strategy. For
  simple categories, this produces a flat execution list. For concurrent categories, this
  produces wave-partitioned execution with parallelism within waves.

The split between builder and scheduler determines where complexity lives. For simple SF
categories, most logic stays in the scheduler. For concurrent categories, the builder
preprocesses more to keep the scheduler's job tractable.

### Concurrency Model

The foundation does not define what "access" or "conflict" means. Each SF category's
builder defines its own access modes and conflict semantics. A concurrent graph builder
accepts per-system concurrency descriptions and a conflict function, then produces an
execution plan that the scheduler can consume.

Concurrency configurations are precomputed and reused — not evaluated every frame.
Topology changes (new storages, new queries) invalidate the configuration and trigger a
rebuild. Bitset-based conflict detection keeps rebuild cost negligible for typical system
counts.

### Prior Art: MintyCore

The predecessor engine (Alendon/MintyCore) used a greedy task-based model:

- Systems dispatched via `Task.ContinueWith` as soon as ordering + resource constraints
  clear.
- Per-component `(AccessType, Task)` tracking: readers depend on current writer, writers
  depend on all current accessors. Multiple-reader / single-writer pattern.
- System groups as hierarchical: `ASystemGroup : ASystem` with recursive scheduling.

In Sparkitect, this approach moves to the emergent layer. The per-category scheduling
extension provides the structural framework, and the emergent executor implements the
dispatch strategy.

---

## Emergent Features

Emergent features are concrete implementations built on framework core contracts. They are
categorized by how tightly they couple to the core.

### Emergence Levels

| Level | Description | Example |
|-------|-------------|---------|
| Foundation | Ships with the framework, uses core contracts, could theoretically be replaced | Component Storage, Component Query, Generational IDs, Command Buffer |
| Extension | Adds new capabilities on top of framework, plugs into existing contracts | Relationship Storage, Spatial Storage, Position-Based Identity |
| Peripheral | Uses the framework but does not extend its contracts, self-contained | Debug Inspector, Entity Name Component |

### Foundation: Archetypes (Blueprints)

**Emergence:** Foundation
**Design status:** Designed

Archetypes are blueprints, not runtime containers. They define the shape of an entity
(which components it has) for creation and source generation of typed helpers.

- Foundation defines the empty archetype object structure (definition).
- Emergent layer populates properties (declaration) — e.g., the list of component IDs.
- Archetypes have no direct links to foundation features — all links are emergent.
- Attribute-based declaration, SG-generated, exposed through entrypoints.
- Main registration trigger is type registration.
- No automatic archetype-to-archetype migration at runtime. Entities are owned by storages.
- Archetype tracking on live entities is optional (debugging validation only).

### Foundation: SoA Component Storage

**Emergence:** Foundation (the reference storage implementation)
**Implements:** Storage Contract, ComponentIteration capability (+ others TBD)
**Design status:** In progress

SoA columnar layout with NativeMemory-backed columns. One storage instance per archetype
(1:1 mapping). The foundation does not enforce 1:1 — other implementations may differ.

Provides:

- Component iteration capability: iterate entities matching a component set.
- SG-generated entity creation routed from archetype declarations.
- Swap-and-pop slot management: dense arrays, last entity fills gaps on destruction.

Data access via chunk-based capability method:

```csharp
Span<TComponent> GetNextChunk<TComponent>(ref ChunkHandle handle)
    where TComponent : ..., IHasIdentification
```

Emergent abstractions auto-instantiate storage instances on world creation (or similar
trigger) and route entity creation calls appropriately.

Open questions:

- Column memory management (NativeMemory, pooling, growth strategy).
- Chunk size for cache-friendly iteration.

### Foundation: Component Query

**Emergence:** Foundation (the reference query implementation)
**Implements:** Query Contract
**Uses:** ComponentIteration capability from storage
**Design status:** In progress

Iterates all entities across storages that have specific components. This is NOT an
"archetype query" — the storage's internal organization (archetype-based, sparse, etc.)
is invisible to the query.

**Declaration:**

```csharp
[ComponentQuery]
[WriteComponents<Position>]
[ReadComponents<Velocity, Mass>]
[ExcludeComponents<Dead>]
partial class MoveQuery;
```

- `[ComponentQuery]` — strategy attribute, determines which SG processes this query.
- `[ReadComponents<T1, ..., T9>]` — generates read-only accessors on entity handle.
- `[WriteComponents<T1, ..., T9>]` — generates read-write accessors on entity handle.
- `[ExcludeComponents<T1, ..., T9>]` — filters entities, no accessors generated.
- Hand-written 1-9 arity variants per attribute type (27 classes total).
- Stackable: multiple attributes of the same type merge.
- Strategy-specific attributes reusable across query types.

**Entity Handle:**

SG-generated per query. Typed accessor methods match component type name exactly:

- `WriteComponents` → `ref T GetPosition()` (read-write).
- `ReadComponents` → `ref readonly T GetVelocity()` or `T GetVelocity()` (SG decides
  based on struct size, 16-byte boundary).

**Iteration:**

Entity-by-entity enumerator pattern:

```csharp
foreach (var entity in query)
{
    ref var pos = ref entity.GetPosition();
    var vel = entity.GetVelocity();
    pos.X += vel.X * dt;
}
```

Enumerator internally manages chunk transitions: calls `GetNextChunk<T>()` when current
span is exhausted. The query interacts with the ComponentIteration capability to fetch
entity batch locations. 1-to-few virtualized calls per batch, then raw processing.

**SG generates:**

- Entity handle type with typed accessor methods.
- Enumerator yielding entity handles (manages chunk transitions).
- Filter with component IDs and access modes.
- Capability resolution logic (filter.Resolve(world)).
- Resource access metadata entrypoint (for SF concurrency extension).

### Foundation: Generational Entity IDs

**Emergence:** Foundation (the reference identity implementation)
**Implements:** An identity capability on the storage contract
**Design status:** Designed, implementation pending

One identity scheme among many. Generational IDs provide stable cross-storage entity
references with validity checking. u32 index + u32 generation packed into u64, allocated
and recycled via free list.

Generational IDs are opt-in. To have a generational ID, an entity must live in a storage
with the matching identity capability. Entities that don't need cross-storage references
(particles, short-lived effects) never allocate a generational slot.

The lookup table (generational ID to storage-internal entity) is maintained by the World
or by the identity capability implementation.

ID allocation follows the same deferred-mutation discipline as all structural changes.
SG-generated create functions return the allocated ID when the archetype includes the
generational ID capability; otherwise the create function returns void.

### Foundation: Command Buffer

**Emergence:** Foundation (fully emergent — no foundation contract)
**Design status:** Emergent design established

Command buffers are built on top of storage capabilities with typed interaction points.
The foundation does not define a command buffer contract and does not enforce deferred
mutation. Concrete command buffer implementations are emergent, built against the
capability interfaces that storages expose.

Base capabilities for component operations expose mutation methods keyed by the storage's
internal key type (encoded in the storage type hierarchy). Command buffers interact with
these capability methods, not with storages directly.

The stock emergent implementation uses a session-based API: begin a session targeting one
entity (existing or newly created), record operations against that entity, then end the
session. The buffer is storage-specialized via generics — it knows the key type and
capability surface of its target storage. No entity handles or references are needed since
all operations within a session are scoped to the current entity.

Buffer playback timing is controlled by the emergent execution model. The foundation does
not prescribe when buffers flush. No internal enforcement currently prevents direct storage
access during system execution — protection will be added based on what the prototype
reveals.

### Foundation: Source-Generated Bridges

**Emergence:** Foundation (critical infrastructure)
**Role:** Same bridging role as SF source generation
**Location:** Sparkitect.Generator project (same assembly, logically separated by namespace)

Source generation bridges developer experience to the capability protocol, just as SF source
generation bridges system declarations to `IStatelessFunction` and `IScheduling`. It reads
query/system declarations and generates the typed bridge code.

Source generation is a large portion of the emergent implementation. Much of the emergent
ECS will be fully done through source generators to lower boilerplate, improve developer
experience, enable performance optimizations, and keep the system open. Others can manually
write their own variants or source generators for their specific needs.

### Extension: Position-Based Entity Identity

**Emergence:** Extension (alternative identity scheme)
**Implements:** An identity capability
**Design status:** Conceptual

For entities identified by their in-world position rather than a generational index. Block
entities in a Minecraft-like game are the motivating example: a large number of entities
that don't need generational IDs but need identification by spatial coordinates. A fully
separate identity approach from generational IDs, demonstrating that identity is a
capability, not a framework concern.

### Extension: Relationship Storage

**Emergence:** Extension (adds relationship capability)
**Implements:** Storage Contract, RelationshipTraversal capability (TBD)
**Design status:** Not started
**Uses:** Identity capabilities for relationship targets

Entity relationships (ChildOf, IsA, custom pairs). A storage that understands directed
entity-to-entity links with optional relationship data.

Relationships within the same storage can use direct internal ID references for performance,
with automatic updates on ID changes. Cross-storage relationships use whatever identity
scheme is available.
Bulk relationships (many children of one parent) typically target the same storage, making
internal references the common fast path.

### Extension: Spatial Storage

**Emergence:** Extension (adds spatial capability)
**Implements:** Storage Contract, SpatialRange capability (TBD)
**Design status:** Not started

A storage that indexes entities by spatial position, supporting range/proximity queries.
The spatial index is internal to the storage. The framework sees it only through the
SpatialRange capability.

### Peripheral: Named Entity Component

**Emergence:** Peripheral (ordinary component data)
**Implements:** Nothing; it is a component like any other
**Uses:** Identification system, component identity

An Identification value stored as a component on an entity. Provides human-readable names
for entities that need them (the player, a specific NPC). The framework has no awareness
of this. It is emergent game-level convention.

---

## Integration Points

How the layers connect in practice.

### Query declares filter, World resolves capabilities, data flows

```
[System: MoveEntities]
  Parameter: MoveQuery (source-generated partial class)
    ↓ [ComponentQuery] [WriteComponents<Position>] [ReadComponents<Velocity>]
    ↓ at construction: registers filter with World via DI
    ↓ World notifies when matching storages exist
    ↓ filter.Resolve(world) → matching capability instances
      ↓ per capability: GetNextChunk<Position>() → Span<Position>
      ↓ per capability: GetNextChunk<Velocity>() → Span<Velocity>
        ↓ enumerator walks spans: entity.GetPosition() → ref Position
        ↓ mass processing within span, no virtualization
```

### Entity creation delegates to storage

```
[Archetype: PlayerArchetype]
  [Components: Position, Velocity, Health]
  [Identity: GenerationalId]
  ↓ SG generates: CreatePlayerEntity() → EntityId (because GenerationalId capability)

[System: SpawnPlayer]
  Parameter: CommandBuffer (emergent implementation)
    ↓ records: CreatePlayerEntity(position, velocity, health)
    ↓ deferred until between system executions

[Emergent Tick — between systems]
  ↓ plays back command buffer (emergent timing)
    ↓ routes to correct storage via archetype → storage mapping
      ↓ storage allocates slot, stores data
    ↓ generational ID allocated via identity capability
    ↓ ID returned to caller via deferred callback
```

### Per-category scheduling enables parallelism

```
[System A: MoveEntities — writes Position, reads Velocity]
[System B: RenderEntities — reads Position, reads Sprite]
[System C: ApplyGravity — writes Velocity]

ECS graph builder receives concurrency descriptions:
  System A: writes(Position), reads(Velocity)
  System B: reads(Position), reads(Sprite)
  System C: writes(Velocity)

ECS scheduler resolves (using category-defined conflict function):
  A conflicts with B (both access Position, A writes)
  A conflicts with C (both access Velocity, opposite modes)
  B and C don't conflict → can run in parallel

Execution (one possible strategy):
  Wave 1: [A]              — runs alone
  Wave 2: [B, C]           — run in parallel
```

### Cross-storage reference (future)

```
[Storage A: entity X has relationship → target entity Y in Storage B]
  Option 1: target = generational ID → identity capability lookup → Storage B → data
  Option 2: target = position-based ID → spatial lookup → Storage B → data
  Option 3: target = Storage B internal ID (direct) → data
    ↓ requires: reference update when Storage B reorganizes
    ↓ update mechanism: same deferred-mutation pattern as all structural changes
```

---

## Open Design Questions

Tracked here, resolved as design progresses. Each question notes which layer and feature
it affects.

### Active

| Question | Layer | Feature | Notes |
|----------|-------|---------|-------|
| Parallel execution strategy | Emergent | Parallel Executor | Fully emergent (DetermineNext logic). MintyCore used greedy task-based; Sparkitect can reuse or try alternatives |
| Column memory management | Emergent | Component Storage | NativeMemory, pooling, growth strategy |
| Chunk size for iteration | Emergent | Component Storage | Cache-friendly iteration |
| Relationship storage design | Extension | Relationship Storage | Pair components, cascading, directed links |
| Spatial indexing strategy | Extension | Spatial Storage | Data structure, update patterns, query shapes |

### Resolved

| Question | Layer | Resolution |
|----------|-------|------------|
| Capability contract surface | Core | `ICapability` non-generic marker, CLR interface type as identity, `ICapabilityMetadata` per storage, `ICapabilityRequirement<TMeta>` for typed filter matching. Push-based resolution for queries, pull-based API for ad-hoc. |
| Command buffer contract shape | Emergent | Fully emergent, no foundation contract. Session-based API (begin/operations/end) targeting one entity per session. Storage-specialized via generics. |
| Concurrent execution infrastructure | Engine Infrastructure | Per-category graph builder + scheduler pairs via scheduling extension. No separate resource access protocol. Category-specific builder defines its own concurrency semantics. |
| Synchronization / locking coordination | Engine Infrastructure | Per-category scheduling extension — each SF category defines its own concurrency model via graph builder + scheduler |
| Iteration shape | Emergent | Entity-by-entity enumerator, internally chunk-based via GetNextChunk |
| Entity handle source generation | Emergent | SG generates per-query handle from ReadComponents/WriteComponents attributes |
| Query declaration mechanism | Emergent | Separate partial type with strategy + component access attributes |
| DI integration for World/queries | Core / DI | No DI refactoring — queries take World via DI, capabilities live in World |
| Archetype role at runtime | Core / Emergent | Blueprints only, not runtime containers. Entities owned by storages |
| Entity creation return type | Emergent | Conditional on identity capability — ID if present, void otherwise |
| Storage slot management | Emergent | Swap-and-pop for dense iteration |

### Deferred

| Question | Layer | Notes |
|----------|-------|-------|
| Storage internal composition/compilation | Storage internals | Left open by design |
| Change detection (Changed, Added filters) | Extension | Reactive/incremental processing |
| Component toggling | Extension | Enable/disable without structural change |
| Observer/event hooks | Extension | Reactive hooks on structural changes |

---

*Design model — last updated 2026-03-12 (capability contract, command buffer, scheduling extension resolved)*
