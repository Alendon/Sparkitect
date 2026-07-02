---
uid: sparkitect.rendergraph.internals-graphing-core
title: Internals — Graphing Core and the L0/L1/L2 Seam
description: The generic computational-graph core beneath the stock render graph, and the two handle types at the core/specialization seam
---

# Internals: Graphing Core and the L0/L1/L2 Seam

> [!NOTE]
> This page describes engine internals, not the mod-author surface. Mod authors write passes and descriptions; see <xref:sparkitect.rendergraph.pass-authoring>.

The Graphing core is a generic, GPU-unaware computational-graph substrate under `Sparkitect.Graphing`, layered as L0 plus L1. L0 is the executable-graph substrate: node identity, declaration capture, hook-driven binding and fetch, opaque handles, and extension contracts. L1 is the dataflow core layered on it: the [`DeclarationLedger`](xref:Sparkitect.Graphing.Ledger.DeclarationLedger), the Read/Increment grammar over epochs, moment bookkeeping, and the compile pipeline. Resource is not a core concept — the core owns no GPU or rendering semantics; a resource is composed entirely in the layer above.

The stock render graph is L2, the GPU/Vulkan specialization under `Sparkitect.Graphics.RenderGraph`, and the core's first consumer; a future non-rendering consumer would reuse L0+L1 with a different L2. L2 is expected to be thin — large complexity in L2 signals a concern that belongs in L1. Two handle types live at the seam and must not be confused: [`IGraphResource<T>`](xref:Sparkitect.Graphing.IGraphResource`1) is the behavior-free pass-side handle fetched each frame, while [`ResourceRef<T>`](xref:Sparkitect.Graphing.Ledger.ResourceRef`1) is the opaque structural reference that lives inside facts and is never fetched.

## See Also

- <xref:sparkitect.rendergraph.internals-resource-model> for how a resource is composed in L2
- <xref:sparkitect.rendergraph.internals-ledger-epochs> for the ledger and epoch substrate
- <xref:sparkitect.rendergraph.internals-compilation> for the compile pipeline
- <xref:sparkitect.rendergraph.requirements> for the full layering rationale
