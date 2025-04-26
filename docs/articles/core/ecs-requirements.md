# ECS Requirements

List of different requirements for the Entity Component System (ECS) architecture.

## General Requirements

- **Performance**: The ECS should be designed for high performance, allowing for efficient processing of large numbers
  of entities and components.
- **Flexibility**: The ECS should be flexible enough to accommodate a wide range of game mechanics and systems, allowing
  for easy addition and removal of components and systems.
- **Scalability**: The ECS should be able to scale with the complexity of the game, allowing for efficient management of
  large numbers of entities and components.

## System Requirements

Systems must be executable in parallel. Based on minimal overhead job/task library.
Maybe partitioning systems themselves into smaller jobs/tasks.

## Component Queries

Component Queries are the core interface for Systems to access components/entities.
They can be implemented as multiple different types, for optimization purposes. This is not further specified here.

Features of Component Queries:

- Query all entities with specific components
- Provide selective read/write access to components
- Support fetching of specific entities by (volatile) ID

## Archetypes / Component Groups / Component Pools

Archetypes group entities by their component types.

Archetypes can be immutable or dynamic.

Immutable archetypes are created at mod loading phase / registry phase.
They can be further optimized for performance, but are not flexible.
Especially useful for entity types that exists in high numbers.

Dynamic archetypes are constructed at runtime, when entities are created.

Each Archetype has a corresponding Component Pool containing the components of the entities in that archetype.
For immutable archetypes, the component pool can be optimized for performance, e.g. by using runtime source generation.
Adding/Removing components from an entity creates a new archetype.

Add Entity Templates. A Template is basically a kind of archetype that is used to instantiate entities.
They can also be used for creating immutable archetype variants.

Open Questions:

- Instead of dynamic/immutable archetypes, we could only have dynamic archetypes. All specialization for static
  archetypes could be triggered upfront, when the archetype is created or when a threshold of minimum entities in an archetype is reached.



## Entities

Entities are just IDs.

There must be two types of Entity IDs. A stable and a "volatile" ID.
The stable ID is used 