---
uid: sparkitect.windowing.windowing-input
title: Windowing and Input
description: Window management, Vulkan surface integration, and keyboard/mouse input handling
---

# Windowing and Input

Sparkitect provides window management through `IWindowManager` and `ISparkitWindow`. Unlike many game engines, **input is accessed directly through the window object** rather than through a global input service.

> **Important**: There is no global `IInputService`. Keyboard and mouse state are accessed through `window.Keyboard` and `window.Mouse` properties.

## Window Management

### Creating Windows

Use `IWindowManager` to create windows. Access it through dependency injection:

```csharp
[StateService<IMyService, MyModule>]
public class MyService : IMyService
{
    public required IWindowManager WindowManager { private get; init; }

    private ISparkitWindow? _window;

    public void Initialize()
    {
        _window = WindowManager.CreateWindow("My Game", 1280, 720);
    }
}
```

### IWindowManager Interface

```csharp
public interface IWindowManager
{
    ISparkitWindow? MainWindow { get; set; }

    ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null);

    IReadOnlyList<string> GetRequiredVulkanExtensions();
}
```

The `MainWindow` property provides quick access to the primary game window.

## ISparkitWindow Interface

Each window provides access to its dimensions, state, input devices, and Vulkan resources:

```csharp
public interface ISparkitWindow : IDisposable
{
    // Window properties
    int Width { get; }
    int Height { get; }
    string Title { get; set; }
    bool IsOpen { get; }

    // Input devices
    IKeyboard Keyboard { get; }
    IMouseInput Mouse { get; }

    // Vulkan integration
    VkSurface Surface { get; }
    VkSwapchain Swapchain { get; }

    // Event handling
    void PollEvents();

    // Frame acquisition
    VkResult<uint> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false);

    Result Present(uint imageIndex, VkSemaphore waitSemaphore, Queue presentQueue);
}
```

## Input Access Pattern

**Input is accessed through the window, not through a global service.** This design:
- Supports multiple windows with independent input states
- Makes input ownership explicit
- Avoids hidden global state

### Keyboard Input

Access keyboard state through `window.Keyboard`:

```csharp
// In your per-frame function or render loop
var keyboard = _window.Keyboard;

// Check if a key is currently held down
if (keyboard.IsKeyDown(Key.Escape))
{
    GameStateManager.Shutdown();
}

// Movement example
if (keyboard.IsKeyDown(Key.W))
    MoveForward(speed * deltaTime);
if (keyboard.IsKeyDown(Key.S))
    MoveBackward(speed * deltaTime);
if (keyboard.IsKeyDown(Key.A))
    MoveLeft(speed * deltaTime);
if (keyboard.IsKeyDown(Key.D))
    MoveRight(speed * deltaTime);
```

The `IKeyboard` interface:

```csharp
public interface IKeyboard
{
    bool IsKeyDown(Key key);
}
```

Key codes are from `Silk.NET.Input.Key`.

### Mouse Input

Access mouse state through `window.Mouse`:

```csharp
var mouse = _window.Mouse;

// Get current mouse position in window coordinates
Vector2 position = mouse.GetPosition();

// Get movement since last frame
Vector2 delta = mouse.GetDelta();

// Check button state
if (mouse.IsButtonDown(MouseButton.Left))
{
    // Handle left click
}
```

The `IMouseInput` interface:

```csharp
public interface IMouseInput
{
    Vector2 GetPosition();
    Vector2 GetDelta();
    bool IsButtonDown(MouseButton button);
}
```

Mouse buttons are from `Silk.NET.Input.MouseButton`.

> **Note**: Methods are used instead of properties to signal snapshot semantics - values represent the state at the time of the call.

## Event Polling

Call `PollEvents()` once per frame to process window system events (input, resize, close):

```csharp
public void Render()
{
    _window.PollEvents();

    if (!_window.IsOpen)
    {
        GameStateManager.Shutdown();
        return;
    }

    // Continue with rendering...
}
```

**When to call PollEvents:**
- Once per frame at the start of your render loop
- Before checking input state
- Before checking `IsOpen`

## Vulkan Integration

Windows provide Vulkan surface and swapchain access:

### Surface and Swapchain

