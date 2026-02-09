---
uid: api.index
title: API Reference
description: Complete API reference documentation for Sparkitect Engine
---

# Sparkitect API Reference

This section provides complete API reference documentation for Sparkitect Engine, automatically generated from source code comments.

## Namespaces

The Sparkitect Engine codebase is organized into the following primary namespaces:

### Core Engine Namespaces

- **Sparkitect** - Core engine types and entry points
  - [EngineBootstrapper](xref:Sparkitect.EngineBootstrapper) - Main entry point for initializing the engine

- **Sparkitect.DI** - Dependency injection framework
  - [CoreConfigurator](xref:Sparkitect.DI.CoreConfigurator) - Base class for IoC configuration
  - [IRegistryConfigurator](xref:Sparkitect.DI.IRegistryConfigurator) - Interface for registry configuration

- **Sparkitect.ECS** - Entity Component System implementation
  - [Entity](xref:Sparkitect.ECS.Entity) - Entity representation
  - [EntityId](xref:Sparkitect.ECS.EntityId) - Entity identifier
  - [IComponent](xref:Sparkitect.ECS.IComponent) - Interface for components
  - [System](xref:Sparkitect.ECS.System) - Base class for systems
  - [IWorld](xref:Sparkitect.ECS.IWorld) - World container interface

- **Sparkitect.GameState** - Game state management
  - [IGameStateManager](xref:Sparkitect.GameState.IGameStateManager) - Game state manager interface
  - (planned) IGameStateSystem — effectively covered by IGameStateManager

- **Sparkitect.Graphics.Vulkan** - Vulkan rendering implementation
  - [IVulkanContext](xref:Sparkitect.Graphics.Vulkan.IVulkanContext) - Vulkan context interface
  - [VkDevice](xref:Sparkitect.Graphics.Vulkan.VkDevice) - Vulkan device wrapper

- **Sparkitect.Modding** - Modding framework and registry system
  - [IModManager](xref:Sparkitect.Modding.IModManager) - Mod management interface
  - [IRegistryManager](xref:Sparkitect.Modding.IRegistryManager) - Registry management interface
  - [ModManifest](xref:Sparkitect.Modding.ModManifest) - Mod metadata representation

- **Sparkitect.Utils** - Utility classes and helpers
  - [PropertyManager](xref:Sparkitect.Utils.PropertyManager) - Property management
  - **Sparkitect.Utils.Serialization** - Data serialization utilities

### Third-Party Libraries

The core DI used by Sparkitect is custom-built for the engine and modding requirements.

## Using the API Reference

This reference is intended for developers who need detailed information about specific classes, methods, properties, and interfaces. Each API item includes:

- Member signatures and return types
- Parameter descriptions
- Inheritance hierarchy
- XML documentation comments from the source code
- Examples (where available)

## API Stability Guidelines

Sparkitect Engine follows semantic versioning principles:

- Public APIs are guaranteed not to have breaking changes in minor or patch releases
- Internal APIs (those in namespaces ending with `.Internal`) are not considered part of the public API surface and may change at any time

## Related Resources

- For conceptual information and guides, see the [Articles](~/articles/index.md) section
