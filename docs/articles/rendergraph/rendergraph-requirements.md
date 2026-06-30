---
uid: sparkitect.rendergraph.requirements
title: Render Graph Requirements
description: Requirements and design constraints for the render graph
---

# Render Graph Requirements

This document captures the current render graph design direction. It is requirements-level:
exact type names and source generator contracts remain open, but the following are locked —
the architectural boundary between the **generic computational-graph core (L0+L1)** and the
**stock RenderGraph specialization (L2)**, the two-relation declaration grammar (Read/Increment
over epochs), the description/facts declaration model, and moment-based cross-pass identity.
Sections marked as sketched directions carry no requirements weight.

## Design Anchors

- The lower two layers form a generic, GPU-unaware **computational-graph core**. **L0** is the
  executable-graph substrate: pass (node) identity, declaration *capture*, hook-driven binding and
  fetch, opaque handles, extension contracts. **L1** is the dataflow core layered on it: the
  ledger, the two-relation grammar (Read/Increment over epochs), epoch/moment bookkeeping, the
  compile pipeline, data-flow ordering, and the description/facts/instance machinery. The core owns
  no GPU or rendering semantics — *resource* is not a core concept; it is composed entirely in the
  specialization layer. The core exists to make the graph modular, testable, and modding-friendly;
  it is not the surface authors use day to day.
- Sparkitect's stock render graph is **L2**, the GPU/Vulkan specialization built on the core. It
  owns stock resource/view semantics, synchronization, lifetime, validation, command recording
  orchestration, and Vulkan integration. The RenderGraph is the core's first consumer; a future
  non-rendering consumer (e.g. a general compute graph, with or without GPU acceleration) would
  reuse L0+L1 with a different L2. L2 is expected to be thin — large complexity in L2 signals a
  concern that belongs in L1.
- The stock render graph is the shipped render-graph experience. Its ergonomics are first-class, not
  a thin pass-through over the generic foundation. The foundation's genericity buys modularity and
  modding reach; it must not dilute stock authoring.
- The render graph is primarily an orchestration layer. Passes and resource/view contracts
  provide the actual rendering and Vulkan logic; the graph builds, binds, schedules, and invokes
  them.
- The system is built horizontally, not vertically. Foundation does not define a universal
  resource hierarchy that stock Vulkan resources and mods inherit from. Instead, stock engine
  layers and mod layers cooperate through generated or manually implemented contracts.
- All resource relationships are epoch relationships. Every edge the graph orders on is a Read
  or an Increment against a specific epoch of a specific resource; there is no separate create,
  write, publish, or consume vocabulary.
- The declaration ledger is the single source of compile truth. The full resource graph is
  reconstructable from declarations alone, without executing a frame.
- Pass declarations should be source-generation friendly. Generated contracts must remain
  manually implementable for advanced mods and unusual resource models.
- Runtime graph building should consume explicit generated or hand-written contracts. It should
  not infer deep semantics from arbitrary pass code.
- At the core there is no resource-vs-view distinction: everything is a generic resource reached
  through a description, over a **single universal backing**. "Data" and "view" are emergent,
  logical labels over the same resource — they routinely mix and are never cleanly separable, so
  the foundation does not model them as distinct kinds. Where "view" appears it denotes a
  graph-level semantic usage, never a Vulkan `VkImageView`.
- A general resource description assumes nothing about the shape of the data it holds — a general
  staging buffer stages a buffer, not an array of a known stride. Data-shape (stride, layout,
  metadata prefixes, element counts) is encoded by *specialized* descriptions and resources. The
  same result is reachable two equally valid ways: compose general parts and model the complexity
  in pass setup, or encapsulate it in a specialized description. Composability is what offers both.
- There is no resource lifecycle hook model in the foundation. The stock implementation uses
  generated or manually supplied render graph lifecycle hooks at the emergent graph layer.
- Synchronization is primarily plan output derived from the ledger and static view metadata;
  lifecycle hooks are the authored extension surface, not the synchronization mechanism.
- No authoring role is privileged. Engine code, game code, and mods use the same public
  contracts, registration paths, and extension points.

## Layer Model

The system is three layers. The lower two — **Layer 0** and **Layer 1** — together form a
generic, GPU-unaware **computational-graph core**: a reusable substrate for modelling data
transformation by nodes into a result, with no rendering, Vulkan, or resource-backing semantics.
**Layer 2** is the stock RenderGraph specialization built on that core. The architectural boundary
that matters is **L0+L1 (generic core) vs L2 (specialization)**: the RenderGraph is the core's
first consumer, and a future non-rendering consumer (such as a general compute graph, with or
without GPU acceleration and an integrated CPU fallback) would reuse L0+L1 with a different L2.
All logic worth unit-testing lives in L0+L1, which is testable without a GPU; L2 is proven by the
sample renderers running.

### Layer 0 — Executable Graph Substrate

L0 defines the minimum protocol needed for graph-like systems to exist, with no notion of data
flow, resources, or relations:

- Pass (node) lifecycle shape.
- Pass identity and registration substrate.
- Setup-time declaration *capture* — not the semantics of what is captured.
- Hook-driven binding and fetch protocol.
- Opaque handles that bridge setup declarations to frame execution.
- Extension points for generated and hand-written contracts.
- Protocol-level validation, such as detecting an unbound handle fetch.

L0 deliberately does not define resources, resource views, relation semantics, the ledger's
contents, synchronization, allocation or aliasing, frames-in-flight, swapchain behavior, resource
overlap, or any GPU concept.

### Layer 1 — Dataflow Core

L1 is the generic dataflow engine layered on L0. It is domain-agnostic — it knows nodes,
declarations, and the data-flow relationships between them, but nothing about GPUs or rendering:

- The declaration **ledger** and the two-relation transaction grammar (Read/Increment over
  epochs) — one grammar spoken by passes and by description declaration logic.
- Epoch and lineage bookkeeping, **moment** linking, and the compile pipeline (collection, link,
  plan-structure emission).
- Data-flow ordering derivation, and fork / cycle / unproducible-epoch detection with full
  provenance.
- The description / declared-facts / instance pipeline, declaration products, and the
  one-declaration-per-instance rule.
- Central instance multiplication and frame binding behind `IGraphResource<T>.Fetch()` — the
  generic resolution machinery; what an instance physically *is* belongs to L2.
- The per-operation provenance a future conditional-re-execution layer would consume.

