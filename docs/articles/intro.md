---
uid: articles.intro
---

# Introduction to Sparkitect

Sparkitect is a modular 3D game engine built on .NET, designed with modding as its foundational concept. Unlike traditional game engines where the core executable provides a complete environment, Sparkitect's base is intentionally minimal - the actual games and functionality are implemented through mods.

## Philosophy

The engine is built around several core principles:

### Modding-Centric Architecture

The entire engine is designed around the concept of modding. The core executable is essentially a framework that loads and manages mods, with games themselves implemented as collections of mods. This approach creates a unified system where there's minimal distinction between "engine code" and "mod code" - they operate through the same interfaces and mechanisms.

### Integration Over Duplication

When adding new functionality, Sparkitect prioritizes integrating with existing systems rather than creating parallel implementations. For example, if an inventory system is being added and an Entity Component System is already in place, the inventory should be built atop the ECS rather than as a separate system.

### Modularity

Components are designed to be as modular and separated as possible to facilitate development and maintenance. This principle sometimes requires careful balancing against the "integration over duplication" principle.

### Registry-Based Resource Management

All game "objects" - from component types to textures to key bindings - are managed through a central Registry System. This provides a consistent way to reference any resource by ID throughout the engine and mods.

## Technical Foundation

Sparkitect is built using:

- **Full .NET**: Utilizing the latest version of .NET for performance and modern language features
- **Custom ProjectSDK**: For optimized build support
- **Dryloc**: For dependency injection and inversion of control
- **Vulkan**: As the graphics backend with a custom abstraction layer

## Target Use Cases

Sparkitect is particularly well-suited for:

- Games that benefit from extensive modding capabilities
- Projects where modularity is a priority
- 3D games across various genres

It's designed for PC platforms, aiming to support Windows, Linux, and macOS.

## Development Approach

Sparkitect is designed as a framework rather than a full-featured editor-centric engine. Unlike engines that focus on visual editors and built-in world creation tools, Sparkitect emphasizes programmatic control and mod-based extensibility.

This makes it especially suitable for games that have minimal requirements for visual editing tools and instead focus on runtime behavior and systems.