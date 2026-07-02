# Sparkitect

A modular game engine for .NET with deep modding support.

**[Documentation](https://alendon.github.io/Sparkitect/articles/index.html)** — start here for guides, API reference, and articles.

## Overview

Sparkitect is a game engine where everything is a mod. There is no separate "engine API" vs "mod API". The engine and mods register services through the same attributes, define logic through the same function types, and populate registries through the same generated infrastructure. Anything the engine does, a mod can extend or replace.

The engine uses Roslyn source generators to create all DI wiring at compile time. The frame loop executes with zero reflection: generated wrapper classes hold pre-resolved dependencies and call your methods directly. Containers are immutable during simulation, so reads require no locks. Invalid configurations are caught at build time through analyzers rather than at runtime.

v1.5 adds a general-purpose Entity Component System and a render-graph resource model over the Vulkan backend.

Sparkitect is a framework, not an editor. It targets PC platforms (Windows, Linux, macOS).

## Key Features

- **Source-generated dependency injection** — zero-reflection resolution, validated at build time.
- **Game state machine** — hierarchical states composed from modules with dependency ordering.
- **Registry system** — dual human-readable and numeric identifiers with per-state activation.
- **Attribute-driven discovery** — mark code with attributes; source generators handle the wiring.
- **Entity Component System** — SoA archetype storage with chunk-based queries and command buffers.
- **Render graph** — declarative resource model over a Vulkan backend (Silk.NET).
- **Mod SDK** — MSBuild SDK for manifest generation, archive packaging, and dependency resolution.
- **Rider modding plugin** — Go to Registration, Find Usages, and YAML navigation for mod projects.
- **Diagnostic analyzers** — SPARKXXYY codes with actionable messages and IDE highlighting.

## Project Structure

```
src/
  Sparkitect/                          Engine core
  Sparkitect.Sdk/                      MSBuild SDK for mod projects
  Sparkitect.Graphics.Vulkan.Vma/      Vulkan memory allocation
gen/
  Sparkitect.Generator/                Roslyn source generators
samples/
  PongMod/                             Full game with rendering and input
  SpaceInvadersMod/                    ECS-driven game
  MinimalSampleMod/                    Bare minimum mod structure
  ColorProviderMod/                    Optional dependency and registry patterns
  BackgroundColorMod/                  Inter-mod integration example
tools/
  RiderPlugin/                         Rider modding plugin (outside the solution)
tests/                                 TUnit test projects
docs/                                  DocFX documentation site
benchmark/                             Performance benchmarks
build/                                 Build infrastructure
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Vulkan-capable GPU and drivers (for graphics mods)
- An IDE: Visual Studio, Rider, or VS Code with the C# extension

## Building

```bash
dotnet build
```

To run tests (uses the built-in test runner, not `dotnet test`):

```bash
dotnet run --project tests/Sparkitect.Generator.Tests
dotnet run --project tests/Sparkitect.Tests
```

## Quick Start

Create a mod project using the Sparkitect SDK:

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

Define a module with a transition function:

```csharp
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

Build your project to produce a `.sparkmod` archive, then run the engine with your mod directory:

```bash
Sparkitect -addModDirs=path/to/your/mod/bin/Debug/net10.0
```

See the [Getting Started guide](https://alendon.github.io/Sparkitect/articles/intro.html) for a complete walkthrough including state descriptors and entry state selectors.

## Samples

The `samples/` directory contains working mods. `PongMod` is a complete game with rendering and input handling. `SpaceInvadersMod` is the ECS validation target, exercising archetype storage, queries, and command buffers. `MinimalSampleMod` is the simplest starting point for a new mod. `ColorProviderMod` and `BackgroundColorMod` demonstrate optional dependency patterns and inter-mod integration.