L1 owns no physical backing, no synchronization primitives, and no Vulkan. Everything a source
generator might later automate — declaration shapes, hook-dispatch wiring, metadata — lives at
this layer. L0+L1 together mirror the ECS and Stateless Function direction: a small generic core
over which concrete behavior emerges from category-specific contracts, source generation,
registries, metadata, and runtime backing.

### Layer 2 — Stock RenderGraph Specialization

The stock engine render graph is Sparkitect's shipped render graph feature: the GPU/Vulkan
specialization of the computational-graph core, not the core itself. It is expected to be **thin**
— its substance is dominated by L0+L1, and large complexity surfacing in L2 is a signal that a
concern belongs in L1.

L2 provides:

- Pass abstractions such as render and compute pass bases.
- Synchronization, layout transitions, and presentation behavior resolved at runtime from the L1
  plan's ordering plus resources' carried state — the plan fixes order and position, not state.
- Physical backing providers for leaf resources — the only per-family runtime services retained.
- The physical meaning of an instance (per-swapchain-image / per-in-flight-frame backing rotation).
- Render graph lifecycle hook dispatch.
- Command-buffer access for pass Execute — implementation-specific payload (wrapper object or
  direct `VkCommandBuffer`), with raw Vulkan preserved as an escape hatch.
- Descriptor orchestration through composite descriptions and static view metadata.
- Externally-managed state integration (the swapchain) through dedicated handlers, not the graph's
  relation grammar.
- Debugging and validation tooling.
- GameState integration through normal Sparkitect infrastructure.

Graph-core components (the L0+L1 ledger, instance table, epoch bookkeeping) are registered through
the graph-local service model, so alternative L2 specializations — or non-rendering consumers —
can reuse them piecemeal.

Stock graph instances are not engine-global singletons. Like ECS worlds, they are created and
owned by GameState-driven stock infrastructure for the relevant state/module lifetime.

Mods can extend or replace portions of this L2 behavior by adding compatible views, handlers,
pass abstractions, generated metadata, or hand-written contracts. A full replacement graph is not
the intended common path, but the architecture must not make it impossible.

### Vulkan Backend Layer

Vulkan-specific behavior lives in the L2 specialization or in Vulkan-specific extension layers.
The computational-graph core (L0+L1) does not know about Vulkan.

Current engine code already provides wrappers and services that the stock render graph can build
on:

- `IVulkanContext` exposes raw Vulkan access, wrapped device objects, queue lookup, object
  tracking, and factory methods.
- `VulkanObject` wrappers provide disposal and leak tracking.
- VMA wrapper types represent allocated buffers and images.
- Shader registries and `ShaderManager` resolve shader modules through `Identification`.
- Window and swapchain wrappers expose surface, acquisition, presentation, and resize behavior.

These are backend inputs for the stock graph, not foundation concepts.

## Pass Authoring

Passes are user-facing objects in the stock render graph. A pass has two functions:

- **Setup** declares which resources the pass reads and which it produces. It runs when the graph
  is built or rebuilt, not every frame.
- **Execute** runs every frame the compiled graph schedules the pass. It fetches previously bound
  views and records or performs the pass work.

Pass authors should be able to write the common case with minimal boilerplate. Resource usage is
declared through annotated pass-local fields or properties; the member's resource type is
the source-generator source of truth for that member's declaration.

```csharp
public sealed partial class EntityStagingPass : ComputePass
{
    [GraphResource]
    private IGraphResource<EntityListReadView> _input;

    [GraphResource]
    private IGraphResource<StagingBuffer> _staging;

    [GraphResource]
    private IGraphResource<EntityListResource> _entities;

    public override void Setup(ISetupContext ctx)
    {
        // The pass states only that it uses a resource shaped by a description. Whether the
        // usage reads or increments an epoch is encoded by the description, not chosen here.

        // Consume the externally pushed CPU entity list (a read-usage description carrying its moment).
        _input = ctx.Use(new EntityListReadView(GraphMomentID.SpaceInvadersMod.EntitiesRaw));

        // General staging — no data-shape, stride, or size assumption baked in by the pass.
        var staging = new StagingDescription();
        _staging = ctx.Use(staging);

        // Compose the published entity list from the staged buffer plus the element count it
        // carries. The count rides inside the resource — never reached through DI. Complexity is
        // assembled here, from reusable parts.
        _entities = ctx.Use(new EntityListResourceDescription(
            staging.PopulatedBuffer,
            GraphMomentID.SpaceInvadersMod.EntitiesGpu));
    }

    public override void Execute(ExecutePayload payload)
    {
        var staging = _staging.Fetch();
        // Record pass work; synchronization for declared relations is plan-emitted.
    }
}
```

The same result can instead be reached by encapsulating the staging and composition in one
specialized description, moving the complexity out of the pass and into the resource model:

```csharp
// Only _input and _entities are needed; the specialized description owns staging internally.
public override void Setup(ISetupContext ctx)
{
    _input = ctx.Use(new EntityListReadView(GraphMomentID.SpaceInvadersMod.EntitiesRaw));

    // Stages and composes the ready-to-use entity list (which itself composites the staged
    // buffer). Element layout and count are the description's concern, not the pass's.
    _entities = ctx.Use(new EntityListStagingDescription(GraphMomentID.SpaceInvadersMod.EntitiesGpu));
}
```

A consuming pass — possibly in a different mod — reaches the published resource with one
declaration, no manager, no DI:

```csharp
_entities = ctx.Use(new EntityListReadView(GraphMomentID.SpaceInvadersMod.EntitiesGpu));
```

The exact API shape is open. The important requirements are:

- Setup remains real code. Resource declaration is too dynamic for an attribute-only model.
- Setup declares resource usage through a single context verb, `use(description) ->
  IGraphResource<T>`, with one generic parameter — the resource type — inferred from the
  description; authors never write type arguments. The pass states only *that* it uses a resource
  shaped by the description; whether that usage reads or increments an epoch is encoded by the
  description, not chosen at the pass. The two-relation grammar (Read/Increment, plus
  sub-declaration) is spoken *inside* descriptions, one level down. The single-verb pass surface is
  the requirement; the exact verb name is open.
- There are no pass-local slots and no slot-specific declaration functions. A declaration's
  identity is its ledger node, minted when the declaration is recorded; the annotated member the
  result is assigned to is the source-generator and analyzer anchor, not an identity. *(This
  supersedes the earlier `(pass, slot)` scheme and the generated per-slot declaration functions.)*
