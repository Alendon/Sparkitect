---
uid: sparkitect.rendergraph.requirements
title: Render Graph Requirements
description: Requirements and design constraints for the render graph
---

# Render Graph Requirements

This document captures the current render graph design direction. It is intentionally
requirements-level: exact type names, source generator contracts, and implementation files are
still open, but the architectural boundaries between the foundational substrate and the stock
emergent render graph are locked here.

## Design Anchors

- The foundational layer is a generic executable graph: pass (node) identity, declaration capture,
  hook-driven binding and fetch, opaque handles, and extension contracts. It owns no GPU, resource,
  or data semantics — *resource* is not a foundational concept; it is composed entirely in the stock
  layer. The foundation exists to make the graph modular and modding-friendly; it is not the surface
  authors use day to day.
- Sparkitect's stock render graph is an emergent engine-layer implementation built on that
  substrate. It owns stock resource/view semantics, synchronization, lifetime, validation,
  command recording orchestration, and Vulkan integration.
- The stock render graph is the shipped render-graph experience. Its ergonomics are first-class, not
  a thin pass-through over the generic foundation. The foundation's genericity buys modularity and
  modding reach; it must not dilute stock authoring.
- The render graph is primarily an orchestration layer. Passes and resource/view contracts
  provide the actual rendering and Vulkan logic; the graph builds, binds, schedules, and invokes
  them.
- The system is built horizontally, not vertically. Foundation does not define a universal
  resource hierarchy that stock Vulkan resources and mods inherit from. Instead, stock engine
  layers and mod layers cooperate through generated or manually implemented contracts.
- Pass declarations should be source-generation friendly. Generated contracts must remain
  manually implementable for advanced mods and unusual resource models.
- Runtime graph building should consume explicit generated or hand-written contracts. It should
  not infer deep semantics from arbitrary pass code.
- Resource views are graph-level semantic views, not Vulkan `VkImageView` objects.
- There is no resource lifecycle hook model in the foundation. The stock implementation uses
  generated or manually supplied render graph lifecycle hooks at the emergent graph layer.
- No authoring role is privileged. Engine code, game code, and mods use the same public
  contracts, registration paths, and extension points.

## Layer Model

### Foundational Substrate

The foundation defines the minimum protocol needed for graph-like systems to exist:

- Pass lifecycle shape.
- Pass identity and registration substrate.
- Setup-time declaration capture.
- Hook-driven binding and fetch protocol.
- Opaque handles that bridge setup declarations to frame execution.
- Extension points for generated and hand-written contracts.
- Protocol-level validation, such as detecting an unbound handle fetch.

The foundation deliberately does not define:

- Physical resources or resource views.
- Vulkan images, buffers, descriptor sets, queues, layouts, or barriers.
- Read/write/produce semantics.
- Synchronization rules.
- Allocation or aliasing policy.
- Frames-in-flight policy.
- Swapchain behavior.
- Resource overlap validation.
- Graph dependency semantics beyond what emergent layers provide.

This mirrors the ECS and Stateless Function direction: the core establishes a small protocol,
while concrete behavior emerges from category-specific contracts, source generation, registries,
metadata, and runtime managers.

### Stock Engine Render Graph

The stock engine render graph is Sparkitect's shipped render graph feature. It is an emergent
implementation over the foundation, not the foundation itself.

The stock layer provides:

- Pass abstractions such as render and compute pass bases.
- Stock resource declaration APIs.
- Stock resource managers and resource view contracts.
- Physical resource and resource view modeling.
- Pass graph compilation and dependency ordering.
- Frame-resolved bindings.
- Render graph lifecycle hook dispatch.
- Command-buffer access for pass Execute — implementation-specific payload (wrapper object or direct `VkCommandBuffer`), with raw Vulkan preserved as an escape hatch.
- Descriptor update/bind orchestration.
- Stock synchronization, layout transition, transfer, and presentation behavior.
- Debugging and validation tooling.
- GameState integration through normal Sparkitect infrastructure.

