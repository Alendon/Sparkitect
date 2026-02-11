---
uid: sparkitect.tooling.sdk-guide
title: SDK Guide
description: How to use the Sparkitect SDK to create mods
---

# Sparkitect SDK Guide

This guide explains how to use the Sparkitect SDK to create mods.

## Project Setup

### Creating a New Mod Project

1. Create a new .NET project
2. Reference the Sparkitect SDK:

```xml
<Project Sdk="Sparkitect.Sdk/0.1.0">

    <PropertyGroup>
        <ModId>your_mod_id</ModId>
        <ModName>Your Mod Name</ModName>
        <ModVersion>1.0.0</ModVersion>
        <ModAuthor>Your Name</ModAuthor>
        <ModDescription>What your mod does</ModDescription>
        <IsRootMod>true</IsRootMod>

        <TargetFramework>net10.0</TargetFramework>
        <GenerateDependencyFile>true</GenerateDependencyFile>
        <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    </PropertyGroup>

    <!-- Mod dependencies -->
    <ItemGroup>
        <ModProjectDependency Include="path/to/Sparkitect.csproj" />
    </ItemGroup>

</Project>
```

> [!NOTE]
> `GenerateDependencyFile` is critical for automatic dependency detection by the SDK.
> `EmitCompilerGeneratedFiles` is optional but useful for inspecting source-generated code during debugging.

### Required Properties

| Property | Description | Example |
|----------|-------------|---------|
| ModId | Unique identifier (snake_case) | `my_cool_mod` |
| ModName | Display name | `My Cool Mod` |
| ModVersion | Semantic version | `1.0.0` |
| ModAuthor | Author name(s), separated by `;` or `,` | `YourName` or `Name1;Name2` or `Name1,Name2` |
| ModDescription | Short description (recommended but not currently enforced by build validation) | `Adds cool features` |
| IsRootMod | Can be loaded directly | `true` or `false` |

### Optional Properties

| Property | Default | Description |
|----------|---------|-------------|
| ModPackageEnabled | true | Enable .sparkmod generation |
| ModAutoDetectDependencies | true | Auto-detect DLL dependencies (canonical property for controlling dependency auto-detection) |
| ModResourceDirectory | Resources/ | Resource folder path |

## Mod Dependencies

### Declaring Dependencies

Use `ModProjectDependency` for in-solution mod references:

```xml
<ItemGroup>
    <!-- Required dependency -->
    <ModProjectDependency Include="../OtherMod/OtherMod.csproj" />

    <!-- Optional dependency -->
    <ModProjectDependency Include="../OptionalMod/OptionalMod.csproj" IsOptional="true" />
</ItemGroup>
```

### Version Ranges

By default, the SDK infers a compatible version range (`^x.y.z`) from the referenced project's ModVersion.

To specify an explicit range:

```xml
<ModProjectDependency Include="../OtherMod/OtherMod.csproj" VersionRange=">=1.0.0 <2.0.0" />
```

### Declaring Incompatibilities

> [!NOTE]
> The `ModIncompatibility` MSBuild item type is **planned but not yet implemented**. The runtime `ModManager` supports incompatibility validation, but the SDK build pipeline does not yet produce incompatibility entries in the manifest. This section describes the intended usage for a future release.

Use `ModIncompatibility` to declare mods that cannot be loaded together:

```xml
<ItemGroup>
    <ModIncompatibility Include="conflicting_mod_id" VersionRange="*" />
</ItemGroup>
```

## Build Output

When you build your mod project, the SDK:

1. Validates required properties
2. Detects DLL dependencies
3. Generates `manifest.json` in `obj/` directory
4. Creates `.sparkmod` archive in `bin/` directory

### Output Locations

| Artifact | Location |
|----------|----------|
| manifest.json | `obj/Debug/net10.0/manifest.json` |
| .sparkmod | `bin/Debug/net10.0/your_mod_id-1.0.0.sparkmod` |

## Running Mods

### Using launchSettings.json (Recommended)

Create `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "Run My Mod": {
      "commandName": "Executable",
      "executablePath": "$(SolutionDir)/src/Sparkitect/bin/$(Configuration)/net10.0/Sparkitect",
      "commandLineArgs": "-addModDirs=$(ProjectDir)/bin/$(Configuration)/net10.0",
      "workingDirectory": "$(ProjectDir)/.run"
    }
  }
}
```

This launches the Sparkitect engine with your mod loaded. The `$(Configuration)` variable automatically switches between Debug/Release builds.

> [!NOTE]
> The MSBuild variables (`$(SolutionDir)`, `$(ProjectDir)`, `$(Configuration)`) are expanded by IDEs such as Rider and Visual Studio. They will **not** work when running from the command line directly. For CLI usage, substitute the actual paths.

In Rider/VS, select the profile and run.

### From Command Line

Build and run the Sparkitect engine with your mod:

```bash
dotnet run --project path/to/Sparkitect.csproj -- -addModDirs=path/to/your/mod/bin/Debug/net10.0
```

### Multiple Mod Directories

Separate multiple paths with semicolons using the `key=value` format:

```bash
-addModDirs=path/to/mod1;path/to/mod2;path/to/mod3
```

## Optional Dependencies

### Runtime Checking

Use `IGameStateManager.IsModLoaded()` to check if an optional dependency is present:

```csharp
if (gameStateManager.IsModLoaded("optional_mod_id"))
{
    // Use optional mod features
}
```

### Isolated Integration Pattern

Create a separate class for optional mod integration:

```csharp
// Only instantiate if optional mod is loaded
[OptionalModDependent("optional_mod_id")]
public class OptionalModIntegration
{
    // This class can safely reference types from optional_mod_id
}
```

See the [Mod Specification](xref:sparkitect.tooling.mod-specification) for the manifest format that describes optional relationships.

## Troubleshooting

### Common Build Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "ModId must be set" | Missing ModId property | Add `<ModId>` to PropertyGroup |
| "Could not find ModProjectDependency" | Wrong path | Check relative path to .csproj |
| "does not have a ModId property" | Referenced project not a mod | Ensure referenced project uses SDK |

### Manifest Issues

If manifest.json appears in wrong location, ensure you're using the latest SDK version.