- `IGraphResource<T>` is the central foundational wiring object: a behavior-free handle
  carrying a single `Fetch()`, declared against the ledger and resolved later to the live
  instance of its resource type. Its implementation's sole responsibility is to return the
  correct instance for the current frame; per-frame backing rotation lives inside that instance,
  so a stable handle resolves correctly each frame without re-declaration. The handle encodes no
  read/write, descriptor, synchronization, upload, ownership, or lifecycle behavior. Fetching
  from a handle in the same hook execution it was declared in is undefined behavior.
- Intra-pass wiring between declarations flows through **declaration products**: transaction-
  minted references exposed on the description instance after it was declared (see Resource
  Model, Descriptions and Declared Facts).
- Additional resource behavior is exposed by interfaces implemented by the resource type
  and wired by generated or manually implemented pass contracts.
- `Execute` receives an implementation-specific payload chosen by the concrete stock graph type
  for its pass type. The concrete shape is open — it may be a wrapper object or a direct
  `VkCommandBuffer` pass-through. There is no central named command-context abstraction; earlier
  design iterations named one and it has been dropped.
- Stock pass abstractions may use attributes, generated wrappers, generated metadata, generated
  hook dispatchers, and generated binding code to wire resource-dependent logic in place.
- Anything source generation emits for pass/resource contracts must be implementable manually.
- Advanced mods may write the contract code directly or introduce a more suitable generator.

Passes should not manually create or destroy Vulkan objects for normal stock graph resources.
Ownership and cleanup belong to stock resource/view contracts and graph orchestration. Passes may
still call stable DI services and may use raw Vulkan where the stock abstraction intentionally
leaves an escape hatch.

### Pass Dependencies

Pass dependencies are split into two categories:

- **DI services** are stable APIs such as camera controllers, settings, scene services, shader
  managers, or other long-lived capabilities. Pass objects can be DI-constructed like other
  Sparkitect services.
- **Graph resources/views** are data and access surfaces that participate in graph execution.
  They are declared in setup and fetched through handles during execute.

If data participates in graph ordering, synchronization, frame resolution, or generated
lifecycle hook work, it should be modeled as a graph resource/view in the stock graph. If a pass
calls a stable service API, it should use DI.

If a value is derived from graph data, it is reached through a graph resource — composing a
resource that carries the derived value (such as an element count alongside a GPU buffer) is the
prescribed shape. Reaching through DI to input-side services for values the graph's own data
flow produced is the canonical violation of this boundary.

## Source Generation And Manual Contracts

Stock render graph ergonomics are source-generation backed, following the same style as
Stateless Functions and ECS ComponentQueries. The generator is member-declaration driven, not
setup-code driven.

A pass-local field or property annotated as a graph resource is the generator trigger. The
member's resource type is the generator's source of truth for that member's declaration.
Setup code supplies runtime declaration values, but the generator must not infer resource
semantics by interpreting arbitrary setup method bodies.

The stock path should follow this pattern:

1. The user writes a small partial pass class using stock pass abstractions.
2. Annotated graph-resource members define the pass's resource surface; the member's resource type
   is the generator's source of truth.
3. The generator inspects each resource type and the hook-shaped interfaces it implements.
4. The generator emits pass/resource metadata, binding/fetch code, and strongly typed lifecycle
   hook dispatchers.
5. Setup assigns declaration results to the annotated members; analyzers verify assignment and
   type agreement between member, verb, and description.
6. Runtime graph building consumes the generated or manually written contracts; declarations land
   in the ledger.
7. The compiled plan interleaves emitted synchronization, hook dispatch, and pass `Execute`.

This avoids deep runtime inference while keeping day-to-day authoring small. It also keeps the
system open: generated contracts are just code. When the stock generator is not suitable, a mod
can manually implement the same contracts or provide a custom generator.

Companion analyzers should validate that:

- Annotated members use supported graph handle shapes and are assigned during setup from
  declaration verbs whose resource type matches the member.
- Graph handles are fetched only from valid execution-time code paths.
- A description type implements exactly one description-interface instantiation (protects single-
  generic inference; ships with the feature so violations surface as domain diagnostics, not raw
  compiler errors).
- Descriptions are immutable-shaped (sealed records, init-only members); declared-facts types do
  not smuggle descriptions or arbitrary behavior back into the graph.
- Description product properties are assigned only within declaration logic and read only after
  the description was declared.
- `Identification`-bearing description parameters are validated by the general `Identification`
  usage analyzer family — symbolic-usage enforcement and back-link registration checks. That
  family is general engine infrastructure specified outside this document; the render graph is
  its first consumer and depends on it.
- Manually implemented contracts match the generated contract shape.

Generator design must account for existing Sparkitect constraints:

- Do not require one generator pipeline to see another pipeline's generated output.
- Extract information from user-declared syntax and symbols, then emit code that compiles after
  all generated output is combined.
- Prefer ID-mapped metadata and entrypoint-container collection where it matches existing
  `ApplyMetadataEntrypoint<T>` patterns.
- Keep generated contracts focused. Avoid storing metadata that no runtime contract consumes.

## Resource Model

Resource management is the central stock render graph design problem. The computational-graph core
defines the generic dataflow machinery (L1); *resource* semantics — backing, views,
synchronization — are the L2 specialization.

All resource behavior derives from one structure: the **declaration ledger** (an L1 construct).
Setup populates the ledger; compilation resolves and validates it; the frame runtime executes the
plan derived from it. The full resource graph — every resource, every relationship, every ordering
edge — is reconstructable from the ledger alone, without executing a frame. No relationship exists
only as an anonymous runtime value.

### Terminology

**Resource type** — a plain C# type. Type identity is shape-based: descriptions, references, and
handles are generic over the resource type, and the compiler carries the association end to end.
The stock graph requires no per-resource-type registry. (This deliberately reverses an earlier
direction in which a type-level `Identification` routed declarations to managers; routing by type
identity no longer exists.)

**Resource data** — the instances of a resource type. An `IGraphResource<T>` resolves to one
such instance at `Fetch()`. One declaration may be realized by several runtime instances. General
instance multiplication is a deferred capability; until then the graph resolves a single instance,
**except** where a backing is inherently multi-image (the swapchain), whose per-index selection is
handled inside its backing provider's resolve. A pass never observes this.