Stock graph instances are not engine-global singletons. Like ECS worlds, they are created and
owned by GameState-driven stock infrastructure for the relevant state/module lifetime.

Mods can extend or replace portions of this stock behavior by adding compatible views, handlers,
pass abstractions, generated metadata, or hand-written contracts. A full replacement graph is not
the intended common path, but the architecture must not make it impossible.

### Vulkan Backend Layer

Vulkan-specific behavior lives in the stock engine layer or in Vulkan-specific extension layers.
The foundation does not know about Vulkan.

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

- **Setup** declares which resource views the pass needs and how it intends to use them. It runs
  when the graph is built or rebuilt, not every frame.
- **Execute** runs every frame the compiled graph schedules the pass. It fetches previously bound
  views and records or performs the pass work.

Pass authors should be able to write the common case with minimal boilerplate. Resource usage is
declared through annotated pass-local fields or properties. The annotated member records a graph
resource slot, and the member's resource view type is the source-generator source of truth for
that slot.

```csharp
public sealed partial class EntityCopyPass : ComputePass
{
    [GraphResource]
    private IGraphResource<FooStorageBufferView> _storage;

    [GraphResource]
    private IGraphResource<BarEntityListView> _entities;

    public override void Setup()
    {
        _storage = DeclareStorage(...);
        _entities = DeclareEntities(...);
    }

    public override void Execute(ExecutePayload payload)
    {
        var storage = _storage.Fetch();
        var entities = _entities.Fetch();

        // Record pass-specific work through the payload, or via the raw Vulkan escape hatch.
    }
}
```

The exact API shape is open. The important requirements are:

- Setup remains real code. Resource declaration is too dynamic for an attribute-only model.
- Setup binds runtime declarations to generated slots through slot-specific declaration
  functions. These functions already know the target slot identity.
- `IGraphResource<TView>` is the central foundational wiring object: a behavior-free handle carrying
  a pass-local `Slot` and a single `Fetch()`, declared against a slot and resolved later to the live
  instance of its resource type. Its implementation's sole responsibility is to return the correct
  instance for the current frame; per-frame backing rotation lives inside that instance, so a stable
  handle resolves correctly each frame without re-declaration. The handle encodes no read/write,
  descriptor, synchronization, upload, ownership, or lifecycle behavior. Fetching from a handle in
  the same hook execution it was declared in is undefined behavior.
- Additional resource behavior is exposed by interfaces implemented by the resource view type
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

## Source Generation And Manual Contracts

Stock render graph ergonomics are source-generation backed, following the same style as
Stateless Functions and ECS ComponentQueries. The generator is member-declaration driven, not
setup-code driven.

A pass-local field or property annotated as a graph resource is the generator trigger. The
member's resource view type is the generator's source of truth for the slot. Setup code supplies
runtime declaration values for generated slots, but the generator must not infer resource
semantics by interpreting arbitrary setup method bodies.

The stock path should follow this pattern:

1. The user writes a small partial pass class using stock pass abstractions.
2. Annotated graph resource members define generated resource slots.
3. The generator inspects each resource view type and the hook-shaped interfaces it implements.
4. The generator emits explicit pass/resource metadata, slot-specific declaration functions,
   binding/fetch code, and strongly typed lifecycle hook dispatchers.
5. Setup calls slot-specific declaration functions to bind runtime declarations to those slots.
6. Runtime graph building consumes the generated or manually written contracts.
7. The compiled graph calls generated lifecycle hook dispatchers and then pass `Execute`.

Setup declaration functions should be resource-specific. They communicate author intent more
clearly than a universal object payload API:

```csharp
_storage = DeclareStorage(...);
_target = DeclareFrameStorageImage(...);
_swapchain = DeclareSwapchainTarget(...);
```

The declaration functions are generated or manually supplied as part of the pass contract. They
already know their target slot identity, so setup does not need call-site rewriting to bind a
declaration to a slot.

