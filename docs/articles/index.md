---
uid: articles.index
---

# Sparkitect Engine Documentation

Welcome to the Sparkitect Engine documentation. This documentation serves as a reference for both engine developers and mod creators.

## Core Design Principles

Sparkitect is built on several key principles that inform its architecture and capabilities:

1. **Modding-Centric Design**: The engine executable itself is just a foundation, with games implemented as mods. Every aspect of the engine revolves around modding.

2. **Integration Over Duplication**: New systems should leverage existing ones when appropriate rather than creating redundant functionality.

3. **Modularity**: Components are designed to be as modular and separated as possible to facilitate development and maintenance.

4. **Registry System**: A centralized registration mechanism for all game objects and resources, enabling consistent identification and access.

## Documentation Structure

This documentation is organized into the following sections:

- **[Core Systems](core/index.md)**: Foundational systems that power the engine
    - Modding Framework
    - Dependency Injection
    - Registry System
    - Entity Component System
    - Game States

- **[Technical Systems](technical/index.md)**: Implementation of technical capabilities
    - Multiplayer & Networking
    - Input Handling
    - Rendering
    - Audio
    - Serialization

- **[API Reference](~/api/index.md)**: Technical reference for engine APIs

## Getting Started

If you are new to Sparkitect, we recommend starting with the [Introduction](intro.md) to understand the engine's philosophy and architecture before diving into specific systems.

## Target Audience

This documentation is primarily intended for:

- Engine developers extending or maintaining Sparkitect
- Mod developers creating content using the Sparkitect framework

It assumes a working knowledge of C# and game development concepts.