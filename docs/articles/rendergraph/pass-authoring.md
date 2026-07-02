---
uid: sparkitect.rendergraph.pass-authoring
title: Authoring Passes
description: Writing render-graph passes — Setup and Execute, resource handles, and dependency boundaries
---

# Authoring Passes

A pass is a stock render-graph object with two phases: `Setup` declares the resources the pass uses, and `Execute` records GPU work. `Setup` runs when the graph is built or rebuilt; `Execute` runs every scheduled frame. A pass is a partial class registered through [`RenderPassRegistry`](xref:Sparkitect.Graphics.RenderGraph.RenderPassRegistry) and derived from [`ComputePass`](xref:Sparkitect.Graphics.RenderGraph.ComputePass):

```csharp
[RenderPassRegistry.RegisterPass("space_invaders_copy")]
internal sealed partial class SpaceInvadersCopyPass : ComputePass
{
    private IGraphResource<TransferSrcReadView> _source = null!;
    private IGraphResource<SwapchainWriteView> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Use(new TransferSrcReadViewDescription { TargetMoment = GraphMomentID.SpaceInvadersMod.Target });
        _swapchain = ctx.Use(new SwapchainWriteViewDescription());
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        commandBuffer.BlitFullExtent(
            _source.Fetch().Backing, ImageLayout.TransferSrcOptimal,
            _swapchain.Fetch().Backing, ImageLayout.TransferDstOptimal,
            Filter.Nearest);
    }
}
```

The `partial` keyword is required by the Registry Generator, which emits the pass's identification. The class is your only authoring entry point; see [Registry System](xref:sparkitect.core.registry-system) for the registration mechanics.

## Setup vs Execute

`Setup` and `Execute` have disjoint jobs, and mixing them is an error.

| Phase | Runs | Does | Must not |
|-------|------|------|----------|
| `Setup(ISetupContext ctx)` | At build/rebuild | Declare resource usage via `ctx.Use` | Fetch, record commands, touch the GPU |
| `Execute(VkCommandBuffer commandBuffer)` | Every scheduled frame | Fetch handles, record work | Declare new usage |

`Setup` produces the declarations the graph compiles into an order. `Execute` consumes the per-frame result. A pass that fetches during `Setup` reads a resource before it resolves — undefined behavior in this layer, not runtime-guarded.

## The Resource Surface

A pass holds each resource it uses as an [`IGraphResource<T>`](xref:Sparkitect.Graphing.IGraphResource`1) field, assigned in `Setup`:

```csharp
private IGraphResource<StagingBuffer> _staging = null!;
// ...
_staging = ctx.Use(new StagingDescription());
```

The field's type argument is the resource the handle resolves to. The handle is behavior-free: its only member is `Fetch()`, which returns the live instance for the current frame. It carries no slot, backing, or declaration-internal wiring.

`ctx.Use` is the single declaration verb. It takes a resource description and returns the handle:

```csharp
IGraphResource<TResource> Use<TResource>(IResourceDescription<TResource> description);
```

Whether a use reads an existing resource or produces a new epoch of one is decided *inside the description*, not chosen at the pass. A pass never selects a read-vs-write mode. See <xref:sparkitect.rendergraph.descriptions-and-moments>.

## Fetch in Execute

Call `Fetch()` inside `Execute` to resolve the frame's instance:

```csharp
public override void Execute(VkCommandBuffer commandBuffer)
{
    var img = _target.Fetch();
    commandBuffer.ClearColorImage(img.Backing, ImageLayout.TransferDstOptimal, in clearColor);
}
```

`Fetch()` re-resolves per frame — the handle is stable across rebuilds, the instance it returns is not. Fetching in the declaring hook (`Setup`, or inside a description's `Declare`) is undefined.

## Dependency Boundaries

A pass draws on two kinds of input, and the distinction is load-bearing:

| Input | How | Example |
|-------|-----|---------|
| Engine service | DI constructor parameter | An `IShaderManager`, a config |
| Graph resource / view | `ctx.Use(description)` | A staging buffer, a swapchain view |

Data derived from graph resources flows **through a graph resource**, never back through DI. When the compute pass needs the entity count the staging pass produced, it reads that count off the published resource instance — not from an injected manager. Reaching back into DI for graph-produced data breaks the ordering the graph derives; the resource model makes it structurally inexpressible.

## Escape Hatches

`Execute` receives a raw [`VkCommandBuffer`](xref:Sparkitect.Graphics.Vulkan.VulkanObjects.VkCommandBuffer) — record any Vulkan work you need directly on it. Layout transitions and barriers for stock resources are contributed by their views' lifecycle hooks (the graph dispatches them around your `Execute`); the pass records none for stock resources.

A pass must not create or destroy Vulkan objects that back stock resources — the graph owns their lifetime. Override `Dispose()` only to release GPU objects the pass itself created; it runs at graph teardown after the device is idle.

## See Also

- <xref:sparkitect.rendergraph.descriptions-and-moments> for what a description declares
- <xref:sparkitect.rendergraph.data-flow-ordering> for how declarations become order
- <xref:sparkitect.rendergraph.requirements> for the layering rationale behind the pass model