This avoids deep runtime inference while keeping day-to-day authoring small. It also keeps the
system open: generated contracts are just code. When the stock generator is not suitable, a mod
can manually implement the same contracts or provide a custom generator.

Companion analyzers should validate that:

- Annotated members use supported graph handle shapes.
- Annotated members are assigned during setup.
- Setup assignments use declaration functions compatible with the member's resource view type.
- Graph handles are fetched only from valid execution-time code paths.
- Manually implemented contracts match the generated contract shape.

Generator design must account for existing Sparkitect constraints:

- Do not require one generator pipeline to see another pipeline's generated output.
- Extract information from user-declared syntax and symbols, then emit code that compiles after
  all generated output is combined.
- Prefer ID-mapped metadata and entrypoint-container collection where it matches existing
  `ApplyMetadataEntrypoint<T>` patterns.
- Keep generated contracts focused. Avoid storing metadata that no runtime contract consumes.

## Resource Model

Resource management is the central stock render graph design problem. The foundation does not
define resource semantics; the stock engine layer does.

At the stock emergent layer a resource is a composite — a registered type, its instances (the data),
the description that selects them, and the bound handle a pass holds (see Terminology). Resource data
is treated as **physical** (backing storage or device state) or as a **view** (the semantic way a
pass interacts with that backing); the two usually stay distinct, though either may bend its rule.
One physical backing may be surfaced through several view types with different graph meaning,
descriptor behavior, synchronization requirements, or lifecycle hooks. The foundational layer knows
none of this — *resource* is a stock concept.

### Terminology

There is no single resource object. A **resource** is a composite the stock layer assembles, fully
realized only **when bound to a pass**. The foundation never sees a resource — only a typed slot, an
opaque request, and an opaque handle.

**Resource type** — a registered C# type carrying a static `Identification` (one per type). The type
identity *routes* a request to the stock manager responsible for it. It is type-level: it names the
kind, never an instance.

**Resource data** — the **instances** of a resource type. The relationship is the plain C# one: the
resource type is a class; resource data are its instances. An `IGraphResource<TView>` resolves to one
such instance at `Fetch()`. How a backing lives behind that instance is delegated to the stock
manager and need not persist between pass invocations — a manager may retain an instance or produce
one on demand.

**Resource description** — the request a participant supplies when declaring or pushing. It *selects
and shapes*: naming a registered backing, requesting a transient, selecting among known sources, or
composing several. It may embed an `Identification` to reference a registered backing — the only
place an instance-level reference lives. The stock manager interprets it; the foundation passes it
opaquely.

Resource data is *treated* two ways. These are mental categories, **not** foundational types, and
either may bend its rule:

- **Physical** — tied directly to specific GPU objects or concrete CPU data (a device buffer, an
  image, a CPU entity array). A physical instance may also reference other resources when needed.
- **View** — a logical layer over physical resource(s) that modifies their state **directly** rather
  than holding a local copy, so resources gain behavior (descriptor exposure, staging, layout intent)
  without duplicating or re-syncing physical state. A view may also hold its own state when needed —
  a staging view owns a staging buffer and the copy that fills it.

**Bound resource** — what a pass holds: an `IGraphResource<TView>` resolved at a `(pass, slot)`, whose
`Fetch()` resolves to the live resource-data instance. This is the only form the foundation reifies,
and the only point at which a resource is unambiguous.

A view type authors its relationship to the management layer. Through its implemented contracts or
generic shape, a view declares which stock manager interface resolves and maintains its backing
behavior — the manager needs no prior knowledge of every view type, and views remain the flexible
semantic layer over physical resources. Descriptor behavior follows the same rule: descriptor sets,
buffers, allocation/update/bind behavior, and descriptor-facing state belong to resource/view
contracts and their managers; the graph orchestrates when those contracts run but defines no
descriptor semantics. The foundation treats views and handles opaquely — staging uploads, layout
transitions, descriptor updates, frame duplication, subresource selection, and lifecycle hook
behavior all belong to stock or extension resource/view contracts.

