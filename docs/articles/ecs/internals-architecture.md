---
uid: sparkitect.ecs.internals-architecture
title: "Internals: Architecture"
description: The three-layer structure of the ECS and how it maps to the shipped namespaces
---

# ECS Architecture

*Engine internals. Mod authors can work entirely from the usage pages; this page describes how the ECS is built.*

The ECS is built in three layers: a zero-policy framework core that defines only coordination and extension points, emergent implementations that supply all concrete behavior, and an engine-infrastructure extension that schedules systems as a Stateless Function category. The core is deliberately smaller than a typical ECS — entity identity, iteration, storage layout, and command buffers are all emergent, so a new component type, storage backend, or identity scheme plugs in without a core change.

The shipped namespaces map onto those layers. `Sparkitect.ECS` (the [`IWorld`](xref:Sparkitect.ECS.IWorld) coordinator and `EntityId`) together with the `Sparkitect.ECS.Capabilities` and `Sparkitect.ECS.Storage` contracts form the core; `SoAStorage`, `Sparkitect.ECS.Queries`, and `Sparkitect.ECS.Commands` are the shipped emergent implementations; `Sparkitect.ECS.Systems` extends the engine's Stateless Function scheduling. Forward-looking work is a broader query/system source-generator family and parallel system dispatch — the shipped surface stops at the sequential executor.

## See Also

- <xref:sparkitect.ecs.internals-capabilities> for the capability contract the core is built around.
- <xref:sparkitect.ecs.internals-scheduling> for the Stateless Function scheduling extension.
- <xref:sparkitect.ecs.ecs-design> for the design rationale and forward-looking model.