**Epoch** — a position in a resource's intra-frame dataflow. Epochs are static plan structure:
they are advanced only by declared increments, identical in shape every frame, and authored code
never sees an epoch number — epochs are symbols, resolved during compilation.

**Resource description** — the behavioral unit of declaration. A description is authored as plain
data (constructed freely, typically by a pass), but it carries the logic of what declaring it
means: which sub-resources it requires, which increments it performs, what it hands onward. Its
declaration logic runs exactly once, inside a ledger transaction.

**Declared facts** — the immutable record a description's declaration logic produces. The ledger
stores facts, never the description instance; after declaration the description object is
unreachable by the graph. Facts carry the declaration's resource references and a snapshot of
every parameter that matters, and facts own instance creation: the logic that constructs a
runtime instance consumes the facts and the resolved sub-instances, nothing else.

**Leaf and composite resources** — a leaf resource is realized against a physical backing
(a device buffer, an image); its facts resolve a backing provider at instance creation. A
composite resource is CPU data plus references to existing resources; it has no backing provider
and no manager of any kind. Backing providers are the only per-family runtime services the
resource model retains; all wiring and per-execution value state lives in the ledger and graph
core.

A leaf resource instance carries its own **current physical state** (such as an image's layout)
at runtime; the state required by a given use is carried by the using description, and reconciling
current-against-required at the point of request is what produces a transition — never stored in
the ledger. Leaf instances are built through their per-family backing provider (the image manager
for images); composite and view resources self-resolve by composing already-resolved sub-instances,
so only the small set of physically-backed leaf types depends on a manager. Resolution is lazy and
dependency-first, positioned by the plan rather than performed in one upfront sweep: the render
graph drives the swapchain and informs the image manager of the current acquired index as part of
that resolution.

**Resource reference** — an opaque, typed, epoch-qualified reference to a resource, minted only by
ledger transaction verbs. A reference is valid because the ledger recognizes it; references cannot
be constructed, only received.

**Moment** — a registered `Identification` naming one specific symbolic epoch of one resource: the
result of exactly one increment in the assembled graph. Moments are the only cross-pass reference
mechanism (detailed under Identity).

### Relations and Epochs

The ledger knows exactly two relations:

- **Read** — a participant consumes a resource at a specific epoch.
- **Increment** — a participant advances a resource from one epoch to the next, producing the
  next epoch's content.

There is no separate create, write, publish, produce, or consume vocabulary. Creation, editing,
and external publishing are all increments, distinguished only by provenance (which pass,
declaration, or external door performs them). A `(resource, epoch)` pair is immutable once its
producing increment completes; "mutation" means producing the next epoch.

Every resource has a **base epoch**: its introduced-but-unfilled state. A reference to the base
epoch can be held — it is the required input of the first increment — but never read. This is not
a special rule: a read of any epoch must be ordered after the increment that produces it, and the
base epoch has no producing increment, so reading it is unschedulable by construction. Allocation
is a materialization detail, not a ledger relation; a resource's first readable content is the
result of its first increment (its **birth**).

Two increments declared from the same source epoch are a **fork** — a compile-time error carrying
full provenance. Concurrent writers are therefore not validated against; they are structurally
inexpressible. Branching is expressed as new resources grounded on a shared epoch, not as
competing increments of one resource: a chain of increments signals one resource built in steps;
parallel variants are born as their own resources reading the common base. (Recycling a
no-longer-read base backing is future intelligent-tracking territory, enabled by the ledger but
not committed.)

This two-relation base is deliberately minimal because the rest of the system derives from it:

- Pass ordering derives from Read/Increment edges (data-flow ordering needs no other input).
- Anti-dependencies (reuse of a backing, invalidation of values grounded on an advanced epoch)
  derive from epoch advances.
- Synchronization — barriers, layout transitions, including first-use transitions from
  undefined state — is **resolved at runtime**, at the point a resource instance is requested
  (explicitly by a pass fetch, or transitively as a composite resolves its parts). Compilation
  fixes only *where and in what order* each resource is used; it computes no state. At request time
  the transition reconciles the resource instance's **carried current state** with the **required
  state** of the use. First use is the birth edge, not a special case.
- Producer validation (a referenced resource nobody produces, a resource produced twice) is fork
  and absence checking, uniform everywhere.
- Conditional re-execution (skipping work whose inputs did not change) remains expressible over
  the same single edge kind. This is a sketched direction, not a committed feature; see Runtime
  Manipulation.

### Resource References

Transaction verbs return references eagerly, but epochs are symbols: resolution to concrete
positions happens during compilation, after all declarations are collected. The result of a
declaration must never depend on the order in which passes happen to be set up — collection and
resolution are separate phases.

References are the intra-pass and intra-declaration wiring currency: a declaration that performs
an increment hands the post-increment reference onward through its facts and declaration
products, and a sibling declaration takes it as a parameter. References never cross pass
boundaries; there is no dataflow path between passes at setup time, by design. Cross-pass wiring
is symbolic, through moments.

Holding a reference and reading it are distinct capabilities. Holding is free (facts hold
references to base epochs and intermediate states); reading requires a producing increment and
creates an ordering edge.

### Descriptions and Declared Facts

Declaration has two levels. A pass declares its resource usage through `use(description)`; the
description's declaration logic, invoked inside the resulting transaction, speaks the two-relation
grammar (Read/Increment, plus sub-declaration) one level down. Provenance differs — ledger entries
attribute to the pass or to the declaring description — and so does the surface: the pass states
only *that* it uses a resource, while the relation semantics live in the description. Composition
is recursive declaration: a description declares its parts, which may themselves be composites. A
description's logic may read or increment the sub-resources it composes **and** the resource it
itself resolves to — advancing the resolved resource directly (a transient born in place, or an
existing referenced resource taken to its next epoch) is first-class, not only incrementing others.

A description's declaration logic runs exactly once, inside the transaction, and that single
execution is the parameter snapshot: whatever the description's mutable surface held at that
moment is captured into the returned facts, and later mutation of the description object is
inconsequential because nothing graph-side retains it. Facts must be immutable and must not
smuggle the description or arbitrary behavior back into the graph; analyzers enforce the shape.

Instance creation runs once per multiplied runtime instance and is owned by the facts. It
receives the resolved sub-instances for its references and, for leaf resources, the backing
provider surface. It cannot consult the original description, by construction.

Illustrative shape (type names not final), using the canonical staging example — a resource that
accepts CPU data, owns a host-visible buffer and a device buffer, and performs one increment of
the device buffer:

```csharp
public sealed record StagingDescription : IResourceDescription<StagingBuffer>
{
    /// Set during declaration: the device buffer after this description's staging copy (X:1).
    /// Reading it before the description has been declared is an error.
    public ResourceRef<DeviceBuffer> PopulatedBuffer { get; private set; }

    public DeclaredFacts<StagingBuffer> Declare(IResourceTransaction tx)
    {
        var host   = tx.Declare(new HostBufferDescription());   // host, base epoch
        var device = tx.Declare(new DeviceBufferDescription()); // X, base epoch
        var staged = tx.Increment(device);                      // X -> X:1, minted here

        PopulatedBuffer = staged;                  // pass-side exposure of the minted ref

        return new StagingFacts(host, device, staged);   // graph-side truth
    }
}

public sealed record StagingFacts(
    ResourceRef<HostBuffer> Host,
    ResourceRef<DeviceBuffer> Device,
    ResourceRef<DeviceBuffer> PopulatedBuffer) : DeclaredFacts<StagingBuffer>
{
    public override StagingBuffer CreateInstance(IInstanceContext ctx) =>
        new(ctx.Resolve(Host), ctx.Resolve(Device));
}
```

**Declaration products.** A description may expose transaction-minted references on itself —
properties assigned during its declaration logic, read by the declaring pass afterward to wire
sibling declarations. The graph never reads these properties; graph-side truth is the facts. The
exposure direction is therefore strictly asymmetric: the graph keeps facts, the pass keeps the
description. Misuse fails fast — an unassigned product is an unminted reference, rejected with
provenance at the consuming declaration, and analyzers additionally enforce assign-before-read
ordering within setup.

**A description instance may be declared at most once.** Re-declaring the same instance would
re-run its declaration logic and overwrite its products, silently cross-wiring consumers; the
ledger detects instance reuse and rejects it. Descriptions are cheap records; each declaration
gets a fresh instance.

Resources never behave like passes: a resource instance has no setup of its own, and everything a
description declares is instance-invariant.

### Identity

A resource reference takes one of two forms, **symbolic** or **value**. These are stock
engine-layer semantics; the foundation provides only the declaration and binding substrate and
treats handles opaquely.

- **Symbolic** — a description. Descriptions are statically analyzable and may embed an
  `Identification` to reference a registered moment — the only place an instance-level
  `Identification` reference lives. The stock layer resolves symbolic references during
  compilation; authored code never resolves them.
- **Value** — a resource reference or bound handle. Value identity is the ledger node, minted when
  the declaration is recorded — never derived from location, naming, or declaration order.
  Provenance (which pass, which declaration, which sub-declaration) is metadata on the node, not
  the identity itself. This identity is not author-facing — a pass declares and receives a handle,
  nothing more — but it is the stable address graph services key on for dependencies, diagnostics,
  and tooling. (This supersedes the earlier `(pass identity, slot)` scheme; pass-local slots no
  longer exist.)

Cross-pass wiring is carried by **moments**. A moment registration is method-level, follows the
ordinary registry pattern, and yields an SG-emitted `Identification` property like every other
registration. The registration declares the moment's resource type; it declares nothing about
backing, position, or producer. A moment enters a description as a constructor `Identification`
argument: a produce-usage description's internal increment marks the moment, and a consume-usage
description's internal read references it. The pass surface carries no marking verb or parameter —
both publishing and reading a moment are expressed through the description. At setup exactly one
increment in the assembled graph is **marked** with the moment, and the moment thereafter resolves
to that increment's result epoch.

- Marking is publishing. Unmarked epochs are private dataflow; the set of registered moments is a
  pipeline's deliberate, moddable surface — chosen the way public API is chosen.
- A referenced moment with no marked increment is an error naming the moment and its readers. Two
  marked increments for one moment are an error with both provenances. Both surface at
  compilation, before any frame runs.
- Because positions are inferred and never authored, a mod may restructure a chain — replacing one
  marked step with several, redefining which increment carries an existing moment — and every
  reader re-links unchanged.

Type safety for moment-embedding descriptions is layered: the description's generic association
carries the resource type in C#; the general `Identification` usage analyzers (of which the render
graph is the first consumer) validate symbolic usage and registration shape at edit time; graph
compilation re-validates as ground truth, since mod composition is only known at runtime.