```csharp
// Access Vulkan surface for the window
VkSurface surface = _window.Surface;

// Access swapchain for frame presentation
VkSwapchain swapchain = _window.Swapchain;

// Get swapchain images
var images = swapchain.Images;
var extent = swapchain.Extent;
```

### Frame Acquisition and Presentation

```csharp
// Acquire next swapchain image
var acquireResult = _window.AcquireNextImage(_imageAvailableSemaphore, autoRecreate: true);
if (acquireResult is VkResult<uint>.Error)
    return; // Swapchain out of date, skip frame

var imageIndex = ((VkResult<uint>.Success)acquireResult).value;

// ... render to swapchain image ...

// Present the frame
swapchain.Present(imageIndex, _renderFinishedSemaphore, _graphicsQueue);
```

The `autoRecreate` parameter automatically handles swapchain recreation on window resize.

## Complete Example: Pong Input Handling

From the Pong sample:

```csharp
public void Render()
{
    if (_window is null) return;

    _window.PollEvents();
    if (!_window.IsOpen)
    {
        GameStateManager.Shutdown();
        return;
    }

    // ... rendering code ...
}

private void UpdateSimulation()
{
    if (_window != null)
    {
        var keyboard = _window.Keyboard;
        var paddleSpeed = 0.8f;

        // Left paddle: W/S
        if (keyboard.IsKeyDown(Key.W))
            MoveLeftPaddle(-paddleSpeed * DeltaTime);
        if (keyboard.IsKeyDown(Key.S))
            MoveLeftPaddle(paddleSpeed * DeltaTime);

        // Right paddle: Up/Down arrows
        if (keyboard.IsKeyDown(Key.Up))
            MoveRightPaddle(-paddleSpeed * DeltaTime);
        if (keyboard.IsKeyDown(Key.Down))
            MoveRightPaddle(paddleSpeed * DeltaTime);
    }

    // ... physics simulation ...
}
```

## Integration with Game State

Use input in state functions by storing the window reference:

```csharp
[StateService<IGameController, GameModule>]
public class GameController : IGameController
{
    public required IWindowManager WindowManager { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

    private ISparkitWindow? _window;

    public void Initialize()
    {
        _window = WindowManager.CreateWindow("My Game", 800, 600);
    }

    public void HandleInput()
    {
        if (_window == null) return;

        _window.PollEvents();

        if (!_window.IsOpen)
        {
            GameStateManager.Shutdown();
            return;
        }

        var keyboard = _window.Keyboard;

        if (keyboard.IsKeyDown(Key.Escape))
        {
            // Transition to pause menu
            GameStateManager.Request(StateID.MyMod.PauseMenu);
        }
    }

    public void Cleanup()
    {
        _window?.Dispose();
    }
}
```

## Window Lifecycle

Windows should be disposed when no longer needed:

```csharp
public void Cleanup()
{
    // Ensure Vulkan is done with the window's resources
    VulkanContext.VkApi.DeviceWaitIdle(VulkanContext.VkDevice.Handle);

    // Dispose window (disposes surface, swapchain, etc.)
    _window?.Dispose();
}
```

The window automatically disposes its `VkSurface` and `VkSwapchain` resources.

## Best Practices

1. **Single PollEvents call**: Call `PollEvents()` once per frame, at the start of your render loop
2. **Check IsOpen after polling**: Always check `IsOpen` after `PollEvents()` to handle close requests
3. **Store window reference**: Keep the window reference in your service rather than re-resolving it
4. **Dispose properly**: Always dispose windows in your cleanup function
5. **No global input**: Remember that input is per-window, not global

## Integration with Other Systems

- **Vulkan Graphics**: Windows provide surfaces and swapchains for rendering ([details](xref:sparkitect.vulkan.vulkan-graphics))
- **Game State System**: Window services are typically state-scoped ([details](xref:sparkitect.core.game-state-system))
- **Dependency Injection**: Access `IWindowManager` through DI ([details](xref:sparkitect.core.dependency-injection))

## Next Steps

- See [Vulkan Graphics](xref:sparkitect.vulkan.vulkan-graphics) for rendering to window surfaces
- See [Game State System](xref:sparkitect.core.game-state-system) for state function integration
- Review `samples/PongMod/` for complete window and input handling
