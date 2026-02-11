---
uid: sparkitect.core
title: Core Systems
description: Mod loading, dependency injection, registries, state machines, and function scheduling in Sparkitect Engine
---

# Core Systems

Everything in Sparkitect runs as a mod. These systems handle how mods are discovered and loaded, how services are wired together, how game objects are registered, and how runtime behavior is structured.

### Modding Framework

Defines how mods are structured, packaged, and loaded into the engine. The framework handles mod discovery and the loading process; the [Game State System](xref:sparkitect.core.game-state-system) controls when mods are loaded based on active states.

<xref:sparkitect.core.modding-framework>

### Dependency Injection

Inject services into your code using attributes and let the engine wire everything at runtime. Built for mod loading and unloading, with source-generated factories and container chaining across mod boundaries.

<xref:sparkitect.core.dependency-injection>

### Registry System

Register game objects like items, resources, and states by annotating your code. A three-level ID system (ModId, CategoryId, ItemId) gives every registered object a unique, type-safe reference.

<xref:sparkitect.core.registry-system>

### Game State System

Structure your game as a hierarchy of states and modules. States compose from modules, both define behavior through stateless functions, and the GSM owns mod lifecycles within its state hierarchy.

<xref:sparkitect.core.game-state-system>

### Stateless Functions

Define per-frame logic, transitions, and lifecycle hooks as static methods with scheduling attributes. The engine resolves parameters through DI and builds execution graphs from ordering constraints.

<xref:sparkitect.core.stateless-functions>

### Engine Initialization

Reference for the startup sequence from application launch to the first game state. Covers [`EngineBootstrapper`](xref:Sparkitect.EngineBootstrapper), core container creation, mod discovery, and state activation.

<xref:sparkitect.core.engine-initialization>

### External Dependencies

Managing NuGet and third-party assembly dependencies in mod archives.

<xref:sparkitect.core.external-dependencies>

### Optional Dependencies

Integrating with mods that may not be present at runtime, with full DI support for conditional service registration.

<xref:sparkitect.core.optional-dependencies>