### View Examples

The stock graph and compatible extensions must support resource views that are broader than
Vulkan image views:

- **Render target view:** A view suitable for writing render output. It may resolve to a
  registered image or to the current acquired swapchain image.
- **Staging buffer view:** A view over CPU data and GPU buffer state. It can encode size,
  exposed upload buffer, copy timing, and pre-execution transfer behavior.
- **Descriptor-facing buffer/image view:** A view that resolves descriptor type, binding, image
  layout, buffer range, descriptor update requirements, and bind behavior.
- **Entity list view:** A view over renderable entity data prepared by ECS systems or game logic.
  The graph sees data produced by systems or services; how those systems derived the data stays
  outside the resource view contract. The list lives inside the resource — multiplicity is an
  internal detail of the pushed data, not a foundational one-to-many handle.
- **Voxel world composite view:** A view over chunk buffers, world-level acceleration data, and
  CPU metadata. It may expose world/chunk semantics rather than a single raw buffer.
- **Multi-resource mapping view:** A view that wraps several existing resources, such as three
  images used together for a custom material or mapping scheme.

Views can be one-to-one, one-to-many, many-to-one, or composite. Two passes may use completely
different view types that overlap the same physical backing resources. Stock engine contracts
must preserve correctness across those overlaps.

### Identity

A resource reference takes one of two forms, **symbolic** or **value**. These are stock
engine-layer semantics; the foundation provides only the declaration and binding substrate and
treats handles opaquely.

- **Symbolic** — a description or identity that names a resource without being the resolved
  resource: an `Identification`, or a description object — a transient of a given size and format,
  or a discriminated union over registered sources.
  Symbolic references are statically analyzable; the relevant stock or extension handler resolves
  them to concrete values at build or frame time. Resolution semantics are the handler's — a
  symbol may resolve to a shared backing, or act as a specialized factory producing distinct
  backings per use. The symbol guarantees neither.
- **Value** — the actual resolved resource a pass holds. Its canonical instance identity is
  `(pass identity, slot)`: a deterministic per-pass index assigned in declaration order. Integer
  slots give a deduplicated, source-generation-friendly exposition of a pass's resources that
  stays stable as generated or hand-written code adds further declarations. This identity is not
  part of the author-facing surface — a pass declares a resource and receives a handle, nothing
  more — but it is a stable address other render-graph services can key on: expressing
  dependencies, optimizing intra-pass work, or supporting diagnostics. A richer symbol layer —
  resource-type `Identification`, the declaring member name, tracking metadata — maps onto
  `(pass identity, slot)` additively, without replacing the canonical key.

`Identification` is **type-level**: the registered resource type's identity, baked into the type,
routing a request to the managing stock layer. It cannot by itself distinguish two instances of the
same type. Instance selection happens one level down, inside the manager, through the **description**
— which may carry a registered backing's `Identification`. This holds in both directions: a pass
*binds* by description and external code *pushes* by description; the type identity routes and the
description selects. Resource data therefore needs no standalone identity — outside a binding it may
not exist.

Registration is orthogonal to this distinction and to backing. A **registered** resource
carries a public `Identification` and can be referenced symbolically by multiple passes or
systems; a **pass-local** declaration is scoped to one pass or generated contract and needs no
public `Identification`. Registration governs visibility and reference, not whether resolution
shares backing.

A relationship uses whichever form crosses its boundary:

- **Value relationships are intra-pass.** When a pass composes resources it declared itself — a
  descriptor binding its image and buffer views — it relates them by passing resolved value
  handles directly. A value handle never escapes the pass that declared it.
- **Symbolic relationships are external or cross-pass.** A reference that crosses a pass boundary,
  names externally-managed state, or selects among known resources is expressed symbolically, not by
  holding another pass's value handle. The graph resolves the symbol to a value at build or frame
  time.

