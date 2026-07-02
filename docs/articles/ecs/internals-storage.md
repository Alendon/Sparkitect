---
uid: sparkitect.ecs.internals-storage
title: "Internals: Storage"
description: The reference SoA storage — columnar layout, swap-and-pop, single-chunk iteration, and entity identity mapping
---

# Storage Internals

*Engine internals. Mod authors can work entirely from the usage pages; this page describes the reference storage implementation.*

[`SoAStorage`](xref:Sparkitect.ECS.Storage.SoAStorage) is the reference [`IStorage`](xref:Sparkitect.ECS.Storage.IStorage) (keyed by `int`): one [`NativeColumn`](xref:Sparkitect.ECS.Storage.NativeColumn) per component gives a struct-of-arrays layout, dense slot keys, and swap-and-pop removal that moves the last entity into a vacated slot. Iteration is single-chunk — the storage hands a query its whole entity range in one span. Stable cross-storage references come from an internal `EntityIdentityMap<int>` that maps [`EntityId`](xref:Sparkitect.ECS.EntityId) to slot key and stays consistent across swaps via `NotifySwap`.

The world never sees these internals. It interacts with a storage only through the capabilities the storage advertises, so storage is a black box behind the capability contract. The shipped convention is one storage per archetype — a 1:1 component-set shape — but the contract does not enforce it; a sparse-set or mixed-shape backend would satisfy the same interface without a core change.

## See Also

- <xref:sparkitect.ecs.internals-capabilities> for the capability contract storage is discovered through.
- <xref:sparkitect.ecs.worlds-storage> for the mod-author view of building storage and spawning entities.
- <xref:sparkitect.ecs.ecs-design> for why storage internals are left unconstrained.
