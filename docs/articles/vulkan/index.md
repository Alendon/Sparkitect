---
uid: sparkitect.vulkan
title: Vulkan Module
description: Vulkan graphics rendering and shader compilation
---

# Vulkan Module

The Vulkan module provides the rendering backend for Sparkitect, wrapping Vulkan with ergonomic C# types that provide type safety, automatic resource tracking, and debugging capabilities.

Key concepts:

- **Vk prefix naming**: All wrapper types use the `Vk` prefix (e.g., `VkDevice`, `VkCommandPool`, `VkSwapchain`) to distinguish them from raw Silk.NET types.
- **VkResult error handling**: Vulkan operations return `VkResult<T>`, a discriminated union that forces explicit error handling through pattern matching -- there are no silent failures.
- **CallerContext debugging**: Every object creation method accepts a `CallerContext` parameter, automatically injected at compile time by the source generator. This enables precise resource leak debugging through the `ObjectTracker`, which can report exactly where each Vulkan object was created.

## Topics

- **<xref:sparkitect.vulkan.vulkan-graphics>** - Wrapper types, VkResult handling, CallerContext, resource lifecycle
- **<xref:sparkitect.vulkan.shader-compilation>** - Slang shader workflow, YAML resource registration, runtime access
