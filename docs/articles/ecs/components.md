---
uid: sparkitect.ecs.components
title: Components
description: Declaring unmanaged component structs and registering them with the component registry
---

# Components

A component is an unmanaged struct carrying an [`Identification`](xref:Sparkitect.Modding.Identification), registered through the component registry. Storages hold components in per-type columns, so every component must be blittable — no reference fields, no managed types.

## Declaring a Component

Apply `[UnmanagedComponentRegistry.RegisterComponent("key")]` to a `partial struct` and list `IHasIdentification` in its base list:

```csharp
[UnmanagedComponentRegistry.RegisterComponent("position")]
public partial struct Position : IHasIdentification
{
    public Vector2 Value;
}
```

The `"position"` key is the component's item-level identifier. The mod and category parts of its full `mod:category:item` identity are filled in from the declaring mod and the `unmanaged_component` registry category.

You write the `: IHasIdentification` declaration yourself. The Registry Generator emits the `static Identification Identification` implementation from the attribute — you never hand-author that property, and the explicit base list is what lets sibling generators discover the component. See [Registry System](xref:sparkitect.core.registry-system) for the shared registration mechanism.

## Tag Components

A component with no fields is a tag — presence alone is the data:

```csharp
[UnmanagedComponentRegistry.RegisterComponent("enemy_tag")]
public partial struct EnemyTag : IHasIdentification
{
}
```

Tags participate in queries like any other component. A query that reads `EnemyTag` matches only storages whose archetype includes it, so tags are how you partition entities that share the same data columns.

## Typed IDs

Registration generates a type-safe ID for every component, reachable through `UnmanagedComponentID.{Mod}.{Name}`:

```csharp
using SpaceInvadersMod.CompilerGenerated.IdExtensions;

Identification posId = UnmanagedComponentID.SpaceInvadersMod.Position;
```

The name is the PascalCase form of the registered key. Import the `CompilerGenerated.IdExtensions` namespace from the mod that declared the component to bring its IDs into scope. You need these IDs when building storage from component metadata (see <xref:sparkitect.ecs.worlds-storage>) and when writing capability filters (see <xref:sparkitect.ecs.command-buffers>).

## The Unmanaged Constraint

`RegisterComponent<T>` constrains `T` to `unmanaged, IHasIdentification`. This is a storage requirement, not a stylistic one: the reference SoA storage lays each component type out in a contiguous native column and iterates it as a `Span<T>`. Managed fields cannot live in native memory, so the compiler rejects them at the registration call.

Keep components small and data-only. Behavior belongs in systems (<xref:sparkitect.ecs.systems>), not on the component.

## See Also

- <xref:sparkitect.ecs.worlds-storage> for building storage from registered components.
- <xref:sparkitect.ecs.queries> for reading and writing components during iteration.
- [Registry System](xref:sparkitect.core.registry-system) for the registration and ID framework components build on.
