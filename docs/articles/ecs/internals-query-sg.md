---
uid: sparkitect.ecs.internals-query-sg
title: "Internals: Query Generation"
description: What the ECS query source generator emits from a component-query declaration, including concurrency metadata
---

# Query Source Generation

*Engine internals. Mod authors can work entirely from the usage pages; this page describes what the generator emits.*

The `EcsQueryGenerator` turns a [`[ComponentQuery]`](xref:Sparkitect.ECS.Queries.ComponentQueryAttribute) partial class into the code a system consumes: a typed entity handle with per-component accessors, an enumerator that walks capability chunks, a [`ComponentSetRequirement`](xref:Sparkitect.ECS.Queries.ComponentSetRequirement) filter built from the declared component IDs, and constructor forwarding so the query resolves [`IWorld`](xref:Sparkitect.ECS.IWorld) and its cached capabilities from DI. The read/write declarations decide whether a generated accessor returns `ref` or `ref readonly`.

Alongside each query, the generator emits an [`EcsSystemResourceAccess`](xref:Sparkitect.ECS.Systems.EcsSystemResourceAccess) entrypoint per system — the aggregated read and write component sets derived from that system's query parameters. This is the concurrency metadata: it is generated and injected today but consumed only by debug tooling and the future concurrent graph builder, not by the shipped executor.

## See Also

- <xref:sparkitect.ecs.internals-scheduling> for how systems are ordered and executed.
- <xref:sparkitect.ecs.queries> for the mod-author view of declaring queries.
- <xref:sparkitect.ecs.ecs-design> for the source-generation role across the ECS.
