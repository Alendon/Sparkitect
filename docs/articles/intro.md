---
uid: sparkitect.getting-started.intro
title: Introduction to Sparkitect
description: Philosophy, design principles, and your first mod
---

# Introduction to Sparkitect

Sparkitect is a modular 3D game engine built on .NET 10. The engine itself is a minimal executable that loads mods; games are implemented as collections of mods that use the same interfaces and mechanisms as the engine's own code.

## Philosophy

- **Everything is a mod.** There is no separate "engine API" vs "mod API". The engine's built-in functionality ships as mods that use the same attributes, registries, and DI system available to your code.
- **Integration over duplication.** New functionality builds on existing systems rather than creating parallel implementations. If an ECS is in place, an inventory system should be built on top of it.
- **Modularity.** Components are separated to facilitate independent development and maintenance.
- **Registry-based resources.** All game objects (component types, textures, key bindings) are managed through a central [Registry System](xref:sparkitect.core.registry-system), providing consistent ID-based access throughout the engine and mods.

## Technical Foundation

- **.NET 10** for performance and modern language features
- **[Project SDK](xref:sparkitect.tooling.sdk)** that handles mod manifest generation, archive packaging, and dependency resolution
- **Source-generated DI** with zero-reflection resolution and immutable containers (see [Dependency Injection](xref:sparkitect.core.dependency-injection))

Sparkitect is a framework, not an editor-centric engine. It emphasizes programmatic control and mod-based extensibility over visual editing tools. It targets PC platforms (Windows, Linux, macOS).

## Getting Started

This walks you through creating a minimal mod that loads and logs output.

### Prerequisites

- The .NET 10 SDK
- An IDE (Visual Studio, Rider, or VS Code with C# extension)

### 1. Create the Project

Create a new class library project using the Sparkitect SDK:

```xml
<Project Sdk="Sparkitect.Sdk/1.0.0">
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

See [Project SDK](xref:sparkitect.tooling.sdk) for all available properties.

### 2. Create a Module

Modules contain your mod's logic. Here is a minimal module:

```csharp
using Serilog;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

// Generated ID namespace — derived from your project's RootNamespace
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
    private static void SayHello()
    {
        Log.Information("Hello from my first mod!");
    }
}
```

This module:
- Registers itself with the module registry via the generated `RegisterModule` attribute
- Defines a transition function that runs when the module is created during a state transition
- Uses the static `Serilog.Log` logger for output

> [!NOTE]
> A complete working mod also requires an [`IEntryStateSelector`](xref:Sparkitect.GameState.IEntryStateSelector) (so the engine knows which state to start) and an [`IStateDescriptor`](xref:Sparkitect.GameState.IStateDescriptor) (to define the state and its modules). See `samples/MinimalSampleMod/` for a full implementation with all required pieces.

> [!TIP]
> The generated ID extensions namespace (`HelloWorld.CompilerGenerated.IdExtensions`) is derived from your project's `RootNamespace`. If your project uses a different root namespace, the generated namespace changes accordingly.

### 3. Build and Run

Build your project. The SDK produces a `.sparkmod` archive in the output directory (e.g., `bin/Debug/net10.0/hello_world-1.0.0.sparkmod`).

**Using launchSettings (recommended for IDE workflows):**

Add a `Properties/launchSettings.json` to your mod project:

```json
{
  "profiles": {
    "Sparkitect": {
      "commandName": "Executable",
      "executablePath": "path/to/Sparkitect",
      "commandLineArgs": "-addModDirs=$(ProjectDir)bin/$(Configuration)/net10.0",
      "workingDirectory": "$(ProjectDir).run"
    }
  }
}
```

This lets you press F5 in your IDE to build and run with the engine. The `workingDirectory` isolates runtime files in a `.run` folder.

**From the command line:**

```bash
Sparkitect -addModDirs=path/to/your/mod/bin/Debug/net10.0
```

You can also place `.sparkmod` archives directly in the engine's `mods/` directory.

See [Project SDK](xref:sparkitect.tooling.sdk) for more on launch configuration.

### Next Steps

- [Engine Overview](xref:sparkitect.getting-started.overview) for the recurring patterns and lifecycle
- [Game State System](xref:sparkitect.core.game-state-system) for modules, states, and transitions
- [Stateless Functions](xref:sparkitect.core.stateless-functions) for transition and per-frame logic
- [Dependency Injection](xref:sparkitect.core.dependency-injection) for service registration
- [Registry System](xref:sparkitect.core.registry-system) for object registration
- [Modding Framework](xref:sparkitect.core.modding-framework) for mod structure and loading

For a complete working example, see `samples/MinimalSampleMod/`. For a full game with graphics, input, and gameplay, see `samples/PongMod/`.