This split is not stylistic. Cross-pass references are the data-flow edges the compiler orders
passes on; keeping them symbolic keeps every inter-pass edge statically analyzable. Intra-pass
composition needs the resolved value at execution time, so the value handle — which carries both
the resolved view and its `(pass identity, slot)` provenance — is the natural channel.

One invariant holds across both forms: every resource value resolves to exactly one symbolic
source, and every relationship edge is identity-labelled. The full resource graph is therefore
reconstructable from declarations alone, without executing a frame — no relationship exists only
as an anonymous runtime value.

A registered resource binds an `Identification` to a **physical resource** through a description —
for an image, a description carrying size, format, transientness, and an optional default fill, and
carrying no usage. Two registration forms express the symbolic resolution semantics above: one binds
the identity to a single shared backing, the other to a configuration that produces a distinct
backing per use.

What a pass normally consumes is not the physical resource but a **view** over it. A view's setup
request selects its source — a registered resource by `Identification` or an inline description — and
carries the usage and view type. A source backed by externally-managed state, such as the swapchain,
is reached indirectly: the request names a graph resource whose backing the stock layer resolves to
that state (see Pushed Data and Externally-Managed State). Usage is therefore a
property of the view, not of the physical resource: one shared image is consumed by one pass as a
compute-storage view and by another as a transfer-source view. This separation — `Identification`
and descriptions at the physical-resource layer, usage and consumption at the view layer — is
central to how the stock render graph emerges from the foundation.

### Pushed Data and Externally-Managed State

All resources are **graph-owned**. There is no external-resource category; some graph-owned resources
are **pushable** — they accept data fed from outside the pass pipeline (ECS-produced render data, CPU
buffers, mod-owned structures). Pushing is a stock concern the foundation does not see.

A push **routes by resource type and selects its target by description**, exactly as a pass binds. The
pushed-into resource owns its ingestion: it manages allocation internally and exposes a safe, typed
publish/commit surface **on the resource itself**, so callers never hand-route raw data. Because a
push conceptually hands the data to the managing stock layer, the surface should make post-publish
mutation fail fast — a recording-style handoff — since the language cannot express the ownership move
directly.

Once pushed, the data is accessed through the **same** handle and view mechanism as any other graph
resource; a pass never knows whether a backing originated in Vulkan, ECS, CPU memory, or a mod
provider.

**Externally-managed state is not a resource.** The swapchain is the canonical case: its backing is
owned and recreated by stock window/swapchain infrastructure, not the graph. It is set through a
dedicated handler surface — not the push path — and exposed to passes **indirectly**, through whatever
resource resolves its backing to the current acquired image.

## Graph Compilation And Execution

The stock render graph compiles setup declarations into an execution plan. The runtime graph
builder should combine explicit generated/manual contracts, not inspect arbitrary pass logic.

The stock compilation path is expected to handle:

- Pass registration and inclusion.
- Graph-owned resource manager instance creation and inclusion.
- Resource/view declaration collection.
- View-to-backing-resource resolution.
- Data-flow ordering.
- Explicit ordering escape hatches.
- Binding creation for frame execution.
- Lifecycle hook dispatcher scheduling.
- Validation and debug metadata.

### Data-Flow Ordering

Data-flow ordering is the primary stock pass ordering mechanism. Passes declare the views they
need in setup; stock resource/view handlers and generated contracts turn those declarations into
the graph facts needed by the stock builder.

The stock graph may also expose injectable setup-time graph-builder hooks, similar in spirit to
Sparkitect's existing entrypoint ordering integration. Pass contracts, resource views, or related
stock contracts may contribute or manipulate edges during graph setup/compilation.

For the initial semantics:

- Optional edges whose target or source node is missing are ignored.
- Non-optional edges with a missing source or target are errors.
- Cycles are hard errors.

Access concepts such as read, write, produce, consume, update, staging, or present are not
foundation-global semantics. They are stock or extension-layer contracts. The stock engine layer
may define common access models for Vulkan buffers/images, CPU staging data, swapchain targets,
and entity-derived render data.

