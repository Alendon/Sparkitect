---
uid: sparkitect.ecs.internals-capabilities
title: "Internals: Capabilities"
description: How capabilities identify storage features and how the world resolves query filters against them
---

# Capability Resolution

*Engine internals. Mod authors can work entirely from the usage pages; this page describes how storage features are discovered.*

A capability is a closed generic CLR interface used as a storage's identity for one feature: `IChunkedIteration`, `IComponentAccess<int>`, and `IEntityIdentity<int>` are distinct capabilities that a storage advertises by implementing them. The core defines the triad [`ICapability`](xref:Sparkitect.ECS.Capabilities.ICapability) (the non-generic marker), [`ICapabilityMetadata`](xref:Sparkitect.ECS.Capabilities.ICapabilityMetadata) (a per-storage descriptor supplied at registration time), and [`ICapabilityRequirement`](xref:Sparkitect.ECS.Capabilities.ICapabilityRequirement) (a typed `Matches` predicate over that metadata, with a non-generic base so filters can hold heterogeneous requirements).

Discovery runs through [`IWorld`](xref:Sparkitect.ECS.IWorld). A query registers a filter — a list of requirements — with `RegisterFilter`; the world caches the matching storage set and re-fires the query's callback whenever storage topology changes, so on the hot path the query reads its cached capabilities directly rather than the world. `Resolve` is the pull equivalent for ad-hoc, one-shot lookups. Matching intersects requirements: a storage matches only when every requirement returns true against the corresponding capability's metadata.

## See Also

- <xref:sparkitect.ecs.internals-storage> for how the reference storage advertises its capabilities.
- <xref:sparkitect.ecs.queries> for the mod-author view of declaring and iterating queries.
- <xref:sparkitect.ecs.ecs-design> for why identity and iteration are emergent, not core.
