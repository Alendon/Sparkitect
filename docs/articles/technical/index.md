---
uid: articles.technical
title: Technical Systems
description: Technical implementation systems that provide functionality in Sparkitect Engine
---

# Technical Systems

The Technical Systems of Sparkitect build upon the Core Systems to provide the functional capabilities of the game engine. These systems implement specific features such as rendering, input handling, networking, and more.

## Planned Technical Systems

The following technical systems are planned for implementation in Sparkitect:

### Entity Component System (ECS)
The core simulation system that manages game entities, components, and systems.

### Game States
A system to manage multiple engine states and handle transitions between them. This system manages state compositions and shared state.

### Multiplayer & Networking
Integrated networking and multiplayer functionality built directly into the engine core, supporting client/server architecture.

### Input Handling
System for managing user input from various devices and translating it into game actions.

### Rendering
- **Vulkan Abstraction Layer**: A custom abstraction over Vulkan tailored for the engine's needs
- **Render Pipelines**: A centralized system for rendering based on a pipeline approach

### Audio
Sound output and management systems.

### Serialization
Flexible serialization system to support network synchronization and persistent data storage.

### UI System
Integration of a modern UI framework (Avalonia) directly into the engine, with modifications to support the modding system at both design time and runtime.

### Settings/Properties
A centralized settings system with multiple scopes (Game, Client, etc.) and a UI extension layer for configuration.

### Player Handling
Central system for managing players in a client/server architecture.

## Implementation Status

These technical systems are currently in the planning and design phase. Documentation will be expanded as each system is implemented.

## Design Philosophy

All technical systems are designed to adhere to Sparkitect's core principles:

1. **Modding-Centric**: Systems are designed to be extended and modified through mods
2. **Integration Over Duplication**: Systems leverage existing functionality rather than creating redundancy
3. **Modularity**: Components are separated to facilitate development and maintenance
4. **Registry-Based**: Resources are managed through the central Registry System