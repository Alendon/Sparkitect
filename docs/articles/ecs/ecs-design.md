---
uid: sparkitect.ecs.ecs-design
title: ECS Design Model
description: Design rationale for the layered ECS — why the core is zero-policy, what is emergent, and the forward-looking model
---

# ECS Design Model

This page records why the ECS is built the way it is and what is still ahead. The shipped mechanics — writing components, queries, and systems, and how storage, capabilities, and scheduling work — live in the usage and internals articles (<xref:sparkitect.ecs>). This page keeps only the design intent and forward-looking model.

## Why a Layered Model

The ECS is split into three layers so policy never leaks into the foundation:

- **Framework core (zero-policy).** The world coordinator, storage contract, capability contract, and component identity. It defines coordination and extension points and nothing else — no data layout, iteration strategy, identity scheme, or behavioral policy.
- **Emergent features.** Concrete implementations built on the core contracts. All actual behavior — storage, iteration, identity, command buffers — lives here.
- **Engine-infrastructure extension.** Per-category Stateless Function scheduling, so ECS systems get their own graph builder and executor without a bespoke parallelism protocol.

The rationale mirrors Stateless Functions: establish a type-safe protocol via generics, refuse to encode specific behavior, and let emergent implementations make every real decision. The foundation does not change when new emergent types are added. The core is deliberately smaller than a typical ECS — entity identity, command buffers, execution models, and iteration strategies are all emergent.

Shipped form: <xref:sparkitect.ecs.internals-architecture>.

## Why These Pieces Are Core

### World — Coordinator, Not Owner

The World mediates capability discovery and storage lifecycle but owns no entity data, identity, or storage internals. Keeping it a mediator is what lets multiple worlds exist, keeps it independent of GameState, and keeps the execution-model choice emergent rather than a fixed `Tick()` shape. Shipped form: <xref:sparkitect.ecs.internals-capabilities>, <xref:sparkitect.ecs.worlds-storage>.

### Storage as a Black Box

The core sees a storage only through capability interfaces plus a minimal lifecycle. This is deliberate: memory layout, internal IDs, and internal composition are left unconstrained so archetype, sparse-set, spatial-indexed, or GPU-backed backends need no core change. Shipped form: <xref:sparkitect.ecs.internals-storage>.

### Capabilities as Identity

A capability is a closed generic CLR interface, so the CLR itself enforces uniqueness and the core stays component-agnostic — a capability may expose component data, spatial coordinates, or relationship graphs. The contract carries only discovery coordination; each capability type owns its metadata shape and matching logic. Shipped form: <xref:sparkitect.ecs.internals-capabilities>.

### Queries Declare Access Only

The core query contract is access metadata — which components, read or write — identified by Identification, not CLR types. Iteration shape and entity handle are emergent and source-generated per system, so a query never constrains how a storage lays out its data. Shipped form: <xref:sparkitect.ecs.queries>, <xref:sparkitect.ecs.internals-query-sg>.

### Component Identity as a Political Choice

Components are data with an Identification. This is a chosen cornerstone rather than a technical necessity; at the foundation it is deliberately unwired from capabilities, storages, and the World, so all wiring between components and the rest of the ECS happens in the emergent layer. Shipped form: <xref:sparkitect.ecs.components>.

## Why Per-Category Scheduling

Concurrency is reached by extending Stateless Function scheduling, not by adding a parallel resource-access protocol to the ECS. Each SF category gets its own graph builder and scheduler pair, so category-specific execution strategies stay isolated — a flat sequential list for simple categories, wave-partitioned execution for concurrent ones. ECS systems are Stateless Functions, so ordering reuses the existing `OrderAfter`/`OrderBefore` mechanism with no new concept. Shipped form: <xref:sparkitect.ecs.internals-scheduling>.

Prior art: the predecessor engine (Alendon/MintyCore) used a greedy task-based model — systems dispatched via `Task.ContinueWith` as ordering and per-component reader/writer constraints cleared, with hierarchical system groups. In Sparkitect that dispatch strategy moves to the emergent layer; the scheduling extension provides only the structural framework.

## Forward-Looking

Not yet implemented — the residual design targets.

### Future Source-Generator Family

Source generation is intended to cover most of the emergent ECS, lowering boilerplate and keeping the system open. The shipped generator produces component queries and per-system resource-access metadata; system, archetype, and capability generation are planned extensions of the same bridging role Stateless Function generation already plays. Others can write their own generators for specialized query or storage types.

### Parallel Executor

Per-system read/write access metadata is generated today, but the shipped executor is sequential. A concurrent graph builder plus executor that consumes that metadata — pre-computed waves or greedy dispatch, with bitset conflict detection — is the main open execution question. Unresolved write conflicts default to serialization, since build-time errors are impossible under modding.

### Emergence Levels

Emergent features are categorized by how tightly they couple to the core:

| Level | Description | Examples |
|-------|-------------|----------|
| Foundation | Ships with the framework, uses core contracts, could be replaced | SoA component storage, component query, command buffer, generational IDs |
| Extension | Adds new capabilities on top of the framework | Relationship storage, spatial storage, position-based identity |
| Peripheral | Uses the framework but does not extend its contracts | Named-entity component, debug inspector |

Unbuilt extensions:

- **Generational entity IDs** — u32 index + u32 generation packed into u64, opt-in per storage via an identity capability, for entities that need stable cross-storage references.
- **Position-based identity** — entities identified by in-world position (block entities in a voxel game), demonstrating identity as a capability rather than a framework concern.
- **Relationship storage** — directed entity-to-entity links (ChildOf, IsA, custom pairs) with optional relationship data.
- **Spatial storage** — entities indexed by position for range and proximity queries.

### Open Questions

| Question | Layer | Notes |
|----------|-------|-------|
| Parallel execution strategy | Emergent | Pre-computed waves vs. greedy dispatch |
| Column memory management | Emergent | Pooling and growth strategy for native columns |
| Chunk size for iteration | Emergent | Cache-friendly batch sizing |
| Relationship storage design | Extension | Pair components, cascading, directed links |
| Spatial indexing strategy | Extension | Data structure, update and query shapes |
| Change detection, component toggling, observer hooks | Extension | Reactive/incremental processing, deferred |

*Design model — rationale and forward-looking model; shipped behavior lives in the usage and internals articles.*
