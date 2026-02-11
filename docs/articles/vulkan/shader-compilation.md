---
uid: sparkitect.vulkan.shader-compilation
title: Shader Compilation
description: Slang shader compilation workflow, YAML resource registration, and runtime shader access
---

# Shader Compilation

Sparkitect uses [Slang](https://shader-slang.com/) for shader compilation. Slang is a modern shading language that compiles to SPIR-V, providing advanced features like generics, interfaces, and automatic differentiation while maintaining compatibility with existing HLSL code.

## Workflow Overview

The shader workflow has four stages:

1. **Authoring**: Write `.slang` shader files
2. **Compilation**: MSBuild automatically compiles to `.spv` during build
3. **Registration**: YAML resource files register shaders with the engine
4. **Runtime**: Access compiled shaders through `IShaderManager`

```
.slang source  -->  Build  -->  .spv binary  -->  YAML registration  -->  Runtime access
```

## Project Setup

### Adding Shaders to Your Project

Reference your Slang shader files in your mod's `.csproj` using the `SlangShader` item type:

```xml
<ItemGroup>
    <SlangShader Include="Shaders\myshader.slang" />
</ItemGroup>
```

Multiple shaders:

```xml
<ItemGroup>
    <SlangShader Include="Shaders\render.slang" />
    <SlangShader Include="Shaders\compute.slang" />
    <SlangShader Include="Shaders\postprocess.slang" />
</ItemGroup>
```

### Automatic Compilation

The Sparkitect SDK automatically compiles Slang shaders to SPIR-V during build:

- **Input**: `Shaders/myshader.slang`
- **Output**: `Resources/shaders/myshader.spv`

The build system:
- Detects changes to source files (incremental compilation)
- Creates the output directory automatically
- Uses the Slang compiler from Vulkan SDK or PATH
- Supports custom include paths via `SlangIncludePaths` property

### Custom Compiler Settings

Override default settings in your project file:

```xml
<PropertyGroup>
    <!-- Custom compiler path (defaults to VULKAN_SDK or PATH) -->
    <SlangCompilerPath>/custom/path/to/slangc</SlangCompilerPath>

    <!-- Custom output directory (defaults to Resources/shaders) -->
    <SlangShaderOutputPath>$(MSBuildProjectDirectory)/Build/Shaders</SlangShaderOutputPath>

    <!-- Additional include paths (defaults to Shaders/) -->
    <SlangIncludePaths>$(MSBuildProjectDirectory)/Shaders;$(MSBuildProjectDirectory)/Common</SlangIncludePaths>
</PropertyGroup>
```

Per-shader compiler options:

```xml
<ItemGroup>
    <SlangShader Include="Shaders\myshader.slang" AdditionalOptions="-O3" />
</ItemGroup>
```

## YAML Resource Registration

After compilation, register your shader modules using a `.sparkres.yaml` file. This connects the compiled `.spv` files to the engine's shader registry.

### File Location

Place your resource file in the mod root directory, named `{modname}.sparkres.yaml`:

```
MyMod/
  Shaders/
    pong.slang          # Source shader
  Resources/
    shaders/
      pong.spv          # Compiled output
  pong.sparkres.yaml    # Resource registration
  MyMod.csproj
```

### Registration Format

```yaml
Sparkitect.Graphics.Vulkan.ShaderModuleRegistry.RegisterShaderModule:
  - pong: "pong.spv"
```

The format is:
```yaml
{RegistryClass}.{RegistryMethod}:
  - {key}: "{resource_file}"
```

For multiple shaders:

```yaml
Sparkitect.Graphics.Vulkan.ShaderModuleRegistry.RegisterShaderModule:
  - vertex_shader: "vertex.spv"
  - fragment_shader: "fragment.spv"
  - compute_shader: "compute.spv"
```

### How It Works

1. The engine discovers `.sparkres.yaml` files in loaded mods
2. Each entry calls the specified registry method
3. The key becomes the shader's `Identification` (combined with mod ID)
4. The resource file path is relative to `Resources/shaders/`

## ShaderModuleRegistry

The `ShaderModuleRegistry` manages shader module registration:

```csharp
[Registry(Identifier = "shader_module")]
[UseResourceFile(Key = "module", Required = true, Primary = true)]
public partial class ShaderModuleRegistry(IShaderManager shaderManager) : IRegistry
{
    [RegistryMethod]
    public void RegisterShaderModule(Identification id)
    {
        shaderManager.RegisterModule(id);
    }

    public static string ResourceFolder => "shaders";
}
```

The registry:
- Is automatically discovered and configured by the engine
- Reads shader binaries from `Resources/shaders/`
- Associates each shader with an `Identification` for runtime lookup

> **Note**: The simplified listing above omits `Unregister(Identification id)` for dynamic shader unloading and the `Identifier` static property (value: `"shader_module"`). Both are available on the full `ShaderModuleRegistry` type.

## Runtime Shader Access

Access registered shaders through `IShaderManager`:

```csharp
[StateService<IMyRenderer, MyRenderModule>]
public class MyRenderer : IMyRenderer
{
    public required IShaderManager ShaderManager { private get; init; }
    public required IVulkanContext VulkanContext { private get; init; }

    public void CreatePipeline()
    {
        // Look up shader by generated ID
        if (!ShaderManager.TryGetRegisteredShaderModule(ShaderModuleID.MyMod.MyShader, out var shaderModule))
            throw new InvalidOperationException("Shader not registered");

        // Use in pipeline creation
        var stageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = shaderModule.Handle,
            PName = entryPointPtr  // e.g., "main"
        };

        // Create pipeline with shader stage...
    }
}
```

### Generated Shader IDs

The YAML registration generates `Identification` constants accessible through the `ShaderModuleID` class:

```csharp
// Generated from YAML registration
ShaderModuleID.MyMod.VertexShader
ShaderModuleID.MyMod.FragmentShader
ShaderModuleID.MyMod.ComputeShader
```

> **Note**: These are implemented internally using C# extension methods on the `ShaderModuleID` static class. The access syntax above is the intended API surface.

## Complete Example: Pong Shader

Here's the complete workflow from the Pong sample:

**1. Shader source** (`Shaders/pong.slang`):
```slang
// Compute shader that renders the pong game
[shader("compute")]
[numthreads(8, 8, 1)]
void computeMain(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    // Shader implementation...
}
```

> **Note**: Slang renames entry points to `main` in the compiled SPIR-V output. This is why the runtime uses `"main"u8` as the entry point name even though the source uses `computeMain`. You can name your entry point descriptively in Slang source without affecting runtime behavior.

**2. Project file** (`PongMod.csproj`):
```xml
<ItemGroup>
    <SlangShader Include="Shaders\pong.slang" />
</ItemGroup>
```

**3. Resource registration** (`pong.sparkres.yaml`):
```yaml
Sparkitect.Graphics.Vulkan.ShaderModuleRegistry.RegisterShaderModule:
  - pong: "pong.spv"
```

**4. Runtime usage** (`PongRuntimeService.cs`):
```csharp
// Get shader module
if (!ShaderManager.TryGetRegisteredShaderModule(ShaderModuleID.PongMod.Pong, out var shaderModule))
    throw new InvalidOperationException("Pong shader not registered");

// Create compute pipeline with shader
var stageInfo = new PipelineShaderStageCreateInfo
{
    SType = StructureType.PipelineShaderStageCreateInfo,
    Stage = ShaderStageFlags.ComputeBit,
    Module = shaderModule.Handle,
    PName = entryPointPtr
};

var pipelineResult = VulkanContext.CreateComputePipeline(computePipelineInfo);
```

## Troubleshooting

### Shader Not Found at Runtime

1. **Check YAML registration**: Ensure entry exists in `.sparkres.yaml`
2. **Verify build output**: Confirm `.spv` file exists in `Resources/shaders/`
3. **Check mod loading**: Ensure your mod is loaded before accessing shader

### Compilation Fails

1. **Slang compiler missing**: Install Vulkan SDK or add `slangc` to PATH
2. **Syntax errors**: Check Slang documentation for language differences from HLSL
3. **Include path issues**: Add directories to `SlangIncludePaths` property

### SPIR-V Validation Errors

Use Vulkan validation layers during development to catch shader issues:
- Enable validation in Vulkan instance creation
- Check debug output for SPIR-V validation messages

## Integration with Other Systems

- **Vulkan Graphics**: Shader modules are used in pipeline creation ([details](xref:sparkitect.vulkan.vulkan-graphics))
- **Registry System**: ShaderModuleRegistry follows the registry pattern ([details](xref:sparkitect.core.registry-system))
- **Dependency Injection**: Access `IShaderManager` through DI ([details](xref:sparkitect.core.dependency-injection))

## Next Steps

- See [Vulkan Graphics](xref:sparkitect.vulkan.vulkan-graphics) for pipeline creation with shaders
- See [Registry System](xref:sparkitect.core.registry-system) for the general registry pattern
- Review `samples/PongMod/` for a complete shader usage example
