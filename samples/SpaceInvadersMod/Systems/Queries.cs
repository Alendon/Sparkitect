using SpaceInvadersMod.Components;
using Sparkitect.ECS;
using Sparkitect.ECS.Queries;

namespace SpaceInvadersMod;

[ComponentQuery]
[ReadComponents<Position, Velocity, BulletData>]
[ExposeKey<EntityId>(true)]
partial class BulletQuery;

[ComponentQuery]
[ReadComponents<Position, Velocity, ShootCooldown, EnemyTag>]
[ExposeKey<EntityId>(true)]
partial class EnemyQuery;

[ComponentQuery]
[ReadComponents<Position, Velocity, ShootCooldown, PlayerTag>]
[ExposeKey<EntityId>(true)]
partial class PlayerQuery;

// For PlayerInputSystem
[ComponentQuery]
[WriteComponents<Position, Velocity, ShootCooldown>]
[ReadComponents<PlayerTag>]
[ExposeKey<EntityId>(true)]
partial class PlayerInputQuery;

// For EnemyAiSystem
[ComponentQuery]
[WriteComponents<Velocity, ShootCooldown>]
[ReadComponents<Position, EnemyTag>]
[ExposeKey<EntityId>(true)]
partial class EnemyAiQuery;

// For BulletCleanupSystem
[ComponentQuery]
[ReadComponents<Position, Velocity, BulletData>]
[ExposeKey<EntityId>(true)]
partial class BulletCleanupQuery;

// For MovementSystem (non-keyed)
[ComponentQuery]
[WriteComponents<Position>]
[ReadComponents<Velocity>]
partial class MovementQuery;
