---
uid: sparkitect.rendergraph.external-push
title: External Push and Externally-Managed State
description: Feeding external data into the graph through pushed moments, and driving swapchain presentation
---

# External Push and Externally-Managed State

Every resource in the graph is graph-owned. External data enters through one of two doors: a **pushed moment** for CPU data produced outside the graph, and an **externally-managed state handler** for the swapchain. Both keep the graph the sole owner of resource lifetime.

## Pushed Moments

A pushed moment is a registered moment whose configuration declares it externally pushed. The `[ExternalPush]` attribute attaches an [`ExternalPushConfig`](xref:Sparkitect.Graphics.RenderGraph.Push.ExternalPushConfig) under the moment's identification through the general metadata mechanism — zero generator work, the marker plus payload are the whole category:

```csharp
[ResourceMomentRegistry.RegisterMoment("entities_raw")]
public static ResourceMomentDefinition EntitiesRaw() => new ResourceMomentDefinition<PushedResource>();

[ExternalPush]
public sealed class EntitiesRawPushMarker : IHasIdentification
{
    public static Identification Identification => GraphMomentID.SpaceInvadersMod.EntitiesRaw;
}
```

The render graph discovers pushed moments among the registered set and synthesizes a chain-head producer for each — a [`PushedLeafDescription`](xref:Sparkitect.Graphics.RenderGraph.Push.PushedLeafDescription) that declares a [`PushedResource`](xref:Sparkitect.Graphics.RenderGraph.Push.PushedResource) and marks the moment on its birth increment. No pass authors that mark; readers of the moment link to the synthesized increment.

## The Publish Handoff

Publish external data through the push handler, reached from the graph:

```csharp
var handler = graph.GetHandler<IExternalPushHandler>();
handler.Publish(GraphMomentID.SpaceInvadersMod.EntitiesRaw, entitySpan);
```

[`Publish`](xref:Sparkitect.Graphics.RenderGraph.Push.IExternalPushHandler) swap-copies the span into a graph-owned per-moment snapshot buffer (grow-or-reuse). The caller's array is decoupled by construction — you keep mutating your reusable array with no seal handshake. The live mutating collection you own and the frame snapshot the graph holds are distinct by copy.

At frame start the graph binds the most recent snapshot onto the pushed resource's birth increment, re-binding unconditionally even when nothing new was published. The push lands only at the chain head; downstream of the birth increment it is ordinary graph territory — a pass reads the pushed moment like any other.

## Pushed Moments Are a Modding Axis

Because the push configuration is metadata keyed by moment identification, a mod can reconfigure the axis. Drop the `[ExternalPush]` marker and mark a pass increment on the same moment instead, inserting a transform chain — readers re-link to the new producing increment because they name the moment, not a position. Configuration-aware conflict errors catch a moment that is both externally pushed and pass-produced.

## Externally-Managed State: The Swapchain

The swapchain is not fed through the push path. It is an ordinary [`ImageResource`](xref:Sparkitect.Graphics.RenderGraph.Resources.ImageResource) whose backing originates from the engine swapchain, delivered through a dedicated [`ISwapchainHandler`](xref:Sparkitect.Graphics.RenderGraph.ISwapchainHandler):

```csharp
graph.GetHandler<ISwapchainHandler>().SetSwapchain(swapchain);
```

The handler is called after construction and on every resize. The graph drives image acquire and present; passes treat the swapchain image as a normal resource and record work against its views.

## The Finishline Moment

Presentation is modeled by the reserved `finishline` moment. Whichever increment drives the present target to a present-ready state marks the finishline, and the graph presents at the bound position:

```csharp
[ResourceMomentRegistry.RegisterMoment("finishline")]
public static ResourceMomentDefinition Finishline() => new ResourceMomentDefinition<ImageResource>();
```

The stock swapchain write view is the finishline publisher. The present-layout transition is a lifecycle hook on that view; the graph itself issues only the queue-level present. An undefined finishline (nothing marks it) or a duplicate finishline (two increments mark it) is a compile error, the same as any other moment.

## See Also

- <xref:sparkitect.rendergraph.descriptions-and-moments> for the moment mechanism push builds on
- <xref:sparkitect.rendergraph.composites> for how pushed bytes become a published GPU composite
- <xref:sparkitect.rendergraph.requirements> for the resource-ownership rationale
