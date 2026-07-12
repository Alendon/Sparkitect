---
uid: sparkitect.rendergraph.composites
title: Composite Resources
description: Resources built from CPU data plus references to other resources, declaration products, and cleanup strategies
---

# Composite Resources

A composite resource is CPU data plus references to resources that already exist — it has no backing provider and no manager of its own. A leaf resource owns a piece of GPU backing (a buffer, an image); a composite threads leaves and other composites together and may carry CPU metadata alongside them.

The `EntityListResource` (from `SpaceInvadersMod`) is a composite: a device buffer reference plus an element count.

```csharp
public sealed class EntityListResource(BufferResource buffer)
{
    public BufferResource Buffer { get; } = buffer;
    public int Count { get; private set; }  // materialized at the producing pass's Execute
}
```

## Declaration Products

A composite description often needs to hand a reference it minted to a sibling declaration. It exposes that reference as a **declaration product**: a property assigned during `Declare` and read by the pass afterward.

[`StagingDescription`](xref:Sparkitect.Graphics.RenderGraph.Resources.StagingDescription) sub-declares a host leaf and a device leaf, increments the device leaf (the host-to-device staging copy), and exposes the post-increment reference as `PopulatedBuffer`:

```csharp
public sealed record StagingDescription : IResourceDescription<StagingBuffer>
{
    public ResourceRef<BufferResource> PopulatedBuffer { get; private set; }

    public DeclaredFact<StagingBuffer> Declare(IResourceTransaction tx)
    {
        var host = tx.Declare(new HostBufferDescription());
        var device = tx.Declare(new DeviceBufferDescription());
        var staged = tx.Increment(device);

        PopulatedBuffer = staged;          // declaration product, assigned during Declare

        var fact = tx.InstantiateFact<StagingFacts>();
        return fact with { Host = host, Device = device, Staged = staged };
    }
}
```

The declaring pass reads `PopulatedBuffer` after `Use` returns, to wire the next declaration:

```csharp
var staging = new StagingDescription();
_staging = ctx.Use(staging);
_entities = ctx.Use(new EntityListResourceDescription(staging.PopulatedBuffer, GraphMomentID.SpaceInvadersMod.EntitiesGpu));
```

Assign-before-read is the rule: a declaration product is valid only after its description has been declared. A description instance is declared at most once — declaring it twice is a compile error (`DescriptionReuse`).

## Two Authoring Styles

The staging composite shows the two ways to build a composite:

- **Compose general parts in the pass.** Declare a general staging buffer, then hand its `PopulatedBuffer` to a separate published-composite description. The parts are reusable across passes.
- **Encapsulate in one specialized description.** A single description sub-declares its leaves and publishes the composite itself, exposing one handle to the pass.

Prefer the general parts when the leaves are reused; prefer encapsulation when the composition is specific to one pass.

## Published Composites Carry CPU Metadata

A published GPU-side composite can carry CPU metadata that rides inside the resource instance. The entity list carries its element count that way — the producing pass seals it at `Execute`, and every reader reads it off the same instance:

```csharp
// Producing pass, in Execute:
_entities.Fetch().SetCount(count);
```

The count rides inside the resource; it is never re-derived through DI. See [dependency boundaries](xref:sparkitect.rendergraph.pass-authoring) for why.

## Stock View Compositions

"View" is a usage label, not a `VkImageView` — a view is a resource composed for one usage. The stock set:

| Composition | Backs | Cardinality |
|-------------|-------|-------------|
| Render-target view | An image written by a pass | 1:1 |
| Staging buffer | Host + device buffer pair | composite |
| Transfer-src read view | An image read for a blit | 1:1 |
| Storage / descriptor view | A buffer or image bound to a shader | 1:1 |
| Entity list | A device buffer + count | composite |
| Swapchain write view | The engine present target | N:1 |

1:1, 1:N, N:1, and composite mappings are all valid.

## Cleanup Strategies

Each resource declares one [`CleanupStrategy`](xref:Sparkitect.Graphing.Descriptions.CleanupStrategy) on its fact:

| Strategy | Meaning |
|----------|---------|
| `None` | Nothing to release (the leaves own their own release) |
| `Dispose` | The instance directly disposes an owned object |
| `Release` | Manager-backed: the instance signals release, its backing provider decides how |

`Release` is the substrate future backing-aliasing builds on. How a release is honored is the backing provider's decision, never the graph's.

## See Also

- <xref:sparkitect.rendergraph.descriptions-and-moments> for the `Declare`/facts model composites build on
- <xref:sparkitect.rendergraph.requirements> for the resource-model rationale
