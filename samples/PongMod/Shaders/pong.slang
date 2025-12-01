// Pong compute shader - renders game state to storage image
// The source file  gets compiled before packed into the mod archive - For now the compilation happens manually

struct PongGameData
{
    float leftPaddleY;
    float rightPaddleY;
    float paddleWidth;
    float paddleHeight;
    float2 ballPosition;
    float2 ballVelocity;
    float ballRadius;
    int leftScore;
    int rightScore;
    uint screenWidth;
    uint screenHeight;
};

[[vk::push_constant]]
ConstantBuffer<PongGameData> gameData;

[[vk::binding(0, 0)]]
RWTexture2D<float4> outputImage;

[shader("compute")]
[numthreads(8, 8, 1)]
void computeMain(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    uint2 pixelCoord = dispatchThreadID.xy;

    if (pixelCoord.x >= gameData.screenWidth || pixelCoord.y >= gameData.screenHeight)
        return;

    // Normalized coordinates [0,1]
    float2 uv = float2(pixelCoord) / float2(gameData.screenWidth, gameData.screenHeight);

    // Background color (dark)
    float3 color = float3(0.1, 0.1, 0.15);

    // Center line (dashed)
    if (abs(uv.x - 0.5) < 0.002)
    {
        float dashY = frac(uv.y * 20.0);
        if (dashY < 0.5)
            color = float3(0.3, 0.3, 0.3);
    }

    // Left paddle
    float leftPaddleX = gameData.paddleWidth * 0.5;
    if (abs(uv.x - leftPaddleX) < gameData.paddleWidth * 0.5 &&
        abs(uv.y - gameData.leftPaddleY) < gameData.paddleHeight * 0.5)
    {
        color = float3(0.9, 0.9, 0.9);
    }

    // Right paddle
    float rightPaddleX = 1.0 - gameData.paddleWidth * 0.5;
    if (abs(uv.x - rightPaddleX) < gameData.paddleWidth * 0.5 &&
        abs(uv.y - gameData.rightPaddleY) < gameData.paddleHeight * 0.5)
    {
        color = float3(0.9, 0.9, 0.9);
    }

    // Ball
    float2 ballDiff = uv - gameData.ballPosition;
    // Aspect ratio correction for ball
    ballDiff.x *= float(gameData.screenWidth) / float(gameData.screenHeight);
    float ballDist = length(ballDiff);
    float correctedRadius = gameData.ballRadius * float(gameData.screenWidth) / float(gameData.screenHeight);
    if (ballDist < correctedRadius)
    {
        color = float3(1.0, 1.0, 1.0);
    }

    outputImage[pixelCoord] = float4(color, 1.0);
}