Explicit ordering remains a supplementary escape hatch for cases where data-flow declarations do
not express the intended order.

### Render Graph Lifecycle Hooks

The render graph uses lifecycle hooks, but not as foundation resource lifecycle hooks. The
foundation does not define resources, resource views, Vulkan synchronization, descriptors,
allocation, or resource lifecycle semantics. Render graph hooks are emergent graph-level
extension points discovered from pass types and resource view types.

A hook-shaped interface can be implemented by:

- The pass type itself.
- A resource view type used by an annotated pass resource slot.

The source generator discovers hook-shaped interfaces generically. It must not hardcode every
stock or mod-defined hook interface. For each discovered hook on a pass, the generator emits one
strongly typed pass-local dispatcher. The graph runtime calls that generated dispatcher at the
matching lifecycle phase.

```text
graph lifecycle phase
  -> generated pass hook dispatcher
      -> optional pass-level hook implementation
      -> resource view hook for slot A
      -> resource view hook for slot B
      -> resource view hook for slot C
```

Every call inside the generated dispatcher is strongly typed. This resolves clashes between
user-authored pass hook code and resource-view hook code: both are hook sources, and the
generated dispatcher is the single wiring point that binds slots and composes the hook calls.

A resource view hook may perform immediate graph work. For example, a buffer view may implement a
pre-execution synchronization hook that issues GPU synchronization logic through the stock Vulkan
graph context before pass execution. The graph may also expose lifecycle phases during graph
building, compilation, frame binding, pre-execution, post-execution, resize handling, or cleanup.

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

Examples include:

- Copy CPU data into a mapped or staging buffer before a GPU pass.
- Transition an image before compute writes.
- Update descriptor sets before command recording.
- Bind descriptor sets or pipelines.
- Blit a storage image into a swapchain target after compute.
- Publish a resource version after a write.
- Mark a view as updated for runtime conditional execution.
- Acquire a swapchain image before the first pass that targets it.
- Present after the final pass writes the presentation target.

These hooks belong to stock engine resource/view contracts or extension-defined contracts. They
are not foundation resource lifecycle hooks. No custom hook ordering model is required for the
initial design; generated dispatchers use the chosen stock composition order for each lifecycle
phase.

### Runtime Manipulation

The stock graph should support two pass toggling modes:

- **Structural changes** add or remove passes, resource declarations, or graph-visible
  relationships. They trigger graph rebuild.
- **Runtime disable** skips a pass while preserving compiled topology when the pass remains part
  of the graph.

The stock graph may also support conditional execution:

- **Structural conditionality:** a pass or generated contract is not included because required
  stock resources/views cannot resolve.
- **Runtime conditionality:** a pass is skipped because a stock view/resource did not update this
  frame.

The meaning of "required" and "updated" is engine-layer semantics.

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
semaphores, acquire/present, descriptor update plumbing, common barriers, resize invalidation,
storage-image-to-swapchain blit, cleanup ordering, and frame resource duplication.

Pass-specific code should remain in passes: shader choice, resource declarations, push constant
values, dispatch dimensions, unusual custom synchronization, and the actual Vulkan work expressed
through pass code and resource/view lifecycle hooks.

The graph should not know about windows directly. Window binding enters the graph through a
swapchain target resource/view and its associated stock manager contracts.

### Emergent GPU Concerns

These concerns are stock/extension-layer concerns, not foundation concepts:

- **Multi-queue scheduling**: not needed initially, but the design must not prevent graphics,
  compute, and transfer queues later.
- **Memory aliasing**: reusing GPU memory for non-overlapping transient resources is future
  stock/extension behavior.
- **Frames-in-flight**: resources that need per-frame copies or synchronization state should
  model that through stock resource/view contracts and frame-resolved bindings.
- **Subresources**: mips, layers, buffer ranges, and other backing-resource slices are view-layer
  semantics.

## Registration And Integration

