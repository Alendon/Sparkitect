---
uid: articles.core.mod-dependencies
title: Managing External Dependencies
description: How to manage and include external dependencies in Sparkitect mods
---

# Managing External Dependencies in Sparkitect Mods

## Overview

Sparkitect now supports loading non-mod DLLs from mod archives. This allows mods to include and use third-party libraries without requiring them to be installed separately.

## How It Works

When you build your mod, Sparkitect automatically:

1. Detects direct dependencies of your mod
2. Includes these dependencies in a `/lib` directory within your mod archive
3. Loads these dependencies when your mod is loaded

## Configuration

### Automatic Dependency Detection

By default, dependency detection is enabled. This means Sparkitect will automatically find and include direct dependencies of your mod (excluding system libraries and the Sparkitect SDK itself).

You can control this behavior with the `SparkitectAutoDetectDependencies` property:

```xml
<PropertyGroup>
    <!-- Disable automatic dependency detection -->
    <SparkitectAutoDetectDependencies>false</SparkitectAutoDetectDependencies>
</PropertyGroup>
```

### Manual Dependency Specification

You can manually specify dependencies to include using the `SparkitectRequiredAssemblies` property:

```xml
<PropertyGroup>
    <!-- Add specific dependencies -->
    <SparkitectRequiredAssemblies>Newtonsoft.Json.dll;YourLibrary.dll</SparkitectRequiredAssemblies>
</PropertyGroup>
```

When both automatic detection and manual specification are used, the lists are combined.

## Best Practices

1. **Only Include Direct Dependencies**: Transitive dependencies (dependencies of your dependencies) should generally not be included, as they'll be loaded automatically.

2. **Use NuGet Packages**: When possible, reference libraries via NuGet packages rather than including DLLs directly in your project.

3. **Test Your Mod**: Always test that your mod loads and functions correctly with its dependencies.

4. **Version Conflicts**: Be aware of potential version conflicts if multiple mods include the same library. Consider using assembly binding redirects if necessary.

## Example Project File

```xml
<Project Sdk="Sparkitect.Sdk/1.0.12">
  <PropertyGroup>
    <ModIdentifier>mycompany.mymod</ModIdentifier>
    <ModName>My Amazing Mod</ModName>
    <ModDescription>This mod does amazing things</ModDescription>
    <ModVersion>1.0.0</ModVersion>
    <ModAuthor>YourName</ModAuthor>
    
    <!-- Optional: Manually specify additional dependencies -->
    <SparkitectRequiredAssemblies>SpecialLibrary.dll</SparkitectRequiredAssemblies>
    
    <!-- Optional: Control automatic dependency detection -->
    <SparkitectAutoDetectDependencies>true</SparkitectAutoDetectDependencies>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Reference NuGet packages normally -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>
```