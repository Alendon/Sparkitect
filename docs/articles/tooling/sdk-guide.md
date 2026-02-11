---
uid: sparkitect.tooling.sdk
title: Project SDK
description: How to use the Sparkitect SDK to create mods
---

# Project SDK

The Sparkitect SDK is an MSBuild SDK that handles the mod development workflow: project configuration, manifest generation, dependency resolution, and archive packaging.

## Project Setup

Create a `.csproj` file that references the SDK:

```xml
<Project Sdk="Sparkitect.Sdk/1.0.0">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ModId>my_first_mod</ModId>
        <ModName>My First Mod</ModName>
        <ModVersion>1.0.0</ModVersion>
        <ModAuthor>YourName</ModAuthor>
    </PropertyGroup>

    <ItemGroup>
        <ModProjectDependency Include="../../src/Sparkitect/Sparkitect.csproj" />
    </ItemGroup>

</Project>
```

The `ModProjectDependency` to the engine project is required for compilation and appears in the generated manifest as a dependency. Other mod dependencies are declared the same way (see [Mod Dependencies](#mod-dependencies)).

### Required Properties

| Property | Description | Example |
|----------|-------------|---------|
| ModId | Unique identifier (strict snake_case) | `my_cool_mod` |
| ModName | Display name | `My Cool Mod` |
| ModVersion | Semantic version | `1.0.0` |
| ModAuthor | Author name(s), separated by `;` or `,` | `YourName` or `Name1;Name2` |

### Optional Properties

| Property | Default | Description |
|----------|---------|-------------|
| ModDescription | (empty) | Short description of the mod |
| IsRootMod | false | Whether the mod can be loaded directly as an entry point |
| ModPackageEnabled | true | Enable `.sparkmod` archive generation |
| ModAutoDetectDependencies | true | Detect non-mod DLL dependencies from the build output |
| ModResourceDirectory | Resources/ | Resource folder included in the archive |

> [!TIP]
> Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to inspect source-generated code in the `obj/` directory during development.

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

Optional dependencies are fully supported by the DI system. Services annotated with [`[OptionalModDependent("mod_id")]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) are only registered when the dependency is loaded. See <xref:sparkitect.core.optional-dependencies> for details.

### Version Ranges

By default, the SDK infers a compatible version range (`^x.y.z`) from the referenced project's `ModVersion`. To specify an explicit range:

```xml
<ModProjectDependency Include="../OtherMod/OtherMod.csproj" VersionRange=">=1.0.0 <2.0.0" />
```

### Declaring Incompatibilities

> [!NOTE]
> `ModIncompatibility` is **planned but not yet implemented** in the SDK build pipeline. The runtime supports incompatibility validation, but the SDK does not yet produce incompatibility entries in the manifest.

```xml
<ItemGroup>
    <ModIncompatibility Include="conflicting_mod_id" VersionRange="*" />
</ItemGroup>
```

## Build Output

Building the project validates properties, detects DLL dependencies, generates a `manifest.json`, and creates a `.sparkmod` archive.

| Artifact | Location |
|----------|----------|
| manifest.json | `obj/<Configuration>/<TargetFramework>/manifest.json` |
| .sparkmod | `bin/<Configuration>/<TargetFramework>/<ModId>-<ModVersion>.sparkmod` |

See <xref:sparkitect.tooling.mod-specification> for the archive format and manifest schema.

## Running Mods

### Using launchSettings.json (Recommended)

Create `Properties/launchSettings.json` to run the engine with your mod from the IDE:

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

In Rider or Visual Studio, select the profile and run. The `$(Configuration)` variable switches between Debug and Release automatically.

> [!NOTE]
> The MSBuild variables (`$(SolutionDir)`, `$(ProjectDir)`, `$(Configuration)`) are expanded by IDEs. They do not work from the command line. For CLI usage, substitute the actual paths.

### From Command Line

```bash
dotnet run --project path/to/Sparkitect.csproj -- -addModDirs=path/to/your/mod/bin/Debug/net10.0
```

### General Deployment

Place `.sparkmod` archives in the engine's `mods/` directory. The engine discovers and loads mods from this directory at startup. Additional directories can be added with `-addModDirs`:

```bash
-addModDirs=path/to/mod1;path/to/mod2
```

## Troubleshooting

### Common Build Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "ModId must be set" | Missing ModId property | Add `<ModId>` to PropertyGroup |
| "Could not find ModProjectDependency" | Wrong path | Check relative path to .csproj |
| "does not have a ModId property" | Referenced project is not a mod | Ensure referenced project uses the SDK |
