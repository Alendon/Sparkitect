---
uid: articles.core.engine-initialization
---

# Engine Initialization

This document describes the initialization process of the Sparkitect engine, from application startup to the transition to the first game state.

## Overview

The engine initialization process follows these key steps:

1. Core IoC container creation
2. CLI argument processing
3. Root mod discovery and loading
4. Registry processing
5. Transition to the first game state

The `EngineBootstrapper` class manages this sequence, serving as the central coordination point for engine startup and shutdown.

## Initialization Sequence

The `EngineBootstrapper` manages the initialization process through these steps:

### 1. Logger Initialization

The engine initializes the logging system (Serilog) before any other operations, configuring log output to both files and console.

### 2. Root Container Creation

The bootstrapper creates the Root CoreContainer with essential engine services:
- **CliArgumentHandler**: Processes command-line arguments
- **IdentificationManager**: Manages string ↔ numeric ID mappings
- **ResourceManager**: Handles resource loading
- **ModManager**: Coordinates mod discovery and loading
- **RegistryManager**: Manages registry lifecycle
- **GameStateManager**: Controls state transitions and main loop
- **ModDIService**: Provides DI container creation for mods

These services are registered via source-generated factories and are available before any mods are loaded.

### 3. CLI Argument Processing

The CliArgumentHandler processes command-line arguments, allowing runtime configuration before mods are loaded.

### 4. Mod Discovery

The ModManager scans the "mods" folder and reads manifests from discovered archives. At this stage, mods are **discovered but not loaded** - the actual loading happens during root state entry.

### 5. Entering Root State

The GameStateManager's `EnterRootState()` method performs the core initialization:

**Mod Loading:**
- Loads all discovered mods via ModManager
- Assemblies are loaded from zip streams (which remain open)
- Dependencies are resolved and load order determined

**Registry Setup:**
- Adds StateRegistry and ModuleRegistry to the RegistryManager
- Processes these registries for all loaded mods
- Finalizes all pending state and module registrations

**Entry State Selection:**
- Queries discovered `IEntryStateSelector` implementations
- Selects the initial active state (not Root - Root is semantic anchor only)

**State Activation:**
- Creates the entry state frame with its DI container (child of Root container)
- Executes module `[OnCreate]` functions
- Executes state `[OnFrameEnter]` functions
- Starts the main loop

### 6. Main Loop

The main loop executes `[PerFrame]` functions from the active state and its modules until a transition or shutdown is requested.

### 7. Cleanup

On shutdown, the bootstrapper disposes the Root container and flushes remaining logs.

## Container Hierarchy

The engine uses a hierarchy of containers:

1. **Root Container**: Created during bootstrapping with essential engine services (see step 2 above)
2. **State Containers**: Created during state transitions, forming a hierarchical stack where each state container is a child of its parent state's container (or Root for the entry state)

The Root container persists for the application lifetime, while state containers are created and destroyed during state transitions. All containers are immutable once created - subsequent operations create new child containers rather than modifying existing ones.

## Clean-Up Process

The engine performs clean-up operations when shutting down:

1. Game states are properly terminated
2. Mod resources are released
3. Zip streams are closed
4. Other system resources are freed

The `CleanUp` method in the `EngineBootstrapper` class handles these operations.
