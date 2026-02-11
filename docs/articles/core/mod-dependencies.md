---
uid: sparkitect.core.external-dependencies
title: Managing External Dependencies
description: How to manage and include external NuGet and third-party dependencies in Sparkitect mods
---

# Managing External Dependencies in Sparkitect Mods

## Overview

Sparkitect supports loading non-mod DLLs from mod archives. This allows mods to include and use third-party libraries (NuGet packages, standalone DLLs) without requiring them to be installed separately.

> [!NOTE]
> This article covers **external** dependencies (NuGet packages, third-party DLLs). For mod-to-mod dependencies, see the [Modding Framework](xref:sparkitect.core.modding-framework). For optional mod integration, see [Optional Dependencies](xref:sparkitect.core.optional-dependencies).

## How It Works

When you build your mod, the SDK automatically:

1. Detects direct dependencies of your mod
2. Includes these dependencies in a `/lib` directory within your mod archive
3. Loads these dependencies when your mod is loaded

## Configuration

### Automatic Dependency Detection

By default, dependency detection is enabled. This means the SDK will automatically find and include direct dependencies of your mod (excluding system libraries and the Sparkitect SDK itself).

You can control this behavior with the `ModAutoDetectDependencies` property:

```xml
<PropertyGroup>
    <!-- Disable automatic dependency detection -->
    <ModAutoDetectDependencies>false</ModAutoDetectDependencies>
</PropertyGroup>
```

> [!NOTE]
> The SDK property `SparkitectAutoDetectDependencies` also appears in `Properties.props` as a legacy default. The property actually checked during build is `ModAutoDetectDependencies`.

### Manual Dependency Specification

You can manually specify dependencies to include using the `ModRequiredAssemblies` property:

```xml
<PropertyGroup>
    <!-- Add specific dependencies -->
    <ModRequiredAssemblies>Newtonsoft.Json.dll;YourLibrary.dll</ModRequiredAssemblies>
</PropertyGroup>
```

When both automatic detection and manual specification are used, the lists are combined.

## Best Practices

1. **Only Include Direct Dependencies**: Transitive dependencies (dependencies of your dependencies) are handled by the SDK at build time when auto-detection is enabled. The SDK's `ParseDependencyFile` task resolves transitive dependencies from the `.deps.json` file and includes them in the archive's `/lib` folder.

2. **Use NuGet Packages**: When possible, reference libraries via NuGet packages rather than including DLLs directly in your project.

3. **Test Your Mod**: Always test that your mod loads and functions correctly with its dependencies.

4. **Version Conflicts**: Be aware of potential version conflicts if multiple mods include the same library. Consider using assembly binding redirects if necessary.

## Example Project File

```xml
<Project Sdk="Sparkitect.Sdk/0.1.0">
  <PropertyGroup>
    <ModId>mycompany.mymod</ModId>
    <ModName>My Amazing Mod</ModName>
    <ModDescription>This mod does amazing things</ModDescription>
    <ModVersion>1.0.0</ModVersion>
    <ModAuthor>YourName</ModAuthor>

    <!-- Optional: Manually specify additional dependencies -->
    <ModRequiredAssemblies>SpecialLibrary.dll</ModRequiredAssemblies>

    <!-- Optional: Control automatic dependency detection -->
    <ModAutoDetectDependencies>true</ModAutoDetectDependencies>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference NuGet packages normally -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>
```
