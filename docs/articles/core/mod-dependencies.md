---
uid: sparkitect.core.external-dependencies
title: External Dependencies
description: Including NuGet packages and third-party libraries in Sparkitect mods
---

# External Dependencies

Mods can use NuGet packages and third-party libraries. The SDK handles packaging and loading automatically: add a `PackageReference`, build, and the library is included in your mod archive.

> [!NOTE]
> This article covers external (non-mod) dependencies. For mod-to-mod dependencies, see the [Modding Framework](xref:sparkitect.core.modding-framework). For optional mod integration, see [Optional Dependencies](xref:sparkitect.core.optional-dependencies).

## Adding a Dependency

Reference NuGet packages in your project file as you would in any .NET project:

```xml
<Project Sdk="Sparkitect.Sdk/1.0.0">
  <PropertyGroup>
    <ModId>inventory_extras</ModId>
    <ModVersion>1.0.0</ModVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>
```

When you build, the SDK detects your dependencies (including transitive ones), bundles them into the `/lib` directory of your mod archive, and the engine loads them before your mod assembly at runtime.

No additional configuration is needed for typical usage.

## Manual Control

For cases where auto-detection is insufficient (e.g. dynamically loaded DLLs the SDK cannot discover), you can specify assemblies explicitly:

```xml
<PropertyGroup>
  <ModRequiredAssemblies>MyNativeWrapper.dll;SomeOtherLib.dll</ModRequiredAssemblies>
</PropertyGroup>
```

Manually specified assemblies are combined with auto-detected ones. To disable auto-detection entirely:

```xml
<PropertyGroup>
  <ModAutoDetectDependencies>false</ModAutoDetectDependencies>
</PropertyGroup>
```

## How It Works

At build time, the SDK's `ParseDependencyFile` task reads your project's `.deps.json` to resolve the full dependency tree. It excludes system libraries, the Sparkitect SDK, and any assemblies belonging to referenced mods. The resulting list is written into the mod manifest and the corresponding DLLs are packaged under `/lib` in the archive.

At runtime, the engine loads everything in `/lib` into your mod's load context before loading the mod assembly itself.
