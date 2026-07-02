---
uid: sparkitect.rendergraph
title: Render Graph
description: Stock GPU render graph — pass authoring, resource declaration, data-flow ordering, and external push
---

# Render Graph

The render graph is the stock engine layer for GPU rendering: passes declare the resources they use, and the graph derives execution order, synchronization, and per-frame resource resolution from those declarations. You never write a barrier or an ordering index; you declare data usage and the graph compiles the rest.

Sparkitect ships no game-specific passes. You build a pipeline from stock graph components and your own passes. Mods extend it by registering passes, resource descriptions, and cross-pass moments.

A pass is a partial class that overrides `Setup` (declares resource usage) and `Execute` (records GPU work). The one declaration verb is `ctx.Use(description)`, which returns an [`IGraphResource<T>`](xref:Sparkitect.Graphing.IGraphResource`1) handle the pass fetches each frame:

```csharp
[RenderPassRegistry.RegisterPass("clear_color")]
internal sealed partial class ClearColorPass : ComputePass
{
    private IGraphResource<ImageResource> _target = null!;

    public override void Setup(ISetupContext ctx) =>
        _target = ctx.Use(new ClearColorImageDescription());

    public override void Execute(VkCommandBuffer commandBuffer) =>
        commandBuffer.ClearColorImage(_target.Fetch().Backing, ImageLayout.TransferDstOptimal, in clearColor);
}
```

## Usage

- <xref:sparkitect.rendergraph.pass-authoring> — Writing passes: `Setup`/`Execute`, resource handles, dependency boundaries
- <xref:sparkitect.rendergraph.descriptions-and-moments> — Descriptions, facts, and the moment system for cross-pass references
- <xref:sparkitect.rendergraph.composites> — Composite resources, declaration products, and cleanup strategies
- <xref:sparkitect.rendergraph.data-flow-ordering> — How Read/Increment edges become execution order
- <xref:sparkitect.rendergraph.external-push> — Feeding external data in and driving swapchain presentation

## Internals

Engine internals, not the mod-author surface — the generic core beneath the stock render graph.

- <xref:sparkitect.rendergraph.internals-graphing-core> — The L0/L1/L2 layering and the core/specialization seam
- <xref:sparkitect.rendergraph.internals-resource-model> — Description → fact → instance, resolved through graph-local DI
- <xref:sparkitect.rendergraph.internals-ledger-epochs> — The declaration ledger and its symbolic epoch chains
- <xref:sparkitect.rendergraph.internals-compilation> — The staged Link phase and the GPU-free compiled plan

## Design

- <xref:sparkitect.rendergraph.requirements> — Design rationale and forward-looking direction (resize, source-generated authoring, aliasing)
