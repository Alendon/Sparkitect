---
uid: sparkitect.ecs.worlds-storage
title: Worlds and Storage
description: Creating a world, adding archetype storage, and spawning entities imperatively
---

# Worlds and Storage

A [`World`](xref:Sparkitect.ECS.IWorld) is a standalone coordinator for storages, entity identity, and system execution. It is independent of GameState — any code can create and destroy one, and multiple worlds can exist at once.

```csharp
IWorld world = IWorld.Create();
```

The world owns entity identity and mediates capability discovery. It does not own component data — that lives in the storages you add to it.

## Adding Storage

A storage holds the entities of one archetype (one fixed component set). The reference implementation is [`SoAStorage`](xref:Sparkitect.ECS.Storage.SoAStorage), which keeps each component type in its own native column.

Build a storage from component metadata — the ID and byte size of each column — then register it with the world:

```csharp
(Identification, int) Meta(Identification id) => (id, componentManager.GetSize(id));

var playerStorage = new SoAStorage(
    [Meta(posId), Meta(velId), Meta(cooldownId), Meta(playerTagId)],
    tracker, world, initialCapacity: 4);

StorageHandle playerHandle = world.AddStorage(
    playerStorage, playerStorage.CreateCapabilityRegistrations());
playerStorage.SetHandle(playerHandle);
```

`AddStorage` returns a [`StorageHandle`](xref:Sparkitect.ECS.StorageHandle) — an opaque generational reference. `CreateCapabilityRegistrations()` declares what the storage provides (component iteration, keyed iteration, identity) so queries can match against it. Call `SetHandle` afterward so the storage can bind its entities back to the world.

The stock convention is one storage per archetype. Nothing enforces 1:1 — the framework sees a storage only through its capabilities, not its internal layout — but the reference `SoAStorage` maps a single component set to a single instance.

> [!NOTE]
> There is no archetype-declaration source generator yet. You assemble the component-metadata list by hand, as shown above. A future SG family is planned to generate archetype creation and typed spawn helpers.

## Spawning Entities

Entity creation is imperative and goes through capability interfaces on the storage accessor. Allocate a slot, assign an entity ID, then set component values:

```csharp
var accessor = world.GetStorage(playerHandle);
var slot = accessor.AsStorage<int>()!.AllocateEntity();

accessor.As<IEntityIdentity<int>>()!.Assign(world.AllocateEntityId(), slot);

var components = accessor.As<IComponentAccess<int>>()!;
components.Set(slot, new Position { Value = new Vector2(0.5f, 0.9f) });
components.Set(slot, new Velocity { Value = Vector2.Zero });
components.Set(slot, new PlayerTag());
```

[`GetStorage`](xref:Sparkitect.ECS.IWorld) returns a stack-only [`StorageAccessor`](xref:Sparkitect.ECS.StorageAccessor). Its `AsStorage<TKey>()` and `As<TCapability>()` methods cast to the storage's typed slot allocator, identity capability, and component-access capability. The `int` key is the storage's internal slot index.

Inside a system, prefer command buffers for spawning and destroying entities — they defer the structural change to a safe point. Direct allocation as shown here is for world setup. See <xref:sparkitect.ecs.command-buffers>.

## Entity Identity and Lifecycle

[`AllocateEntityId`](xref:Sparkitect.ECS.IWorld) hands out an [`EntityId`](xref:Sparkitect.ECS.EntityId): an index paired with a generation counter. The generation is what makes a reference safe across reclaim — when a slot is reused, stale IDs fail validation.

An entity moves through three [`EntityState`](xref:Sparkitect.ECS.EntityState) values:

| State | Meaning |
|-------|---------|
| `Null` | The slot was never allocated, or has been reclaimed. |
| `Empty` | An ID is allocated but not yet bound to a storage. |
| `Bound` | The entity is bound to a storage and fully alive. |

`Assign` binds the ID to a storage slot. `IsValid(id)` reports whether an entity is still alive. Reclaim has two forms:

- `ReclaimEntityId(id)` — hard reclaim, unconditional.
- `TryReclaimEntityId(id, storageHandle)` — soft reclaim, succeeds only when the entity's current binding matches the given handle. Use this to avoid reclaiming an entity a system no longer owns.

## See Also

- <xref:sparkitect.ecs.components> for declaring the component types a storage holds.
- <xref:sparkitect.ecs.queries> for iterating entities across matching storages.
- <xref:sparkitect.ecs.command-buffers> for deferred entity creation and destruction inside systems.
