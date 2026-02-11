---
uid: sparkitect.vulkan
title: Vulkan Module
description: Vulkan graphics rendering and shader compilation
---

# Vulkan Module

The Vulkan module provides the rendering backend for Sparkitect, wrapping Vulkan with C# types that provide type safety, automatic resource tracking, and debugging capabilities.

Key concepts:

- **Vk prefix naming**: All wrapper types use the `Vk` prefix (e.g., `VkDevice`, `VkCommandPool`, `VkSwapchain`) to distinguish them from raw Silk.NET types.
- **VkResult error handling**: Vulkan operations return [`VkResult<T>`](xref:Sparkitect.Graphics.Vulkan.VkResult`1), a discriminated union that forces explicit error handling through pattern matching. There are no silent failures.
- **CallerContext debugging**: Every object creation method accepts a [`CallerContext`](xref:Sparkitect.Utils.CallerContext) parameter, automatically injected at compile time by the source generator. This enables precise resource leak debugging through the [`IObjectTracker`](xref:Sparkitect.Utils.IObjectTracker`1), which reports exactly where each Vulkan object was created.

## Topics

- **<xref:sparkitect.vulkan.vulkan-graphics>** - [`IVulkanContext`](xref:Sparkitect.Graphics.Vulkan.IVulkanContext), wrapper types, VkResult handling, CallerContext, resource lifecycle
- **<xref:sparkitect.vulkan.shader-compilation>** - Slang shader workflow, YAML resource registration, runtime access via [`IShaderManager`](xref:Sparkitect.Graphics.Vulkan.IShaderManager)
