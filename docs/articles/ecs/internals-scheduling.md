---
uid: sparkitect.ecs.internals-scheduling
title: "Internals: Scheduling"
description: How ECS systems are ordered as a Stateless Function category and executed sequentially with group gating
---

# System Scheduling

*Engine internals. Mod authors can work entirely from the usage pages; this page describes how systems are ordered and run.*

ECS systems run as their own Stateless Function category ([`EcsSystemCategoryAttribute`](xref:Sparkitect.ECS.Systems.EcsSystemCategoryAttribute)) with a dedicated graph builder rather than the engine's flat scheduler. The internal `IEcsGraphBuilder`'s `BuildFromTree` indexes the system tree and validates that every `OrderAfter`/`OrderBefore` constraint targets a sibling in the same group, then produces an internal `EcsExecutionGraph` by locally toposorting each group's direct children and splicing child spans depth-first so a group's members stay contiguous. Group gate nodes carry `GroupSkipRanges`, precomputed jump targets that let the executor skip an inactive group's whole span at tick time.

The shipped executor is sequential: it walks the sorted node list and calls each active system once, in order. Per-system read/write access metadata is generated (see <xref:sparkitect.ecs.internals-query-sg>), but parallel dispatch is not wired — concurrent execution across non-conflicting systems is forward-looking.

## See Also

- <xref:sparkitect.ecs.systems> for the mod-author view of writing, grouping, and ordering systems.
- <xref:sparkitect.ecs.internals-query-sg> for where the concurrency metadata comes from.
- <xref:sparkitect.ecs.ecs-design> for the per-category scheduling rationale and future parallel executor.
