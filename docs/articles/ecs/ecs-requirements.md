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

This mirrors how Stateless Functions work: the core provides `IStatelessFunction`, `IScheduling`,
and `ExecutionGraphBuilder`. Scheduling logic (PerFrame, OnCreate, etc.) is emergent, not baked in.

## Systems

Systems are Stateless Functions. A system is a static method with DI-injected parameters.

- Consistent with existing Sparkitect patterns
- Enforces pure systems (no hidden state)
- Queries and command buffers are DI parameters on the system function
- System parameters are resolved through the existing DI framework (may require extensions
  for world-scoped lifetimes)

### System Groups

Systems belong to System Groups. Groups act as the parent/owner for systems (analogous to
how modules own Stateless Functions).

- Groups determine inclusion/activation of their systems
- Activation is context-based when (re)building the execution graph
- Runtime toggling of system groups may be supported (depends on scheduling performance)

### Ordering

System ordering uses Stateless Function scheduling (`OrderAfter`/`OrderBefore`,
`ExecutionGraphBuilder`). No new ordering mechanism is needed.

### Parallelization

- Systems run in parallel by default where access patterns allow
- Read/write locks at the storage level enforce safe concurrent access
- Locking granularity depends on the storage implementation
- Access metadata (which components are read/written) exists for parallelization, not ordering
- Unresolved write conflicts produce a runtime warning and default to serialization
  (build-time errors are impossible due to modding)

## World

The World is independent of GameState. It is standalone infrastructure that can be created,
ticked, and destroyed by any code.

- `World.Tick()` is triggered by GameState PerFrame Stateless Functions
- Multiple worlds are possible
- GameState integration is optional, not structural

## Structural Changes

Structural changes (create/destroy entities, add/remove components) are always deferred
through command buffers, executed between system executions. No immediate structural changes
during system execution.

- Command buffers are accessed via DI parameters on system functions

## Component Queries

Queries are the sole entity/component interaction point for systems.

### Core Contract

The core query contract is minimal: access metadata only.

- Declares which components are accessed and whether read or write
- Components are identified by Identification (not CLR types)
- No iteration contract, no execution contract in the core
- All iteration and access patterns are defined by query implementations

### Query Implementations (Emergent)

Concrete query types are emergent features, not part of the core. Examples:

- Archetype iteration (iterate entities by component set)
- Spatial querying (query entities by position/range)
- Relationship traversal (query entities by relationships)
- Any future query pattern

Each query type:

- Defines its own entity handle type (parallel to how `StatelessFunctionAttribute` defines `TContext`)
- Entity handles have statically written-out accessor methods (e.g., `GetPosition()`,
  not `Get<Position>()`)
- The handle shape and iteration pattern are defined by the query implementation

### Queries are Always Explicit

Systems explicitly declare what capabilities they need from their queries. There is no default
"just works" query. This makes access patterns, locking, and performance characteristics
visible from the declaration.

### Query Lifecycle

Queries are DI parameters, bound to a specific World instance, and cached.

## Storage

### Fully Abstract Storage

The core defines no fixed storage approach. Storage backends are emergent implementations,
following the same pattern as queries, scheduling, and all other extensible systems.

This enables different storage approaches (archetype-based, sparse sets, spatial-indexed,
graph-based, GPU-backed, etc.) without core changes.

### One Entity = One Storage

An entity belongs to exactly one storage backend. That storage must support all the entity's
data. Storage is determined at entity creation time.

### Storage Capabilities

Storages advertise capabilities that describe what they can do (e.g., component iteration,
relationship traversal, spatial querying). The exact mechanism for defining and discovering
capabilities is not yet determined.

Queries utilize storage capabilities to access entity/component data. The accessor provided
by a capability is a concrete type that knows the storage internals, avoiding per-entity
abstraction overhead in the hot path.

Locking and synchronization are handled by the accessor, not by the core.

### Specialized Pairings

Optimized query + accessor pairings can exist for specific storage types. Source generation
can produce these for compile-time known combinations. Runtime/mod-defined types use the
general capability path.

## Component Identity

Components are identified by Sparkitect's Identification system (`mod:category:item`).
This is consistent with the rest of the engine and naturally supports mod-defined components.

## Not Yet Decided

- Accessor iteration shape (enumerator, callback, span-based batches)
- Specific storage capability definitions and discovery mechanism
- Entity creation and storage selection mechanism
- Component compatibility when adding components to entities in a storage
- Relationship and spatial query implementation details
- System Group activation mechanism specifics
- Entity ID design (stable vs volatile, generational indices)