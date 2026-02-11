---
uid: sparkitect.core.engine-initialization
title: Engine Initialization
description: Startup sequence from application launch to first game state
---

# Engine Initialization

When the engine starts, it goes through a fixed sequence before any mod code runs. Understanding this sequence helps when debugging startup issues or when your mod needs to interact with early-stage services.

## Overview

The startup sequence:

1. Logger setup
2. Root container creation (core services)
3. CLI argument processing
4. Mod discovery
5. Root state entry (mod loading, registry processing, state activation)
6. Main loop
7. Cleanup on shutdown

The [`EngineBootstrapper`](xref:Sparkitect.EngineBootstrapper) class drives this sequence.

## Initialization Sequence

### 1. Logger Initialization

Serilog is configured first, with output to both files and console. This ensures all subsequent steps can log.

### 2. Root Container Creation

The bootstrapper creates the Root container with the eight [`CoreModule`](xref:Sparkitect.GameState.CoreModule) services. These are registered via source-generated factories and are available before any mods load:

| Service | Role |
|---------|------|
| [`ICliArgumentHandler`](xref:Sparkitect.Utils.ICliArgumentHandler) | Processes command-line arguments |
| [`IIdentificationManager`](xref:Sparkitect.Modding.IIdentificationManager) | String-to-numeric ID mappings |
| [`IResourceManager`](xref:Sparkitect.Modding.IResourceManager) | Resource loading |
| [`IModManager`](xref:Sparkitect.Modding.IModManager) | Mod discovery and loading |
| [`IRegistryManager`](xref:Sparkitect.Modding.IRegistryManager) | Registry lifecycle |
| [`IGameStateManager`](xref:Sparkitect.GameState.IGameStateManager) | State transitions and main loop |
| [`IModDIService`](xref:Sparkitect.DI.IModDIService) | DI container creation for mods |
| [`IStatelessFunctionManager`](xref:Sparkitect.Stateless.IStatelessFunctionManager) | Function discovery, sorting, and wrapper creation |

### 3. CLI Argument Processing

Command-line arguments are processed, allowing runtime configuration before mods are loaded.

### 4. Mod Discovery

`IModManager` scans the mods directory and reads manifests from discovered archives. At this stage, mods are **discovered but not loaded**. The actual loading happens during root state entry.

### 5. Entering Root State

`IGameStateManager.EnterRootState()` performs the core initialization:

**Root Mod Selection:**

1. If a `roots.json` configuration file exists, only the specified mods are loaded
2. Otherwise, all discovered mods are loaded

**Mod Loading:**

The selected root mods are loaded via `IModManager`. Assemblies are extracted from zip archives and loaded from memory. Dependencies are validated (missing or incompatible dependencies cause errors), but loading itself has no deterministic order and no side effects.

**Registry Setup:**

Four registries are added to `IRegistryManager` and processed for the loaded mods:

- [`ModuleRegistry`](xref:Sparkitect.GameState.ModuleRegistry)
- [`StateRegistry`](xref:Sparkitect.GameState.StateRegistry)
- [`PerFrameRegistry`](xref:Sparkitect.GameState.PerFrameRegistry)
- [`TransitionRegistry`](xref:Sparkitect.GameState.TransitionRegistry)

After processing, all pending state and module registrations are finalized.

**Entry State Selection:**

One mod in the root mod set must provide an [`IEntryStateSelector`](xref:Sparkitect.GameState.IEntryStateSelector) implementation. This tells the engine which state to enter first. Most mods never need to implement this; it is the equivalent of a "game project" entry point in a classic engine. If no implementation is found, startup fails.

**State Activation:**

The engine creates the entry state frame with its own DI container (child of the Root container), executes transition enter methods via the [stateless function system](xref:sparkitect.core.stateless-functions), and starts the main loop.

Transition enter methods are functions annotated with [`TransitionFunctionAttribute`](xref:Sparkitect.Stateless.TransitionFunctionAttribute) that are resolved and topologically sorted by `IStatelessFunctionManager`.

### 6. Main Loop

The main loop runs [`PerFrameFunctionAttribute`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute)-annotated functions from the active state and its modules on each iteration. Between frames, the loop checks for pending state transitions and executes them inline. The loop continues until shutdown is requested.

See [Stateless Functions](xref:sparkitect.core.stateless-functions) for details on function resolution and scheduling.

### 7. Cleanup

On shutdown, the bootstrapper disposes the Root container (which cascades to all child state containers) and flushes remaining logs.

## Container Hierarchy

The engine uses a hierarchy of containers:

1. **Root Container**: Created during bootstrapping with the eight core services (see step 2). Persists for the application lifetime.
2. **State Containers**: Created during state transitions. Each state container is a child of its parent state's container (or Root for the entry state). Destroyed when the state is popped.

All containers are immutable once created. New states produce new child containers rather than modifying existing ones.
