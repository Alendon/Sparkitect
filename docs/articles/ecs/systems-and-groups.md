---
uid: sparkitect.ecs.systems
title: Systems and Groups
description: Writing ECS systems, grouping them, ordering execution, and building and ticking the system tree
---

# Systems and Groups

A system is a static method that runs each frame over the entities its query matches. Systems live on group classes; groups form a tree that defines execution structure and scope.

```csharp
public partial class GameplayGroup
{
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
}
```

`[EcsSystemFunction("movement")]` registers the method as a system with that item-level identifier; `[EcsSystemScheduling]` opts it into the ECS scheduler. Systems are ECS-category stateless functions — everything they touch arrives through parameters. See [Stateless Functions](xref:sparkitect.core.stateless-functions) for the shared execution model.

## System Parameters

A system's parameters are dependency-injected each frame:

| Parameter | Provides |
|-----------|----------|
| A query type | Iteration over matched entities (see <xref:sparkitect.ecs.queries>). |
| [`FrameTimingHolder`](xref:Sparkitect.ECS.Systems.FrameTimingHolder) | `DeltaTime` and `TotalTime` for the current frame. |
| [`ICommandBufferAccessor`](xref:Sparkitect.ECS.Commands.ICommandBufferAccessor) | Deferred structural changes (see <xref:sparkitect.ecs.command-buffers>). |
| Any mod service | State services and managers resolved from the container. |

```csharp
[EcsSystemFunction("player_input")]
[EcsSystemScheduling]
[OrderBefore<GameplayGroup>]
private static void PlayerInputSystem(
    PlayerInputQuery query,
    ICommandBufferAccessor commandBufferAccessor,
    FrameTimingHolder frameTiming,
    ISpaceInvadersRuntimeService runtimeService)
{
    // ...
}
```

## Groups

A group is a `partial class` registered with `[SystemGroupRegistry.RegisterSystemGroup("key")]`. It lists `IHasIdentification` in its base list and opts into scheduling with `[SystemGroupScheduling]`:

```csharp
[SystemGroupRegistry.RegisterSystemGroup("gameplay")]
[SystemGroupScheduling]
[ParentId<SpaceInvadersSystemGroup>]
public partial class GameplayGroup : IHasIdentification
{
}
```

`[ParentId<TGroup>]` nests one group under another. A root group omits it. Systems attach to a group by being declared as methods on its `partial class` — the `GameplayGroup` above and the `MovementSystem` method share the same class. Group registration generates a typed ID reachable through `EcsSystemGroupID.{Mod}.{Group}`.

## Ordering

Order systems and groups with `[OrderBefore<T>]` and `[OrderAfter<T>]`. The type argument names another system or group; systems reference a sibling's generated function type:

```csharp
[EcsSystemFunction("movement")]
[EcsSystemScheduling]
[OrderAfter<GameplayGroup.EnemyAiFunc>]
private static void MovementSystem(/* ... */) { }
```

Ordering constrains the scheduler only where it matters. Systems with no ordering relationship and no conflicting component access can run in parallel — the scheduler derives that from the read/write sets declared on their queries. Add an ordering attribute only when execution order actually affects the result. These are the same attributes documented under [Stateless Functions](xref:sparkitect.core.stateless-functions).

## Building and Ticking

Build a tree from a root group, hand it to the world, then execute it each frame:

```csharp
world.SetSystemTree(systemManager.BuildTree(EcsSystemGroupID.SpaceInvadersMod.SpaceInvaders));
systemManager.NotifyRebuild(world);

// Each frame:
systemManager.ExecuteSystems(world, new FrameTiming(deltaTime, totalTime));
```

[`BuildTree`](xref:Sparkitect.ECS.Systems.ISystemManager) constructs a [`SystemTreeNode`](xref:Sparkitect.ECS.Systems.SystemTreeNode) tree rooted at the given group; systems not reachable from the root are excluded. `SetSystemTree` installs it on the world. `NotifyRebuild` tells the scheduler that storage topology or the tree changed, so it recomputes the execution plan. `ExecuteSystems` runs one tick with the supplied [`FrameTiming`](xref:Sparkitect.ECS.Systems.FrameTiming).

## Toggling at Runtime

Activate or deactivate any node — a single system or a whole subtree — by identification:

```csharp
world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Inactive);
// ... later
world.SetNodeState(EcsSystemGroupID.SpaceInvadersMod.Gameplay, SystemState.Active);
```

[`SetNodeState`](xref:Sparkitect.ECS.IWorld) toggles a node between [`SystemState.Active`](xref:Sparkitect.ECS.Systems.SystemState) and `Inactive`. It does not rebuild the tree — node state is evaluated at execution time, so a pause is a single flag flip rather than a structural change.

## See Also

- <xref:sparkitect.ecs.queries> for the query parameters systems iterate.
- <xref:sparkitect.ecs.command-buffers> for structural changes issued from a system.
- [Stateless Functions](xref:sparkitect.core.stateless-functions) for the ordering attributes and execution model systems share.
