---
uid: sparkitect.rendergraph.requirements
title: Render Graph Requirements
description: Design rationale and forward-looking direction for the render graph — source-generated authoring, resize, and resource aliasing
---

# Render Graph Requirements

This document is the render graph's design rationale and forward-looking direction. Shipped behavior is documented in the render graph articles and is not restated here: pass authoring in <xref:sparkitect.rendergraph.pass-authoring>, descriptions/facts/moments in <xref:sparkitect.rendergraph.descriptions-and-moments>, composites in <xref:sparkitect.rendergraph.composites>, ordering in <xref:sparkitect.rendergraph.data-flow-ordering>, external state in <xref:sparkitect.rendergraph.external-push>, and the L0/L1/L2 internals in <xref:sparkitect.rendergraph.internals-graphing-core> and its siblings.

What remains here is the intent the design commits to and the parts not yet built: source-generated authoring, resize handling, and resource aliasing. Sections marked as sketched carry no requirements weight.

## Design Intent

These anchors are locked; the shipped implementation realizes them, and every future change must preserve them.

- **Generic core, thin specialization.** The lower two layers (L0 executable-graph substrate, L1 dataflow core) form a generic, GPU-unaware computational-graph core; the stock render graph is L2, the GPU/Vulkan specialization built on it. L2 is expected to be thin — large complexity surfacing in L2 signals a concern that belongs in L1. A future non-rendering consumer would reuse L0+L1 with a different L2. See <xref:sparkitect.rendergraph.internals-graphing-core>.
- **Horizontal, not vertical.** The core defines no universal resource hierarchy that stock and mod resources inherit from. Stock and mod layers cooperate through generated or hand-written contracts, and no authoring role is privileged — engine, game, and mod code use the same public contracts, registration paths, and extension points.
- **All resource relationships are epoch relationships.** Every edge the graph orders on is a Read or an Increment against a specific epoch of a specific resource; there is no separate create, write, publish, or consume vocabulary. See <xref:sparkitect.rendergraph.descriptions-and-moments>.
- **The declaration ledger is the single compile truth.** The full resource graph — every resource, relationship, and ordering edge — is reconstructable from declarations alone, without executing a frame. See <xref:sparkitect.rendergraph.internals-ledger-epochs>.
- **The render graph holds no knowledge of any resource's runtime state.** It never inspects, assumes, or stores what state a resource is in at runtime. Runtime state is owned by the resource itself; the graph only schedules *when* the resource's own lifecycle hook runs, and that hook reconciles the resource's carried state against the state its use requires. This is the root reason synchronization is fully runtime-dynamic rather than plan-computed, and it is inviolable — any coupling here would constrain what a resource can be and how a mod may extend it.
- **Stock ergonomics are first-class.** The genericity of the core buys modularity and modding reach; it must not dilute stock authoring into a thin pass-through over the foundation.

## Forward-Looking Direction

### Source-Generated Pass Authoring

Not yet shipped. Today a pass is a `[RenderPassRegistry.RegisterPass]` partial class with plain `IGraphResource<T>` fields assigned in `Setup` (see <xref:sparkitect.rendergraph.pass-authoring>). The committed direction replaces the hand-assigned fields with a member-declaration-driven generator, following the style of Stateless Functions and ECS component queries.

An annotated graph-resource member is the generator trigger, and the member's resource type is the generator's source of truth for that member's declaration:

```csharp
public sealed partial class EntityStagingPass : ComputePass
{
    [GraphResource] private IGraphResource<StagingBuffer> _staging;
    [GraphResource] private IGraphResource<EntityListResource> _entities;
    // ...
}
```

The generator inspects each resource type and the hook-shaped interfaces it implements, then emits pass/resource metadata, binding/fetch code, and strongly typed lifecycle-hook dispatchers. Setup stays real code — resource declaration is too dynamic for an attribute-only model — but the generator must not infer resource semantics from arbitrary setup method bodies. Anything the generator emits must remain implementable by hand for advanced mods.

Companion analyzers validate the shape: annotated members assigned from declaration verbs whose resource type matches the member; handles fetched only from execution-time paths; a description type implementing exactly one description-interface instantiation; descriptions immutable-shaped (sealed records, init-only) and facts that do not smuggle behavior back into the graph; declaration products assigned only in declaration logic and read only after declaration; and manual contracts matching the generated shape.

