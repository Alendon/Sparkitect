---
uid: sparkitect.core
title: Core Systems
description: Foundational systems that form the backbone of Sparkitect Engine
---

# Core Systems

The Core Systems form the foundation of Sparkitect, establishing the architectural patterns and infrastructure that all other systems build upon.

## Primary Systems

### Modding Framework

The engine's cornerstone. Defines how mods are structured, loaded, and interact with each other. The framework handles mod discovery, loading, and lifecycle management.

<xref:sparkitect.core.modding-framework>

### Dependency Injection

The primary mechanism for components to access each other. A custom-built DI framework designed for modding and container chaining, with source-generated factories and configurators.

<xref:sparkitect.core.dependency-injection>

### Registry System

Centralized tracking and management of game objects and resources. Uses a three-level identification hierarchy (ModId, CategoryId, ItemId) for consistent resource referencing.

<xref:sparkitect.core.registry-system>

### Engine Initialization

The startup sequence from application launch to the first game state. The `EngineBootstrapper` coordinates core container creation, mod loading, and state activation.

<xref:sparkitect.core.engine-initialization>

### Stateless Functions

Attribute-based static methods that define behavior in modules and states. Uses DI for parameters, scheduling attributes for execution timing, and source-generated wrappers.

<xref:sparkitect.core.stateless-functions>

### Game State System

Hierarchical state machine managing runtime configuration. States compose from modules, and both define behavior through stateless functions.

<xref:sparkitect.core.game-state-system>

## Additional Topics

- [External Dependencies](xref:sparkitect.core.external-dependencies) - Managing NuGet and third-party dependencies in mod archives
- [Optional Dependencies](xref:sparkitect.core.optional-dependencies) - Integrating with mods that may not be present
