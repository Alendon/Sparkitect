---
uid: sparkitect.ecs.queries
title: Queries
description: Declaring component queries and iterating matched entities with source-generated handles
---

# Queries

A query declares which components a system reads, writes, and excludes. You write an empty `partial class` with attributes; the source generator emits the enumerator and a typed entity handle for it.

```csharp
[ComponentQuery]
[WriteComponents<Position>]
[ReadComponents<Velocity>]
partial class MovementQuery;
```

A query matches every storage whose components satisfy its access set, regardless of how that storage organizes them internally. It is not an archetype query — the storage's layout is invisible to it.

## Access Attributes

Four attributes describe a query's component access. All are stackable and come in arities 1 through 9; stack multiple attributes of the same kind to exceed nine.

| Attribute | Effect |
|-----------|--------|
| `[ComponentQuery]` | Marks the class for the query generator. Required. |
| `[ReadComponents<...>]` | Read-only access; generates value or `ref readonly` accessors. |
| `[WriteComponents<...>]` | Read-write access; generates `ref` accessors. |
| `[ExcludeComponents<...>]` | Rejects storages containing any listed component. No accessor generated. |

```csharp
[ComponentQuery]
[WriteComponents<Velocity, ShootCooldown>]
[ReadComponents<Position, EnemyTag>]
partial class EnemyAiQuery;
```

Read vs. write is a real access declaration, not a hint: the generator emits it into the concurrency metadata the scheduler uses to decide which systems can run in parallel (see <xref:sparkitect.ecs.systems>).

## The Entity Handle

The generator produces a typed handle per query. Each accessed component gets an accessor method named after the component type:

- A `WriteComponents` type yields `ref T GetPosition()` — mutate in place.
- A `ReadComponents` type yields `GetVelocity()` returning the value (or `ref readonly` for larger structs).

Iterate with `foreach`; the handle is the loop variable:

```csharp
foreach (var entity in query)
{
    ref var pos = ref entity.GetPosition();
    var vel = entity.GetVelocity();
    pos.Value += vel.Value * dt;
}
```

The enumerator walks entities chunk by chunk across every matching storage. Accessor names always match the component type name exactly, so `Position` becomes `GetPosition()` and `ShootCooldown` becomes `GetShootCooldown()`.

## Keyed Queries

By default a query exposes components only. Add `[ExposeKey<TKey>(required)]` to expose the entity key during iteration:

```csharp
[ComponentQuery]
[ReadComponents<Position, Velocity, BulletData>]
[ExposeKey<EntityId>(true)]
partial class BulletQuery;
```

The `required` flag controls matching:

- `true` — the query matches only storages that implement keyed iteration for `TKey`.
- `false` — key access is opportunistic: used where the storage provides it, skipped where it does not.

A keyed query's handle can act on its current entity — for example, routing it to a command buffer for destruction (see <xref:sparkitect.ecs.command-buffers>). A non-keyed query, like the `MovementQuery` above, omits the key entirely.

## Queries as System Parameters

A query is a dependency-injected parameter. Declare it on a system method and the framework constructs it, binds it to the world, and registers its capability filter:

```csharp
[EcsSystemFunction("movement")]
[EcsSystemScheduling]
private static void MovementSystem(MovementQuery query, FrameTimingHolder frameTiming)
{
    var dt = frameTiming.DeltaTime;
    foreach (var entity in query)
    {
        ref var pos = ref entity.GetPosition();
        pos.Value += entity.GetVelocity().Value * dt;
    }
}
```

At construction the query registers a component-set filter with the world; the world resolves matching storages and notifies the query as storage topology changes. On the hot path the query iterates its cached storages directly — no per-frame resolution.

> [!NOTE]
> A query whose concrete type the DI validator cannot confirm as injectable reports `SPARK0403`. Apply `[AllowConcreteResolution]` to the query class to opt into concrete resolution when you know the type is constructed by the framework.

## See Also

- <xref:sparkitect.ecs.components> for the component types a query accesses.
- <xref:sparkitect.ecs.systems> for wiring queries into systems and ordering their execution.
- <xref:sparkitect.ecs.command-buffers> for acting on a keyed query's current entity.
