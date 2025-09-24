---
uid: articles.core
title: Core Systems
description: Fundamental systems that form the foundation of Sparkitect Engine
---

# Core Systems

The Core Systems form the foundation of Sparkitect, establishing the architectural patterns and infrastructure that all other systems build upon. These systems are tightly integrated and work together to enable the engine's modding-centric architecture.

## Primary Core Systems

### Modding Framework

The Modding Framework defines how mods are structured, loaded, and interact with each other. It's the cornerstone of Sparkitect's design, enabling the entire engine to be extensible through mods. The framework handles mod discovery, loading, and lifecycle management.

[Learn more about the Modding Framework](modding-framework.md)

### Dependency Injection

Dependency Injection (DI) is the primary mechanism for components to access and interact with each other. Sparkitect uses a custom-built DI framework designed for modding and container chaining, with additional tooling to streamline common patterns through source generation.

[Learn more about the Dependency Injection system](dependency-injection.md)

### Registry System

The Registry System provides a centralized mechanism for tracking and managing game objects and resources. It enables consistent identification of resources using a combination of ModId, CategoryId, and ObjectId.

[Learn more about the Registry System](registry-system.md)

### Engine Initialization

The engine initialization process ties together all core systems, managing the sequence from application startup to the transfer of control to the first game state. The `EngineBootstrapper` class coordinates this process.

[Learn more about the Engine Initialization process](engine-initialization.md)

## System Integration

These core systems are tightly integrated:

1. **Engine Bootstrapper** initializes the core DI container
2. **Mod Loading** triggers registration of services in the DI container
3. **DI Container** provides access to Registry instances
4. **Registry System** enables mods to register and discover resources

Together, they create a foundation that enables other engine systems to be modular, extensible, and discoverable.

## Additional Core Systems

While the systems above form the essential infrastructure, Sparkitect includes additional core systems that build upon this foundation:

- **Entity Component System (ECS)**: The core simulation system
- **Game States**: Management of engine states and transitions
- **Serialization**: System for data persistence and network synchronization

These systems will be documented as the engine implementation progresses.
