---
uid: sparkitect.getting-started.intro
title: Introduction to Sparkitect
description: Philosophy, design principles, target use cases, and your first mod
---

# Introduction to Sparkitect

Sparkitect is a modular 3D game engine built on .NET, designed with modding as its foundational concept. Unlike traditional game engines where the core executable provides a complete environment, Sparkitect's base is intentionally minimal - the actual games and functionality are implemented through mods.

## Philosophy

The engine is built around several core principles:

### Modding-Centric Architecture

The entire engine is designed around the concept of modding. The core executable is essentially a framework that loads and manages mods, with games themselves implemented as collections of mods. This approach creates a unified system where there's minimal distinction between "engine code" and "mod code" - they operate through the same interfaces and mechanisms.

### Integration Over Duplication

When adding new functionality, Sparkitect prioritizes integrating with existing systems rather than creating parallel implementations. For example, if an inventory system is being added and an Entity Component System is already in place, the inventory should be built atop the ECS rather than as a separate system.

### Modularity

Components are designed to be as modular and separated as possible to facilitate development and maintenance. This principle sometimes requires careful balancing against the "integration over duplication" principle.

### Registry-Based Resource Management

All game "objects" - from component types to textures to key bindings - are managed through a central Registry System. This provides a consistent way to reference any resource by ID throughout the engine and mods.

## Technical Foundation

Sparkitect is built using:

- **Full .NET**: Utilizing the latest version of .NET for performance and modern language features
- **Custom ProjectSDK**: For optimized build support
- **Custom Dependency Injection**: Source-generated, zero-reflection DI system with immutable containers

## Target Use Cases

Sparkitect is particularly well-suited for:

- Games that benefit from extensive modding capabilities
- Projects where modularity is a priority
- 3D games across various genres

It's designed for PC platforms, aiming to support Windows, Linux, and macOS.

## Development Approach

Sparkitect is designed as a framework rather than a full-featured editor-centric engine. Unlike engines that focus on visual editors and built-in world creation tools, Sparkitect emphasizes programmatic control and mod-based extensibility.

This makes it especially suitable for games that have minimal requirements for visual editing tools and instead focus on runtime behavior and systems.

## Getting Started

This section walks you through creating your first Sparkitect mod - a minimal "hello world" that loads and logs output.

### Prerequisites

- The current .NET SDK
- An IDE (Visual Studio, Rider, or VS Code with C# extension)

### 1. Create the Project

Create a new class library project using the Sparkitect SDK:

```xml
<Project Sdk="Sparkitect.Sdk/0.1.0">
  <PropertyGroup>
    <ModId>hello_world</ModId>
    <ModName>Hello World Mod</ModName>
    <ModVersion>1.0.0</ModVersion>
    <ModAuthor>YourName</ModAuthor>
    <ModDescription>A simple hello world mod</ModDescription>
    <IsRootMod>true</IsRootMod>
  </PropertyGroup>
</Project>
```

See [SDK Project Configuration](xref:sparkitect.tooling.sdk-guide) for all available properties.

### 2. Create a Module

Modules contain your mod's logic. Here is a simplified module based on the `samples/MinimalSampleMod/` pattern:

```csharp
using Serilog;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

// Generated ID extension namespace — name derived from your ModId
using HelloWorld.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;

namespace HelloWorld;

[ModuleRegistry.RegisterModule("hello")]
public partial class HelloModule : IStateModule
{
    public static Identification Identification => StateModuleID.HelloWorld.Hello;

    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    [TransitionFunction("say_hello")]
    [OnCreateScheduling]
    private static void SayHello(ILogger logger)
    {
        logger.Information("Hello from my first mod!");
    }
}
```

This module:
- Registers itself with the module registry via the generated `RegisterModule` attribute
- Defines a transition function that runs when the module is created during a state transition
- Uses dependency injection to receive the logger through method parameters

> [!NOTE]
> A complete working mod also requires an `IEntryStateSelector` (to select the initial state) and an `IStateDescriptor` (to define the state and its modules). See `samples/MinimalSampleMod/` for the full implementation with all required pieces.

### 3. Build and Run

Build your project. The SDK produces a `.sparkmod` archive in the output directory (e.g., `bin/Debug/net10.0/hello_world-1.0.0.sparkmod`).

To run the engine with your mod, use the `-addModDirs` CLI argument pointing to your build output directory:

```bash
dotnet run --project path/to/Sparkitect.csproj -- -addModDirs=path/to/your/mod/bin/Debug/net10.0
```

For IDE-based workflows using `launchSettings.json`, see the [SDK Guide](xref:sparkitect.tooling.sdk-guide).

### Next Steps

This minimal example demonstrates the core patterns. For deeper understanding, explore these topics:

- **SDK & Build**: [SDK Guide](xref:sparkitect.tooling.sdk-guide)
- **Modules and States**: [Game State System](xref:sparkitect.core.game-state-system)
- **State Functions**: [Stateless Functions](xref:sparkitect.core.stateless-functions)
- **Service Registration**: [Dependency Injection](xref:sparkitect.core.dependency-injection)
- **Object Registration**: [Registry System](xref:sparkitect.core.registry-system)
- **Mod Structure**: [Modding Framework](xref:sparkitect.core.modding-framework)

For a complete working example, see `samples/MinimalSampleMod/`. For a full game with graphics, input, and gameplay, see `samples/PongMod/`.
