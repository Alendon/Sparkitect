---
uid: sparkitect.rendergraph.internals-resource-model
title: Internals — Resource Model
description: How a description becomes an immutable fact and a per-frame instance, resolved through graph-local DI
---

# Internals: Resource Model

> [!NOTE]
> This page describes engine internals, not the mod-author surface. For the authoring view of descriptions, facts, and moments see <xref:sparkitect.rendergraph.descriptions-and-moments>.

Resource declaration flows description → fact → instance. A description records how a resource is used inside a ledger transaction whose verbs — Read, Increment, sub-`Declare`, and moment marking — run exactly once per use; declaring one description instance twice is the `DescriptionReuse` compile error, so each instance maps to at most one declaration. Each declaration produces an immutable [`DeclaredFact<T>`](xref:Sparkitect.Graphing.Descriptions.DeclaredFact`1) that owns construction of the live instance and never mutates after declaration.

Facts are resolved through the [`FactRegistry`](xref:Sparkitect.Graphics.RenderGraph.FactRegistry), so their constructor dependencies come from graph-local DI (an `IFactoryContainer` scoped to the graph). At frame time the [`IInstanceContext`](xref:Sparkitect.Graphing.Descriptions.IInstanceContext) builds each instance dependency-first and caches it, so the same reference resolves to the same instance within a frame. Each fact declares a [`CleanupStrategy`](xref:Sparkitect.Graphing.Descriptions.CleanupStrategy) that is honored by the resource's backing provider, never by the graph; `Release` is the substrate future backing-aliasing builds on.

## See Also

- <xref:sparkitect.rendergraph.internals-ledger-epochs> for the ledger that stores the facts
- <xref:sparkitect.rendergraph.internals-graphing-core> for the core/specialization seam
- <xref:sparkitect.rendergraph.composites> for the author-facing declaration-product model
- <xref:sparkitect.rendergraph.requirements> for the resource-model rationale