One invariant holds across both forms: every readable epoch has exactly one producing increment,
and every relationship edge lands in the ledger. The full resource graph is reconstructable from
declarations alone.

### Resource Composition Examples

"View" is an emergent label, not a foundational kind (see Design Anchors): these are usage-shaped
compositions over generic resources and one universal backing, not a separate resource category.
The stock graph and compatible extensions must support compositions broader than Vulkan image
views:

- **Render target view:** A view suitable for writing render output. It may resolve to a
  registered image or to the current acquired swapchain image.
- **Staging buffer view:** A view over CPU data and GPU buffer state. Canonically modeled as a
  composite description that declares a host-visible leaf, a device leaf, and one increment of
  the device resource, handing the post-increment reference onward through its declaration
  products.
- **Descriptor-facing buffer/image view:** A view that resolves descriptor type, binding, image
  layout, buffer range, descriptor update requirements, and bind behavior.
- **Entity list view:** A view over renderable entity data prepared by ECS systems or game logic.
  The graph sees data produced by systems or services; how those systems derived the data stays
  outside the resource view contract. The pushed list is consumed through a composite resource
  that carries the GPU buffer reference alongside its CPU-side metadata (such as the element
  count), so passes read one resource rather than re-deriving facts from input-side services.
  The list lives inside the resource — multiplicity is an internal detail of the pushed data, not
  a foundational one-to-many handle.
- **Voxel world composite view:** A view over chunk buffers, world-level acceleration data, and
  CPU metadata. It may expose world/chunk semantics rather than a single raw buffer.
- **Multi-resource mapping view:** A view that wraps several existing resources, such as three
  images used together for a custom material or mapping scheme.

Views can be one-to-one, one-to-many, many-to-one, or composite. Two passes may use completely
different resource types that overlap the same physical backing resources. Stock engine contracts
must preserve correctness across those overlaps.

### Cleanup

