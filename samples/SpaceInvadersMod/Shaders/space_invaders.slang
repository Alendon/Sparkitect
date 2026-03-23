// Space Invaders compute shader - renders entities from storage buffer to storage image

struct SpaceInvadersGameData
{
    uint entityCount;
    uint screenWidth;
    uint screenHeight;
    float padding;
    float3 backgroundColor;
};

struct RenderEntity
{
    float2 position;
    uint entityType;
};

[[vk::push_constant]]
ConstantBuffer<SpaceInvadersGameData, ScalarDataLayout> gameData;

[[vk::binding(0, 0)]]
RWTexture2D<float4> outputImage;

[[vk::binding(1, 0)]]
StructuredBuffer<RenderEntity> entities;

[shader("compute")]
[numthreads(8, 8, 1)]
void computeMain(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    uint2 pixelCoord = dispatchThreadID.xy;
    if (pixelCoord.x >= gameData.screenWidth || pixelCoord.y >= gameData.screenHeight)
        return;

    float2 uv = float2(pixelCoord) / float2(gameData.screenWidth, gameData.screenHeight);
    float3 color = gameData.backgroundColor;

    for (uint i = 0; i < gameData.entityCount; i++)
    {
        RenderEntity e = entities[i];
        float halfW, halfH;
        float3 entityColor;

        switch (e.entityType)
        {
            case 0: // Player - green
                halfW = 0.03;
                halfH = 0.015;
                entityColor = float3(0.2, 0.8, 0.2);
                break;
            case 1: // Enemy - red
                halfW = 0.025;
                halfH = 0.015;
                entityColor = float3(0.9, 0.3, 0.3);
                break;
            case 2: // Bullet - white
                halfW = 0.003;
                halfH = 0.01;
                entityColor = float3(1.0, 1.0, 1.0);
                break;
            default:
                continue;
        }

        if (abs(uv.x - e.position.x) < halfW && abs(uv.y - e.position.y) < halfH)
        {
            color = entityColor;
            break;
        }
    }

    outputImage[pixelCoord] = float4(color, 1.0);
}
