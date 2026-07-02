---
uid: sparkitect.rendergraph.descriptions-and-moments
title: Descriptions, Facts, and Moments
description: The declaration unit, the immutable facts it produces, and moments as the cross-pass reference mechanism
---

# Descriptions, Facts, and Moments

A description is the unit of resource declaration: plain authored data whose `Declare` method records how a resource is used. A description is a `sealed record` with init-only parameters, implementing [`IResourceDescription<T>`](xref:Sparkitect.Graphing.Descriptions.IResourceDescription`1):

```csharp
public sealed record TransferSrcReadViewDescription : IResourceDescription<TransferSrcReadView>
{
    public required Identification TargetMoment { get; init; }

    public DeclaredFact<TransferSrcReadView> Declare(IResourceTransaction tx)
    {
        tx.ReferenceMoment(TargetMoment);
        var fact = tx.InstantiateFact<TransferSrcReadViewFact>();
        return fact with { Moment = TargetMoment };
    }
}
```

`Declare` runs exactly once, inside a ledger transaction, as the parameter snapshot for that use. The description holds authored parameters; it does not hold a resource.

## Facts

`Declare` returns a [`DeclaredFact<T>`](xref:Sparkitect.Graphing.Descriptions.DeclaredFact`1) — an immutable record the description produces. Facts own construction of the live instance:

```csharp
[FactRegistry.Register("staging")]
public sealed partial record StagingFacts(IBufferManager? Provider)
    : DeclaredFact<StagingBuffer>, IHasIdentification
{
    public ResourceRef<BufferResource> Host { get; init; }
    public ResourceRef<BufferResource> Device { get; init; }

    public StagingBuffer CreateInstance(IInstanceContext ctx) =>
        new(ctx.Resolve(Host), ctx.Resolve(Device), Provider!);

    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
```

The ledger stores the fact, never the description. The fact is immutable — `init`-only members, `with`-expression assembly — and immutability is enforced by analyzers. `CreateInstance` builds the live instance at resolve time, pulling sub-references through the [`IInstanceContext`](xref:Sparkitect.Graphing.Descriptions.IInstanceContext). Facts are resolved through the `FactRegistry`, so their constructor dependencies come from graph-local DI.

## The Two-Relation Grammar

A description speaks exactly two relations over resources, through the [`IResourceTransaction`](xref:Sparkitect.Graphing.Descriptions.IResourceTransaction):

| Verb | Meaning |
|------|---------|
| `tx.Read(reference)` | This use consumes the resource at that epoch |
| `tx.Increment(reference)` | This use advances the resource to a new epoch (produces it) |

There is no create, write, publish, or consume vocabulary — Read and Increment are the whole grammar. `Increment` has an overload that also marks the produced epoch with a moment. `tx.Self<T>()` references the description's own resource; `tx.Declare(subDescription)` sub-declares a nested resource (see <xref:sparkitect.rendergraph.composites>).

## Moments

A moment is a registered [`Identification`](xref:Sparkitect.Modding.Identification) naming one symbolic epoch of one resource. It is the only cross-pass reference mechanism — one pass produces at a moment, another reads it, and the two never share a handle.

Moments are registered like any other value registry entry, through [`ResourceMomentRegistry`](xref:Sparkitect.Graphics.RenderGraph.Moments.ResourceMomentRegistry). The registration declares a name and a resource type, nothing else:

```csharp
[ResourceMomentRegistry.RegisterMoment("target")]
public static ResourceMomentDefinition Target() => new ResourceMomentDefinition<ImageResource>();
```

The [`ResourceMomentDefinition<T>`](xref:Sparkitect.Graphing.Moments.ResourceMomentDefinition`1) carries the resource type only — never backing, position, or producer. The source generator emits the `Identification`; reference it as `GraphMomentID.{Mod}.{Name}`.

### Marking Is Publishing

Marking an epoch with a moment is what publishes it. A producing use increments and marks in one call; a consuming use references the moment:

```csharp
// Producer: advance and mark the epoch
tx.Increment(tx.Self<ImageResource>(), GraphMomentID.SpaceInvadersMod.Target);

// Consumer, in another pass's description: reference the moment
tx.ReferenceMoment(GraphMomentID.SpaceInvadersMod.Target);
```

There is no marking verb on the pass surface — marking lives inside the description. An epoch nobody marks stays private to its chain. The set of registered moments is the deliberate, moddable cross-pass surface: a mod can restructure the chain that produces a moment, and readers re-link to the new producing increment unchanged, because they name the moment, not a position.

## Author-Facing Errors

Moment mistakes surface as structured compile diagnostics:

| Error | Cause |
|-------|-------|
| `UndefinedMoment` | A referenced moment has no marked increment |
| `DuplicateMoment` | Two increments marked the same moment |
| Type mismatch | A reference resolves to a different resource type than registered |

See <xref:sparkitect.rendergraph.requirements> for how compilation binds moments to increments.

## See Also

- <xref:sparkitect.rendergraph.composites> for descriptions that sub-declare other resources
- <xref:sparkitect.rendergraph.external-push> for moments fed from outside the graph
- <xref:sparkitect.rendergraph.data-flow-ordering> for how Read/Increment become order
