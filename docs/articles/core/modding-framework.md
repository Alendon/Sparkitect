---
uid: sparkitect.core.modding-framework
title: Modding Framework
description: Mod discovery, loading, and the role of mods in the engine architecture
---

# Modding Framework

Sparkitect is built around mods. The engine itself is a virtual mod, and all game functionality is delivered through the same mod system that third-party mods use. There is no separation between "engine code" and "mod code" at the architectural level.

## Creating a Mod

A mod is an SDK project that produces a `.sparkmod` archive. The minimal project file:

```xml
<Project Sdk="Sparkitect.Sdk/1.0.0">
    <PropertyGroup>
        <ModId>my_mod</ModId>
        <ModName>My Mod</ModName>
        <ModVersion>1.0.0</ModVersion>
    </PropertyGroup>

    <ItemGroup>
        <ModProjectDependency Include="../../src/Sparkitect/Sparkitect.csproj" />
    </ItemGroup>
</Project>
```

The SDK handles manifest generation, dependency detection, and archive packaging. See the [Project SDK](xref:sparkitect.tooling.sdk) guide for the full property reference and build details.

### Root Mods

A mod marked with `<IsRootMod>true</IsRootMod>` can be selected as part of the root mod set during engine startup. One root mod must provide an [`IEntryStateSelector`](xref:Sparkitect.GameState.IEntryStateSelector) implementation so the engine knows which state to start. Most mods are not root mods and do not need to think about this.

## Mod Discovery

The engine discovers mods automatically on startup:

1. Scans the `mods/` directory (relative to the engine executable) for `.sparkmod` archives
2. Reads each archive's manifest to populate the list of available mods
3. Additional directories can be specified via the `addModDirs` CLI argument

Discovery only reads manifests. No assemblies are loaded and no code runs during this step.

```
myproject/
  mods/
    my_mod-1.0.0.sparkmod
    other_mod-2.1.0.sparkmod
```

## Mod Loading

After discovery, the [Game State System](xref:sparkitect.core.game-state-system) determines which mods to load. Currently, this happens at engine startup when the root state is entered. The [`IGameStateManager`](xref:Sparkitect.GameState.IGameStateManager) owns loaded mods and their lifecycle.

Loading a set of mods:

1. Validates that all required dependencies are present (see [External Dependencies](xref:sparkitect.core.external-dependencies))
2. Loads mod assemblies into an isolated `AssemblyLoadContext`
3. Registers each mod with the [`IIdentificationManager`](xref:Sparkitect.Modding.IIdentificationManager) for numeric ID assignment
4. Notifies the resource manager so mod assets become available

Mods within a load call are not loaded in any guaranteed order. The loading process validates dependencies but does not attempt to sequence mods based on them.

### Root Mod Selection

On startup, the engine selects root mods using `mods/roots.json`:

```json
[
    { "Id": "my_game" },
    { "Id": "my_framework", "Version": "1.0.0" }
]
```

If `roots.json` does not exist, the engine falls back to all discovered mods that have `IsRootMod` set to `true`. If no mods declare themselves as root mods, all discovered mods are loaded as a final fallback.

## Dependencies

Each mod declares relationships with other mods through its manifest (generated from MSBuild properties by the SDK). Three relationship types exist:

| Type | Behavior |
|------|----------|
| Required | Mod fails to load if the dependency is missing or the version is outside range |
| Optional | Mod loads regardless; optional types are isolated through CLR lazy loading |
| Incompatible | Mod fails to load if the incompatible mod is present |

Dependencies are validated before any assemblies are loaded. Version constraints use semantic versioning ranges.

See [External Dependencies](xref:sparkitect.core.external-dependencies) and [Optional Dependencies](xref:sparkitect.core.optional-dependencies) for declaration syntax and usage patterns.

## Registration and Configuration

Mod loading and mod processing are separate steps, which is why load order does not matter. When the [Game State System](xref:sparkitect.core.game-state-system) builds a new state frame, the sequence is:

1. **Load mods** (physical assembly loading)
2. **Process GSM registries** for the newly loaded mods: modules, states, per-frame functions, and transitions. These are the registries the GSM needs to construct the frame, so they are processed before it is entered.
3. **Build and enter the new frame.** Module transition functions run here, and any other registries (current or future) that are not GSM-correlated are processed by their respective modules during this step.

New mods fully contribute to the frame from the start. Individual systems apply their own ordering during processing where needed. Because all of this happens in a controlled construction and transition sequence, the order in which mods were physically loaded is irrelevant.

The same sequence applies to the initial root state at engine startup and to child states that add mods via [`RequestWithModChange`](xref:Sparkitect.GameState.IGameStateManager.RequestWithModChange*).

Most of this is automatic. Annotating a class with [`[StateService]`](xref:Sparkitect.GameState.StateServiceAttribute`2) causes the source generator to produce a configurator that the engine discovers and invokes during frame construction. Registry entries work the same way through generated attributes. You do not call registration APIs directly unless you are building engine-level infrastructure.

```csharp
// This is all you need. The generator handles registration.
[StateService<IMyService, MyModule>]
public class MyService : IMyService { }
```

See [Dependency Injection](xref:sparkitect.core.dependency-injection) for service registration and [Registry System](xref:sparkitect.core.registry-system) for object registries.

## Mod Archives

A `.sparkmod` archive is a zip file containing:

| Entry | Description |
|-------|-------------|
| `manifest.json` | Mod metadata, dependencies, assembly references |
| `{ModId}.dll` | Primary mod assembly |
| `*.dll` | Additional required assemblies (non-mod dependencies) |
| `*.sparkres.yaml` | Declarative resource registrations (see [Registry System](xref:sparkitect.core.registry-system)) |
| Other files | Assets, data files accessible through the resource manager |

The SDK produces this archive automatically on build. Archives remain open at runtime so mods can access embedded resources.
