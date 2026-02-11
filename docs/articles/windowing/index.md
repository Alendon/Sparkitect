---
uid: sparkitect.windowing
title: Windowing Module
description: Window management, input handling, and Vulkan surface integration
---

# Windowing Module

The Windowing module provides window creation, input handling, and Vulkan surface integration. Input is accessed directly through window objects rather than through a global input service.

Key concepts:

- **Input-per-window**: Each window owns its own input state. There is no global `IInputService`. Keyboard and mouse state are accessed through `window.Keyboard` and `window.Mouse` properties, making input ownership explicit and supporting multiple windows with independent input.
- **Vulkan surface integration**: Windows provide Vulkan surface and swapchain access, serving as the bridge between the windowing system and the rendering backend. Frame acquisition and presentation are handled through the window object.
- **State-scoped lifecycle**: [`IWindowManager`](xref:Sparkitect.Windowing.IWindowManager) is state-scoped (via `[StateFacade]`), meaning windows have the same lifecycle as the game state they belong to.
- **Event polling**: Call `PollEvents()` once per frame to process window system events before reading input state.

## Topics

- **<xref:sparkitect.windowing.windowing-input>** - [`IWindowManager`](xref:Sparkitect.Windowing.IWindowManager), [`ISparkitWindow`](xref:Sparkitect.Windowing.ISparkitWindow), keyboard/mouse input, Vulkan surface integration