Every resource declares one cleanup strategy: **None** (nothing to dispose — e.g. a composite of
sub-resources plus CPU metadata), **Dispose** (direct disposal of an owned object — e.g. a view
resource disposing its `VkImageView`), or **Release** (manager-backed resources signal release and
the backing provider decides what that means). Release is the substrate on which memory aliasing
and pass-transient resource reuse are later built: the graph signals release; the provider owns the
policy (no-op, pool-return, alias-reuse). Cleanup dispatch rides the Cleanup lifecycle hook. A
resource never contains or knows of views over it, so releasing a backing is independent of any
view's disposal.

### Pushed Data and Externally-Managed State

All resources are **graph-owned**. Some graph-owned resources are **pushable** — they accept data
fed from outside the pass pipeline. A pushable resource is a registered moment whose configuration
declares it externally pushed; the configuration is `Identification`-mapped metadata, carried by
the ordinary metadata mechanism.

Publishing hands the data to the graph: the live, freely-mutating collection and the value a frame
works on are two distinct objects, and the publish surface makes post-publish mutation fail fast —
a recording-style handoff (the exact seal-versus-swap mechanism remains open). At frame start, push
machinery performs the **birth increment** of each externally pushed moment, binding the most
recent published snapshot. If nothing new was published, the frame binds the previous snapshot;
re-binding is unconditional, and whether dependent work could be skipped belongs to the
conditional re-execution sketch, not to committed behavior.

A push lands only at the chain head — a pushed resource is by design the first entry in its epoch
list. From the birth increment onward its chain is ordinary graph territory: passes may read it
and increment past it like any other resource.

The external-push configuration is itself a modding axis. A mod may reconfigure an existing moment
away from external push and define it by marking a pass increment instead — for example, pushing a
different raw format under its own moment and inserting a transform chain that produces the
expected input. Readers re-link against the unchanged moment. Conflicts stay loud and
configuration-aware: a pass marking an externally-configured moment is a duplicate definition
whose error names the configuration; a reconfigured moment with no marking increment is an
undefined moment.

**Externally-managed state is not a special resource.** The swapchain is an ordinary image
resource whose backing origin is the swapchain rather than a VMA allocation; its backing is owned
and recreated by stock window/swapchain infrastructure, not the graph. It is delivered to the graph
as external state through a dedicated handler — not the push path — and the render graph itself
drives swapchain acquisition and presentation. Per-image-index selection lives in the resource's
resolve, so a resolved swapchain image is scoped to the current acquired index. Presentation is
modeled in the relation grammar: the graph reserves a single **finishline moment**, and whichever
increment drives the presentation target to its present-ready epoch marks it. The graph references
that moment as a consumer; presentation and its present-layout transition are issued by the graph
at the finishline position. An unmarked finishline is an undefined-moment error; two markings are a
duplicate-definition error — the ordinary moment diagnostics.

## Graph Compilation And Execution

The stock render graph compiles setup declarations into an execution plan. The runtime graph
builder should combine explicit generated/manual contracts, not inspect arbitrary pass logic.

Compilation runs in two strictly separated phases, because no declaration outcome may depend on
the order in which passes happen to be set up:

**Collection.** Pass setup runs; declarations land in the ledger. Descriptions' declaration logic
executes inside the transactions this opens, attributing nested entries to their declaring
declaration. Epochs are recorded as symbols; references are handed out eagerly but resolve to
nothing yet. Moments are recorded as markings on increments.

**Link.** With the ledger complete, the graph resolves and validates:

- Epoch symbols resolve to concrete positions per resource chain.
- Each referenced moment binds to exactly one marked increment. Zero is an error naming the
  moment and its readers; two is an error with both provenances. External-push configuration
  participates here: an externally pushed moment's birth increment is provided by push machinery,
  and conflicts between configuration and markings are reported configuration-aware.
- Forks (two increments from one source epoch), reads of unproducible epochs, and contradictory
  grounding (one consumer transitively requiring two different epochs of one resource whose
  backing cannot hold both) are rejected with full provenance.
- Moment type expectations from registrations are checked against the marked increments' resource
  types — the runtime ground truth behind the edit-time analyzer checks.

**Plan emission.** From the linked ledger the graph derives: pass execution order (from
Read/Increment edges, plus explicit ordering escape hatches); the epoch-edge structure that
synchronization is resolved against at runtime (barriers and layout transitions, including
first-use transitions, are issued when a resource is requested — reconciling its carried current
state with the use's required state — not precomputed here); the positions where inherently
multi-image backings are index-selected; and the scheduled positions of lifecycle hook dispatch,
ordered ledger-topologically (see Render Graph Lifecycle Hooks).

### Ad-hoc / Precompiled Hybrid

The stock graph deliberately blends two models. **Precompiled:** structure, ordering, moment
binding, and validation are resolved once at compile/link, giving determinism and full diagnostics.
**Ad-hoc:** physical state — instance creation and layout/barrier transitions — is resolved lazily
at runtime, when a resource is explicitly or transitively requested, never baked into a static
barrier list. The plan knows *when, where, and in what order* each resource is used; it does not
know the concrete instance or its state until that instance is requested. This keeps compile-time
correctness while letting state track real runtime conditions.

### Data-Flow Ordering

Data-flow ordering is the primary stock pass ordering mechanism, and it is fully defined by the
ledger: every Read of an epoch orders the reader after that epoch's producing increment; every
Increment orders after its source epoch and after that epoch's declared readers (the
anti-dependency that makes backing reuse safe). No other ordering input exists in the data-flow
layer — there are no access-mode enums and no inferred semantics; richer access information lives
in resource types as static metadata consumed by synchronization emission, never by ordering.

Edge semantics:

- Optional edges whose target or source node is missing are ignored.
- Non-optional edges with a missing source or target are errors.
- Cycles are hard errors.
- Forks are hard errors (the multi-writer case is structurally inexpressible, not validated
  against).

Explicit ordering (class-level ordering metadata between passes) remains a supplementary escape
hatch. It is expected to disappear from well-formed graphs as data-flow edges take over — a
sample pass carrying an explicit edge where a moment read would order it is a smell the migration
should eliminate.

### Render Graph Lifecycle Hooks

The render graph uses lifecycle hooks, but not as foundation resource lifecycle hooks. The
foundation does not define resources, resource views, Vulkan synchronization, descriptors,
allocation, or resource lifecycle semantics. Render graph hooks are emergent graph-level
extension points discovered from pass types and resource types.

A hook-shaped interface can be implemented by:

- The pass type itself.
- A resource type used by an annotated pass resource member.

