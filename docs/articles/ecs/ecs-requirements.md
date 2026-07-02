---
uid: sparkitect.ecs.ecs-requirements
title: ECS Requirements
description: The design decisions and constraints behind the ECS, condensed to intent, plus the parts not yet decided
---

# ECS Requirements

The design decisions and constraints behind the Entity Component System, condensed to intent. The shipped behavior each decision produced lives in the usage and internals articles (<xref:sparkitect.ecs>); this page keeps only why each decision was made and what remains open.

## Design Philosophy

The ECS follows the same philosophy as the rest of Sparkitect: the core defines minimal interaction contracts, and concrete behavior emerges from implementations. Nothing is hardcoded that can be made extensible. This mirrors Stateless Functions, where the core provides the function and scheduling contracts while the concrete scheduling categories are emergent.

The pattern repeats across the ECS: component types, storage, queries, and entity identity are all emergent, each bringing its own registries, constraints, and resolution logic against a generic core contract. The payoff is that multiple implementation approaches can coexist, be benchmarked, and be selected per use case.

## Decisions and Constraints

### Systems Are Stateless Functions

A system is a static method with DI-injected parameters, so systems stay pure (no hidden state) and reuse the existing DI and scheduling machinery. Queries and command buffers are DI parameters; a system may hold any number of queries. Ordering reuses `OrderAfter`/`OrderBefore` — no new mechanism. Shipped form: <xref:sparkitect.ecs.systems>.

### Groups Own Systems

Systems belong to groups, analogous to how modules own Stateless Functions. Groups gate inclusion and activation when the execution graph is built, which is what makes runtime toggling of whole subtrees cheap. Shipped form: <xref:sparkitect.ecs.systems>, <xref:sparkitect.ecs.internals-scheduling>.

### Entity Identity Is a Capability

The core defines no identity scheme because different entity types have different identity needs — cross-storage references, spatial identity, or none at all for short-lived entities. Identity is provided by capabilities so an entity only pays for identity when it lives in a storage that offers it. The creation return type is conditional on that capability: an ID when present, void otherwise. Shipped form: <xref:sparkitect.ecs.internals-storage>.

### Storage Is a Black Box

The core makes no assumption about storage internals and interacts only through capability interfaces plus minimal lifecycle. This keeps every storage approach — archetype, sparse set, spatial, GPU-backed — a core-free change. Each storage owns its internal ID space, unstable by design, so iteration avoids identity-lookup cost. Shipped form: <xref:sparkitect.ecs.internals-storage>, <xref:sparkitect.ecs.worlds-storage>.

### Archetypes Are Blueprints

Archetypes describe an entity's component shape for creation and source generation, not a runtime container. Entities are owned by storages; there is no automatic runtime archetype migration. The stock convention maps one storage per archetype, but the foundation does not enforce it. Shipped form: <xref:sparkitect.ecs.components>, <xref:sparkitect.ecs.worlds-storage>.

### World Coordinates, Does Not Own

The World is standalone infrastructure, independent of GameState, that mediates capability discovery and storage lifecycle without owning entity data or storage internals. Entity creation is delegated to a specific storage; ergonomic routing is emergent. Keeping the execution model out of the core lets different worlds tick differently. No DI refactoring was needed — queries take the World via DI and capabilities stay in the World's internal registry. Shipped form: <xref:sparkitect.ecs.worlds-storage>, <xref:sparkitect.ecs.internals-capabilities>.

### Structural Changes Are Deferred

Create, destroy, and add/remove-component are always deferred — a foundational principle, not an implementation detail — so no structural change happens mid-system. Concrete command buffers are emergent; the foundation sets the principle and leaves buffer strategy and playback timing to the emergent execution model. Shipped form: <xref:sparkitect.ecs.command-buffers>.

### Queries Declare Access Only

The core query contract is access metadata: which components, read or write, identified by Identification. All iteration and handle shape is emergent and source-generated per system, and each query type's generator must also emit resource-access metadata for the scheduler. Declaring a query as a separate partial type makes clear it is a generator declaration, not a runtime object. Shipped form: <xref:sparkitect.ecs.queries>, <xref:sparkitect.ecs.internals-query-sg>.

### Component Identity Is a Political Choice

Components are data identified by Sparkitect's `mod:category:item` system, consistent with the rest of the engine and naturally mod-friendly. It is a chosen cornerstone with no strict technical requirement, deliberately unwired from capabilities, storages, and the World at the foundation. The framework imposes no CLR-type restriction — type constraints are a storage concern, and each concrete component type brings its own registry. Shipped form: <xref:sparkitect.ecs.components>.

## Not Yet Decided

- **Parallelization.** Concurrency rides the Stateless Function scheduling extension, not an ECS-specific protocol. Per-system resource-access metadata is generated today; the concurrent graph builder, executor, and execution strategy (pre-computed waves vs. greedy dispatch) are open. Unresolved write conflicts default to serialization, since build-time errors are impossible under modding.
- **Future source-generator family.** System, archetype, and capability generation are planned alongside the shipped query generator.
- **Relationship and spatial storage.** Directed links and spatial indexing are extension-level capabilities, not yet designed.
- **Reactive processing.** Change detection, component toggling, and observer hooks are deferred extension concerns.
- **System group activation mechanism** specifics.
