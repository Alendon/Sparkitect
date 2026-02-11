---
uid: sparkitect.tooling
title: Tooling
description: Developer tools for building Sparkitect mods
---

# Tooling

Developer-facing tools for building and validating Sparkitect mods.

The Sparkitect SDK provides MSBuild-based tooling for the complete mod development workflow: project configuration, manifest generation, dependency resolution, and archive creation. These tools automate the build pipeline so that mod authors can focus on writing code rather than managing packaging and metadata.

Mod projects use the `<Project Sdk="Sparkitect.Sdk/x.y.z">` syntax to import the SDK, which provides MSBuild properties, targets, and build tasks for compiling mods into distributable `.sparkmod` archives.

The SDK guide covers project setup, dependency declaration, build output, and running mods. The mod specification defines the `.sparkmod` archive format and `manifest.json` schema used by the runtime mod loader.

## Topics

- **<xref:sparkitect.tooling.sdk-guide>** - How to use the Sparkitect SDK to create mods
- **<xref:sparkitect.tooling.mod-specification>** - Mod archive format and manifest specification