The source generator discovers hook-shaped interfaces generically. It must not hardcode every
stock or mod-defined hook interface. For each discovered hook on a pass, the generator emits one
strongly typed pass-local dispatcher. The graph runtime calls that generated dispatcher at the
matching lifecycle phase.

```text
graph lifecycle phase
  -> generated pass hook dispatcher
      -> optional pass-level hook implementation
      -> resource view hook for declaration A
      -> resource view hook for declaration B
      -> resource view hook for declaration C
```

Every call inside the generated dispatcher is strongly typed. This resolves clashes between
user-authored pass hook code and resource-view hook code: both are hook sources, and the
generated dispatcher is the single wiring point that binds declarations and composes the hook
calls.

**Dispatch order is ledger-topological, not declaration-ordered.** Within one pass's execution
window, several declarations may have contributed hook work and declared increments — a staging
copy advancing one resource, a sibling reading the advanced state. The generated dispatcher's
composition order must respect the intra-window ledger edges; member or declaration order is not
a valid tiebreaker where an edge exists. (The ledger already contains the required edges; for
unrelated hook work any stable order remains acceptable.) *This supersedes the earlier "chosen
stock composition order" rule.*

**Hook insertion is graph/generator responsibility.** Resource lifecycle hook calls are inserted
by the generated dispatchers and scheduled by the plan; passes do not manually invoke per-
declaration hook methods. A pass may still override or extend dispatch through its own hook
implementations — the override capability is the reason dispatchers are generated into the pass.

**An increment declared by a description is realized as resource behavior in the declaring pass's
window.** The staging description declares `X -> X:1`; the work that makes it true — the buffer
copy — is authored on the staging view through a hook-shaped contract and dispatched by the graph
at the increment's planned position, with synchronization resolved at runtime around it. Pass
`Execute` neither performs nor orders this work.

The initial lifecycle categories are:

- Graph building / compilation.
- Frame binding.
- Pre-execution.
- Execute.
- Post-execution.
- Resize handling.
- Cleanup.

Source generation must remain agnostic to the specific stock hook list. It discovers hook-shaped
contracts generically and wires them without hardcoding a privileged fixed set of lifecycle
interfaces.

Examples of legitimate hook work include:

- Copy CPU data into a mapped or staging buffer at a declared increment's planned position.
- Update or push descriptor payloads before command recording.
- Bind descriptor sets or pipelines.
- Acquire a swapchain image before the first pass that targets it.
- Present at the finishline moment's position, after the increment that marks the presentation
  target as present-ready.

Moved out of hooks and into plan output: image layout transitions before writes, barrier
placement, publishing a resource version after a write (superseded by epochs), and marking a view
as updated for conditional execution (belongs to the conditional re-execution sketch).

These hooks belong to stock engine resource/view contracts or extension-defined contracts. They
are not foundation resource lifecycle hooks.

### Runtime Manipulation

Committed behavior:

- **Structural changes** add or remove passes, declarations, or graph-visible relationships. They
  trigger graph rebuild: collection and link run again, and re-running setup re-snapshots
  description parameters.
- **Runtime disable** skips a pass while preserving compiled topology when the pass remains part
  of the graph.
- The frame runtime executes every scheduled operation every frame. There is no committed
  change-driven skipping.

> **Sketched direction — conditional re-execution (not committed).** The ledger retains enough
> per-operation provenance that a change-driven skip evaluator could be added without
> re-architecting: cross-frame change stamps ("generations") per value, per-operation memoization
> of input stamps, and a global run-set evaluation in which no participant — including the push
> door — decides locally; eligibility propagates forward along dataflow, and required re-runs
> propagate backward along a backing's epoch chain (a chain skips suffix-closed or reruns
> prefix-closed; there is no skipping the middle). Known brittle areas: purity of passes that read
> live services during Execute, and data residency across instance rotation. Recorded so the
> committed model stays checked against it; unvalidated by any current sample, carries no
> requirements weight.

Structural conditionality — a pass or generated contract not included because required
declarations cannot resolve — remains engine-layer semantics.

## Vulkan And Compute Scope

The initial implementation target is compute-first. Rasterization is future scope.

The stock Vulkan render graph must cover:

- Compute pass nodes.
- Storage images.
- Storage buffers.
- Descriptor sets.
- Push constants.
- Compute dispatch.
- Command-buffer access for pass Execute — implementation-specific payload (wrapper object or direct `VkCommandBuffer`), with raw Vulkan preserved as an escape hatch.
- Swapchain acquisition, presentation, synchronization, and resize invalidation.
- VMA allocator integration as a single-per-device engine service.
- Migration of Space Invaders and Pong from raw per-mod Vulkan orchestration to graph compute
  passes.

Supporting Vulkan abstraction work is expected to cover the Vulkan API surface that Sparkitect
mods and samples currently hand-roll around rendering: command pools/buffers, sync objects,
descriptor plumbing, VMA-backed storage resources, swapchain-relative targets, and related frame
execution concerns.

Current sample rendering is effectively:

1. Prepare CPU/game data.
2. Write or bind buffers/descriptors.
3. Dispatch compute into a VMA storage image.
4. Transition and blit into the acquired swapchain image.
5. Present.

The stock graph should absorb the repeated boilerplate: command pools/buffers, fences,
semaphores, acquire/present, descriptor update plumbing, plan-emitted barriers and layout
transitions, resize invalidation, storage-image-to-swapchain blit, cleanup ordering, and frame
resource duplication.

Pass-specific code should remain in passes: shader choice, resource declarations, push constant
values, dispatch dimensions, unusual custom synchronization, and the actual Vulkan work expressed
through pass code and resource/view lifecycle hooks.

The graph should not know about windows directly. Window binding enters the graph through a
swapchain target resource/view and its associated stock contracts.

### Emergent GPU Concerns

These concerns are stock/extension-layer concerns, not foundation concepts:

- **Multi-queue scheduling**: not needed initially, but the design must not prevent graphics,
  compute, and transfer queues later.
- **Memory aliasing**: reusing a backing is an epoch advance with re-grounding — the ledger makes
  the legality computable (old-epoch readers schedule first). Exploiting this for transient
  resources is future stock/extension behavior, enabled but not committed.
- **Frames-in-flight**: realized as central instance multiplication — one ledger node mapped to
  per-frame instances, with the epoch-to-instance mapping owned by the graph core and resolved
  transparently at `Fetch()`.
