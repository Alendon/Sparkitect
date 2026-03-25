using SpaceInvadersMod.Components;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS;
using Sparkitect.ECS.Queries;

namespace SpaceInvadersMod.Systems;

[ComponentQuery]
[ReadComponents<Position, Velocity, BulletData>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class BulletQuery;

[ComponentQuery]
[ReadComponents<Position, Velocity, ShootCooldown, EnemyTag>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class EnemyQuery;

[ComponentQuery]
[ReadComponents<Position, Velocity, ShootCooldown, PlayerTag>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class PlayerQuery;

// For PlayerInputSystem
[ComponentQuery]
[WriteComponents<Position, Velocity, ShootCooldown>]
[ReadComponents<PlayerTag>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class PlayerInputQuery;

// For EnemyAiSystem
[ComponentQuery]
[WriteComponents<Velocity, ShootCooldown>]
[ReadComponents<Position, EnemyTag>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class EnemyAiQuery;

// For BulletCleanupSystem
[ComponentQuery]
[ReadComponents<Position, Velocity, BulletData>]
[ExposeKey<EntityId>(true)]
[AllowConcreteResolution]
partial class BulletCleanupQuery;

// For MovementSystem (non-keyed)
[ComponentQuery]
[WriteComponents<Position>]
[ReadComponents<Velocity>]
[AllowConcreteResolution]
partial class MovementQuery;