**Generated lifecycle-hook dispatch** is the render graph's synchronization insertion mechanism. Hook-shaped interfaces are discovered generically from pass and resource types — the generator hardcodes no fixed hook set — and each discovered hook yields one strongly typed pass-local dispatcher the graph calls at the matching phase. Dispatch order within a pass's window is ledger-topological, not declaration-ordered. Hook insertion is graph/generator responsibility; a pass may override dispatch through its own hook implementations.

### Resize Handling

The swapchain backing is owned and recreated by stock window/swapchain infrastructure, not the graph; a resize invalidates swapchain-relative resources. Descriptions parameterized from live services (such as the swapchain extent) re-snapshot when the graph rebuilds and re-runs setup.

Open: whether rebuild granularity covers every resize path, and whether swapchain frame management (image acquisition, present-layout transition, present submission) should be modeled as explicit early/late swapchain passes that participate in normal graph ordering. The uniform increment grammar strengthens the case for modeling them as passes; it is unresolved.

### Resource Aliasing and Backing Recycling

The `Release` cleanup strategy is the substrate aliasing builds on: the graph signals release and the backing provider owns the policy — no-op, pool-return, or alias-reuse (see <xref:sparkitect.rendergraph.composites>). The graph never decides how a release is honored.

Memory aliasing is reusing a backing as an epoch advance with re-grounding; the ledger makes the legality computable because old-epoch readers are ordered first (see <xref:sparkitect.rendergraph.data-flow-ordering>). Recycling a no-longer-read base backing, and exploiting release for transient-resource reuse, are enabled by the ledger but not committed — future stock or extension behavior.

### Conditional Re-Execution (sketch)

The ledger retains enough per-operation provenance that a change-driven skip evaluator could be added without re-architecting: cross-frame change stamps per value, per-operation memoization of input stamps, and a global run-set evaluation in which eligibility propagates forward along dataflow and required re-runs propagate backward along a backing's epoch chain (a chain skips suffix-closed or reruns prefix-closed — never the middle). Known brittle areas are passes that read live services during Execute, and data residency across instance rotation. Recorded so the committed model stays checked against it; unvalidated by any sample, no requirements weight.

### Future GPU Scope

The initial target is compute-first; rasterization is future scope. The design must not prevent later multi-queue scheduling across graphics, compute, and transfer queues. Frames-in-flight is realized as central instance multiplication — one ledger node mapped to per-frame instances, resolved transparently at `Fetch()` — with residency policy for multi-frame resources still open (single-instance-with-sync vs copy-on-advance). Subresources (mips, layers, buffer ranges) are view-layer semantics. The sample renderers already run on the model with explicit ordering edges eliminated in favor of data-flow ordering.

## Open Questions

- For a pushable resource, should the publish handoff seal the data object in place or swap it for a fresh recording target? The fail-fast intent is settled; the mechanism is not.
- What exactly do the stock pass/resource source generators emit, and how are hook dispatchers represented in generated and manual contracts?
- Exact names of the description-internal transaction verbs. The pass surface is settled — a single `use(description)` verb, with relation semantics and moment marking carried inside the description.
- Residency and instance policy for resources carried across frames once multiple frames are in flight, and its interaction with the conditional re-execution sketch.
- Whether swapchain frame management should be modeled as explicit early/late swapchain passes in normal graph ordering.
- Whether rebuild granularity is sufficient for all resize paths when descriptions are parameterized from live services.
- Cross-mod contradictory grounding: diagnosis ships; resolution policy is deferred until real data exists.

## See Also

- <xref:sparkitect.rendergraph> for the render graph overview and article index.
- <xref:sparkitect.rendergraph.internals-graphing-core> for the L0/L1/L2 layering the intent commits to.
- <xref:sparkitect.core.stateless-functions> for the generic function/category/source-generation pattern.
- <xref:sparkitect.tooling.source-generation> for Sparkitect source generator conventions.
- <xref:sparkitect.ecs.ecs-design> for the foundational/emergent layering pattern and generated query bridges.
- <xref:sparkitect.core.registry-system> for `Identification` and registry patterns.
- <xref:sparkitect.core.dependency-injection> for DI and entrypoint-container patterns.