- **Subresources**: mips, layers, buffer ranges, and other backing-resource slices are view-layer
  semantics.

## Registration And Integration

The render graph is standalone infrastructure, independent of GameState in the sense that it is
not a special GameState primitive. However, stock graph instances are expected to be created,
owned, and driven by GameState-driven infrastructure, analogous to how ECS worlds are created and
owned by the active game state.

Registration should follow standard Sparkitect patterns:

- Pass registration through a stock pass registry or generated registration path.
- **Moment registration** is the graph's primary registration path: method-level, ordinary
  registry mechanics, SG-emitted `Identification` properties. A moment registration declares
  name and resource type — never backing, position, or producer.
- **No per-resource-type registry is required.** Resource types participate by shape.
  *(Deliberate reversal of the earlier type-routing direction.)*
- External-push configuration for moments is `Identification`-mapped metadata through the
  ordinary metadata mechanism, and is itself a modding surface.
- Backing providers and graph-core services register as graph-local services; shader and Vulkan
  services go through existing Vulkan module infrastructure where appropriate.
- Mod-defined views, descriptions, and handlers use the same public extension contracts as
  engine-defined ones.

Graph composition may be driven by explicit graph-owner configuration, attribute/entrypoint-driven
registration metadata, or a combination of both. The exact stock composition rules remain open.
The foundation does not require per-resource-type registries, and the stock layer defines none.

## Validation And Debugging

Validation belongs mostly to the stock engine layer and compatible extensions.

Expected validation areas:

- Unbound or fetched-before-bound handles.
- Unsupported resource description/view combinations.
- Conflicting stock view semantics over overlapping physical resources.
- Missing generated/manual contracts.
- Swapchain-relative resources that need recreation after resize.
- Resource leaks, especially Vulkan/VMA objects.
- Dependency visualization and pass/resource graph inspection.
- Debug display of generated declarations, view bindings, and lifecycle hook dispatchers.

Link-stage validation, all with full provenance:

- Undefined moment: referenced, never marked — names the moment and lists its readers.
- Duplicate moment definition: two marked increments — both provenances; configuration-aware
  when external-push settings conflict with markings.
- Fork: two increments from one source epoch.
- Read of an unproducible epoch (including base epochs).
- Contradictory grounding: one consumer transitively requires two epochs of one resource that
  cannot coexist on its backing — reported as the spelled-out diamond (both paths, both epochs),
  not as a bare cycle. Legality is backing-policy-dependent; the diagnostic states which policy
  would admit the shape.
- Moment type mismatch between registration, marking increment, and readers.
- Description instance reuse: the same description instance declared twice.

Cross-mod contradictory grounding — two independently-correct mods whose combination grounds
contradictorily — is diagnosable with the same machinery; resolution strategies beyond diagnosis
are deliberately out of scope until real data exists.

The foundation should validate only protocol-level misuse. It should not attempt to validate
Vulkan layouts, descriptor compatibility, staging safety, or graph resource overlap without a
stock/extension contract that gives it those semantics.

The initial implementation does not need a dedicated diagnostics layer or an additional
deterministic ordering feature beyond what falls out of explicit constraints and the chosen graph
resolution algorithm. Cycle, fork, and link validation remain mandatory.

## Initial Implementation Scope

The redesign is proven by the smallest stock feature set that exercises every model element:

- The ledger, the two relations, epoch linking, and plan-emitted ordering and synchronization.
- The description/facts/instance pipeline, including declaration products and the
  one-instance-per-declaration rule.
- Leaf buffer and image descriptions backed by graph-local backing providers.
- Moment registration, marking, linking, and external-push configuration.
- The staging composite and a published GPU-side composite carrying CPU metadata (the entity
  list with its count), consumed across passes through moments.
- Descriptor composites deriving layouts from static view metadata at declaration.
- Central instance resolution behind `Fetch()` (single in-flight frame initially).
- Migration of the sample renderers onto the model, with explicit ordering edges eliminated in
  favor of data-flow ordering.

Out of scope: conditional re-execution (sketched only), rasterization, multi-queue scheduling,
backing-recycling intelligence, and frame-graph optimizations.

## Open Questions

- For a pushable resource, should the publish handoff seal the data object in place (further
  writes fail) or swap it for a fresh recording target? The fail-fast / recording-handoff intent
  is settled; the mechanism is not.
- What is the minimal foundational pass/declaration/binding protocol?
- What exactly do the stock pass/resource source generators emit, and how are hook dispatchers
  represented in generated/manual contracts?
- Exact names of the *description-internal* transaction verbs (read, increment, sub-declaration).
  The pass surface is settled — a single `use(description)` verb, with relation semantics and
  moment marking carried inside the description, not as pass-level verbs or parameters.
- The return shape of moment registration methods — the single symbol both analyzers and link
  read the moment's resource type from.
- How leaf descriptions reach backing providers at instance creation (the one place the design
  bends never-pull: scoped provider lookup on the instance context vs dedicated transaction
  primitives for physical leaves).
- Residency and instance policy for resources carried across frames once multiple frames are in
  flight (single-instance-with-sync vs copy-on-advance), and its interaction with the
  conditional re-execution sketch.
- Should swapchain frame management (image acquisition, present-layout transition, present
  submission) be modeled as explicit *early* and *late* swapchain passes that participate in
  normal graph ordering? The uniform increment grammar strengthens the case; unresolved.
- Descriptions parameterized from live services (swapchain extent): rebuild re-runs setup and
  re-snapshots — is rebuild granularity sufficient for all resize paths?
- Cross-mod contradictory grounding: diagnosis ships; resolution policy deferred until data
  exists.

## See Also

- <xref:sparkitect.core.stateless-functions> for the generic function/category/source-generation
  pattern.
- <xref:sparkitect.tooling.source-generation> for Sparkitect source generator conventions.
- <xref:sparkitect.ecs.ecs-design> for the foundational/emergent layering pattern and generated
  query bridges.
- <xref:sparkitect.ecs.ecs-requirements> for ECS requirements and component query access metadata.
- <xref:sparkitect.core.registry-system> for `Identification` and registry patterns.
- <xref:sparkitect.core.dependency-injection> for DI and entrypoint-container patterns.
- <xref:sparkitect.vulkan.vulkan-graphics> for current Vulkan wrappers and resource tracking.
- <xref:sparkitect.vulkan.shader-compilation> for shader registration and loading.
