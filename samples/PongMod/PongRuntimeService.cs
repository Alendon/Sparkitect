using System.Diagnostics;
using System.Numerics;
using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Runtime;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Windowing;

namespace PongMod;

[StateService<IPongRuntimeService, PongModule>]
internal class PongRuntimeService : IPongRuntimeService
{
    private PongGameData _gameData;
    private Vector3 _backgroundColor = new(0.1f, 0.1f, 0.15f);
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    private ISparkitWindow? _window;
    private RenderGraph? _renderGraph;

    private IPushBinding? _leftPaddlePush;
    private IPushBinding? _rightPaddlePush;
    private float _leftIntent;
    private float _rightIntent;

    public required IWindowManager WindowManager { private get; init; }
    public required IRenderGraphManager RenderGraphManager { private get; init; }
    public required IInputActions InputActions { private get; init; }

    public ref PongGameData GameData => ref _gameData;
    public float DeltaTime { get; private set; }

    public ISparkitWindow Window => _window!;
    public bool IsOpen => _window?.IsOpen ?? false;

    public Vector3 BackgroundColor
    {
        get => _backgroundColor;
        set => _backgroundColor = value;
    }

    public void Initialize()
    {
        _gameData = PongGameData.CreateDefault();
        _frameTimer.Start();
        _lastFrameTime = 0;

        _window = WindowManager.CreateGameWindow("Pong");

        Log.Debug("Pong runtime initialized");
    }

    public void CreateGraph()
    {
        if (_renderGraph is not null) return;
        if (_window is null)
            throw new InvalidOperationException("Initialize must run before CreateGraph so the window exists.");

        _renderGraph = RenderGraphManager.CreateGraph<RenderGraph>(
            new List<Identification>
            {
                RenderPassID.PongMod.PongCompute,
                RenderPassID.PongMod.PongCopy,
            },
            _window);
    }

    public void WireInput()
    {
        _leftPaddlePush = ActionID.PongMod.LeftPaddle.Push(InputActions, v => _leftIntent = v);
        _rightPaddlePush = ActionID.PongMod.RightPaddle.Push(InputActions, v => _rightIntent = v);
    }

    public void RunFrame() => _renderGraph?.RunFrame();

    public void ShutdownGraph()
    {
        _renderGraph?.Dispose();
        _renderGraph = null;
    }

    public void Tick()
    {
        var currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        DeltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        UpdateSimulation();
    }

    public void Cleanup()
    {
        _leftPaddlePush?.Dispose();
        _leftPaddlePush = null;
        _rightPaddlePush?.Dispose();
        _rightPaddlePush = null;


        if (_window is not null)
            WindowManager.DestroyWindow(_window);
        _window = null;

        Log.Debug("Pong runtime cleanup complete");
    }

    private void UpdateSimulation()
    {
        const float paddleSpeed = 0.8f;
        MoveLeftPaddle(_leftIntent * paddleSpeed * DeltaTime);
        _leftIntent = 0f;
        MoveRightPaddle(_rightIntent * paddleSpeed * DeltaTime);
        _rightIntent = 0f;

        var deltaTime = DeltaTime;
        _gameData.BallPosition += _gameData.BallVelocity * deltaTime;

        if (_gameData.BallPosition.Y - _gameData.BallRadius <= 0 ||
            _gameData.BallPosition.Y + _gameData.BallRadius >= 1)
        {
            _gameData.BallVelocity.Y = -_gameData.BallVelocity.Y;
            _gameData.BallPosition.Y = Math.Clamp(_gameData.BallPosition.Y,
                _gameData.BallRadius, 1 - _gameData.BallRadius);
        }

        if (_gameData.BallPosition.X - _gameData.BallRadius <= _gameData.PaddleWidth)
        {
            if (Math.Abs(_gameData.BallPosition.Y - _gameData.LeftPaddleY) < _gameData.PaddleHeight / 2)
            {
                float speed = _gameData.BallVelocity.Length();
                float offset = (_gameData.BallPosition.Y - _gameData.LeftPaddleY) / (_gameData.PaddleHeight / 2);
                float maxAngle = 0.6f;
                _gameData.BallVelocity = new Vector2(1, offset * maxAngle);
                _gameData.BallVelocity = Vector2.Normalize(_gameData.BallVelocity) * speed;
                _gameData.BallPosition.X = _gameData.PaddleWidth + _gameData.BallRadius;
            }
        }

        if (_gameData.BallPosition.X + _gameData.BallRadius >= 1 - _gameData.PaddleWidth)
        {
            if (Math.Abs(_gameData.BallPosition.Y - _gameData.RightPaddleY) < _gameData.PaddleHeight / 2)
            {
                float speed = _gameData.BallVelocity.Length();
                float offset = (_gameData.BallPosition.Y - _gameData.RightPaddleY) / (_gameData.PaddleHeight / 2);
                float maxAngle = 0.6f;
                _gameData.BallVelocity = new Vector2(-1, offset * maxAngle);
                _gameData.BallVelocity = Vector2.Normalize(_gameData.BallVelocity) * speed;
                _gameData.BallPosition.X = 1 - _gameData.PaddleWidth - _gameData.BallRadius;
            }
        }

        if (_gameData.BallPosition.X < 0)
        {
            _gameData.RightScore++;
            Log.Information("Right scores! {Left} - {Right}", _gameData.LeftScore, _gameData.RightScore);
            ResetBall();
        }
        else if (_gameData.BallPosition.X > 1)
        {
            _gameData.LeftScore++;
            Log.Information("Left scores! {Left} - {Right}", _gameData.LeftScore, _gameData.RightScore);
            ResetBall();
        }
    }

    public void MoveLeftPaddle(float delta)
    {
        _gameData.LeftPaddleY = Math.Clamp(_gameData.LeftPaddleY + delta,
            _gameData.PaddleHeight / 2, 1 - _gameData.PaddleHeight / 2);
    }

    public void MoveRightPaddle(float delta)
    {
        _gameData.RightPaddleY = Math.Clamp(_gameData.RightPaddleY + delta,
            _gameData.PaddleHeight / 2, 1 - _gameData.PaddleHeight / 2);
    }

    public void ResetBall()
    {
        _gameData.BallPosition = new Vector2(0.5f, 0.5f);
        var angle = (Random.Shared.NextSingle() - 0.5f) * 0.5f;
        var direction = Random.Shared.Next(2) == 0 ? 1 : -1;
        _gameData.BallVelocity = new Vector2(0.4f * direction, 0.3f * angle);
    }
}