The render graph is standalone infrastructure, independent of GameState in the sense that it is
not a special GameState primitive. However, stock graph instances are expected to be created,
owned, and driven by GameState-driven infrastructure, analogous to how ECS worlds are created and
owned by the active game state.

Registration should follow standard Sparkitect patterns:

- Pass registration through a stock pass registry or generated registration path.
- Stock resource managers and resource/view handlers through normal registry, metadata, or
  DI-backed entrypoint patterns.
- Shader and Vulkan services through existing Vulkan module infrastructure where appropriate.
- Mod-defined views and handlers through the same public extension contracts as engine-defined
  views and handlers.

Graph composition may be driven by explicit graph-owner configuration, attribute/entrypoint-driven
registration metadata, or a combination of both. The exact stock composition rules remain open.
The foundation should not require per-resource-type registries; that is a stock engine design
choice if it proves useful.

## Validation And Debugging

Validation belongs mostly to the stock engine layer and compatible extensions.

Expected validation areas:

- Unbound or fetched-before-bound handles.
- Missing registered resources.
- Unsupported resource description/view combinations.
- Conflicting stock view semantics over overlapping physical resources.
- Missing generated/manual contracts.
- Swapchain-relative resources that need recreation after resize.
- Resource leaks, especially Vulkan/VMA objects.
- Dependency visualization and pass/resource graph inspection.
- Debug display of generated pass slots, view bindings, and lifecycle hook dispatchers.

The foundation should validate only protocol-level misuse. It should not attempt to validate
Vulkan layouts, descriptor compatibility, staging safety, or graph resource overlap without a
stock/extension contract that gives it those semantics.

The initial implementation does not need a dedicated diagnostics layer or an additional
deterministic ordering feature beyond what falls out of explicit constraints and the chosen graph
resolution algorithm. Cycle detection and required-edge validation remain mandatory.

## Initial Implementation Scope

The first implementation should prove the design with the smallest useful stock feature set:

- Pass registration and dependency-driven execution ordering.
- Opt-in source-generated pass/resource contract path alongside the manual contract path.
- Compute passes.
- Storage images.
- Storage buffers.
- Descriptor sets.
- Push constants.
- Compute dispatch.
- Swapchain acquisition, presentation, and sync object lifecycle in graph infrastructure.
- A single VMA allocator per Vulkan device exposed through the Vulkan module.
- Migration of the sample renderers from raw per-mod Vulkan orchestration to graph compute
  passes.

Rasterization, multi-queue scheduling, memory aliasing, and advanced frame-graph optimizations
are later stock or extension-layer concerns.

## Open Questions

- What is the minimal foundational pass/declaration/binding protocol?
- What generated/manual contracts does the stock render graph consume?
- What exactly is emitted by stock pass/resource source generators?
- How do stock pass abstractions encode hidden generic or marker metadata for generation?
- What does a stock resource slot declaration contain?
- How are lifecycle hook dispatchers represented in generated/manual contracts?
- How are stock resource descriptions validated without pushing semantics into foundation?
- Which stock view types ship first?
- How does the stock graph model staging buffer semantics?
- How does the stock graph model entity-derived render data provided by ECS systems?
- For a pushable resource, should the publish handoff seal the data object in place (further writes
  fail) or swap it for a fresh recording target? The fail-fast / recording-handoff intent is settled;
  the mechanism is not.
- How does the stock graph model composite resources such as voxel chunk buffers plus world BVH
  data?
- Which validation happens in analyzers, source generators, graph compilation, and frame
  execution?
- Should swapchain frame management (image acquisition, present-layout transition, present
  submission) be modeled as explicit *early* and *late* swapchain passes that participate in
  normal graph ordering, instead of being hardcoded into a manager's `BeginFrame`/`EndFrame`?
  An early pass would own acquisition and seed the swapchain view's bound backing; a late pass
  would own the transition to `PresentSrcKhr` and signal the present semaphore. This would
  collapse a category of "this only the manager can do" logic into the same pass + hook
  contracts every other graph participant uses, and make swapchain handling extensible by mods.

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
