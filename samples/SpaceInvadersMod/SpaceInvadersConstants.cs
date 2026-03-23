namespace SpaceInvadersMod;

public static class SpaceInvadersConstants
{
    // Movement speeds (units/sec in 0-1 normalized space)
    public const float PlayerSpeed = 0.6f;
    public const float BulletSpeed = 0.8f;

    // Shoot cooldowns (seconds)
    public const float PlayerShootCooldown = 0.3f;
    public const float EnemyShootCooldownMin = 1.0f;
    public const float EnemyShootCooldownMax = 3.0f;

    // Enemy oscillation (per D-05/D-07: deterministic sine wave)
    public const float EnemyOscillationSpeed = 1.5f;
    public const float EnemyOscillationAmplitude = 0.15f;

    // AABB half-extents (per D-12: fixed-size per entity type in 0-1 coords)
    public const float PlayerHalfW = 0.03f;
    public const float PlayerHalfH = 0.015f;
    public const float EnemyHalfW = 0.025f;
    public const float EnemyHalfH = 0.015f;
    public const float BulletHalfW = 0.003f;
    public const float BulletHalfH = 0.01f;

    // Render buffer
    public const int MaxRenderEntities = 256;

    // Entity type codes (per D-01)
    public const uint TypePlayer = 0;
    public const uint TypeEnemy = 1;
    public const uint TypeBullet = 2;
}
