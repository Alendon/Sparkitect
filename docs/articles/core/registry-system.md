---
uid: sparkitect.core.registry-system
title: Registry System
description: Tracking and managing game objects using attribute-based and resource-file registration with a three-level identification hierarchy
---

# Registry System

Registries let mods declare game objects (items, blocks, states, shader modules, etc.) that the engine collects and makes available at runtime. Each registry owns a category of objects and maps them through a three-level identification system.

Registration is declarative: you annotate a static member or class with a generated attribute, and the engine handles discovery and processing during state transitions. A generated ID framework gives you type-safe access to all registered identifications.

## Registering Objects

The most common interaction with the registry system is registering objects from your mod. Each registry generates a nested attribute you apply to static members or classes.

### Value Registration (Property or Method)

When a registry defines a method like `RegisterValue(Identification, string)`, a corresponding attribute is generated. Both static properties and static methods are valid providers for the same registry method:

```csharp
// Property provider: simple static value
[DummyRegistry.RegisterValue("greeting")]
public static string Greeting => "Hello World";

// Method provider: can accept DI-resolved parameters
[DummyRegistry.RegisterValue("player_count")]
public static string PlayerCount(IGameConfig config) => $"Players: {config.MaxPlayers}";
```

The attribute takes a snake_case identifier that becomes the object's item ID. The static member provides the data to register.

Method providers can declare parameters that are resolved from the DI container at registration time. Non-nullable parameters throw if the dependency is missing; nullable parameters resolve to null when unavailable.

### Type Registration

Registries like [`StateRegistry`](xref:Sparkitect.GameState.StateRegistry) and [`ModuleRegistry`](xref:Sparkitect.GameState.ModuleRegistry) register types rather than values. The attribute goes on the class itself:

```csharp
[StateRegistry.RegisterState("sample")]
public partial class SampleEntryState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.MyMod.Sample;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.MyMod.Sample];
}
```

The generator embeds the type reference directly in the registration call (`registry.RegisterState<SampleEntryState>(id)`), so the registered class must satisfy whatever generic constraints the registry method declares.

### Resource File Registration

Registry methods that take only an [`Identification`](xref:Sparkitect.Modding.Identification) parameter (no value or generic type) accept entries from `.sparkres.yaml` files. When combined with [`[UseResourceFile]`](xref:Sparkitect.Modding.UseResourceFileAttribute) on the registry class, each entry maps to resource files on disk:

```yaml
# pong.sparkres.yaml
Sparkitect.Graphics.Vulkan.ShaderModuleRegistry.RegisterShaderModule:
  - pong: "pong.spv"
```

The top-level key is the fully qualified registry class and method name. Each list entry maps a snake_case identifier to its resource file(s). For registries with a single `[UseResourceFile(Primary = true)]` slot, a plain string value suffices. For multiple file slots, use a dictionary:

```yaml
MyMod.TextureRegistry.RegisterTexture:
  - stone:
      albedo: "stone_albedo.png"
      normal: "stone_normal.png"
```

Resource files are resolved from the registry's `ResourceFolder` during processing.

## Type-Safe ID Access

The source generator produces a type-safe ID framework so you never need to construct [`Identification`](xref:Sparkitect.Modding.Identification) values by hand. For every registry category, a static container class is generated in the `Sparkitect.Modding.IDs` namespace:

```csharp
using Sparkitect.CompilerGenerated.IdExtensions;
using MinimalSampleMod.CompilerGenerated.IdExtensions;

// Access IDs through: {Category}ID.{ModName}.{EntryPascalCase}
Identification rootState = StateID.Sparkitect.Root;
Identification greeting  = DummyID.MinimalSampleMod.Hello1;
```

The structure is three tiers:

1. **Category container**: `StateID`, `DummyID`, `ShaderModuleID`, etc. (one per registry category)
2. **Mod extension**: Each mod that registers into a category gets a property (e.g., `.Sparkitect`, `.MinimalSampleMod`)
3. **Entry properties**: Each registered identifier becomes a property returning its [`Identification`](xref:Sparkitect.Modding.Identification) (e.g., `.Root`, `.Hello1`)

The property names are PascalCase conversions of the snake_case identifiers. Import the `CompilerGenerated.IdExtensions` namespace from each relevant mod assembly to bring the extensions into scope.

## Defining a Registry

Registries are partial classes implementing [`IRegistry`](xref:Sparkitect.Modding.IRegistry), annotated with [`[Registry]`](xref:Sparkitect.Modding.RegistryAttribute), and instantiated through DI:

```csharp
[Registry(Identifier = "items")]
public partial class ItemRegistry(IItemManager manager) : IRegistry
{
    public static string Identifier => "items";

    [RegistryMethod]
    public void RegisterItem(Identification id, ItemData data)
    {
        manager.AddItem(id, data);
    }

    public void Unregister(Identification id)
    {
        manager.RemoveItem(id);
    }
}
```

Key elements:

- **`[Registry(Identifier = "...")]`**: Marks the class and sets the category identifier (must be snake_case, globally unique).
- **`partial class`**: Required for source generator output (nested attributes, factory, configurator).
- **`static string Identifier`**: Must match the attribute value. Satisfies the [`IRegistry`](xref:Sparkitect.Modding.IRegistry) contract.
- **Constructor injection**: Dependencies come from the current state's DI container.
- **`Unregister(Identification)`**: Required by [`IRegistryBase`](xref:Sparkitect.Modding.IRegistryBase) for cleanup during teardown.

### Registry Methods

Methods marked with [`[RegistryMethod]`](xref:Sparkitect.Modding.RegistryMethodAttribute) define what can be registered. The source generator creates a nested attribute class for each one.

There are three kinds of registry methods, distinguished by their signature:

```csharp
// Value registration: providers are static properties or methods returning the data type
[RegistryMethod]
public void RegisterItem(Identification id, ItemData data) { }

// Type registration: providers are classes matching the generic constraint
[RegistryMethod]
public void RegisterState<T>(Identification id) where T : class, IStateDescriptor { }

// ID-only registration: no value or type parameter, entries typically come from .sparkres.yaml files
[RegistryMethod]
public void RegisterShaderModule(Identification id) { }
```

All signatures require [`Identification`](xref:Sparkitect.Modding.Identification) as the first parameter. Methods that don't meet these constraints are silently skipped by the generator; analyzers report shape issues.

### Generated Attribute Shape

For a value registry method `RegisterItem(Identification, ItemData)`, the generator produces a nested attribute class:

```csharp
public class RegisterItemAttribute([SnakeCase] string identifier)
    : Attribute, IRegisterMarker
{
    public bool GroupAtRoot { get; set; } = false;
    // Resource file properties appear here if [UseResourceFile] is on the registry
}
```

The [`IRegisterMarker`](xref:Sparkitect.Modding.IRegisterMarker) interface is how the engine discovers registration attributes across loaded mods. `GroupAtRoot` controls whether the identifier is scoped to the declaring type's parent or the mod root.

The same attribute shape is generated for type registration methods. The difference is in how it is applied: on a class (type registration) vs. on a static property or method (value registration).

### Generated Registration Processing

For each mod that uses registration attributes, the generator produces a `Registrations<TRegistry>` subclass that handles the actual processing. The generated code for each entry:

1. Registers the identifier with [`IIdentificationManager`](xref:Sparkitect.Modding.IIdentificationManager) to obtain the [`Identification`](xref:Sparkitect.Modding.Identification)
2. Resolves the provider: reads the static property, calls the method (with DI parameters resolved from the container), or references the type directly
3. Calls the registry method with the ID and resolved value/type

The static [`Identification`](xref:Sparkitect.Modding.Identification) fields on this class are what the ID framework properties delegate to.

### Resource File Slots

Registries that work with external files (shaders, textures, configs) declare their file schema with [`[UseResourceFile]`](xref:Sparkitect.Modding.UseResourceFileAttribute):

```csharp
[Registry(Identifier = "shader_module")]
[UseResourceFile(Key = "module", Required = true, Primary = true)]
public partial class ShaderModuleRegistry(IShaderManager shaderManager) : IRegistry
{
    public static string Identifier => "shader_module";
    public static string ResourceFolder => "shaders";

    [RegistryMethod]
    public void RegisterShaderModule(Identification id)
    {
        shaderManager.RegisterModule(id);
    }

    public void Unregister(Identification id)
    {
        shaderManager.UnregisterModule(id);
    }
}
```

`[UseResourceFile]` properties:
- **`Key`**: The file slot identifier used in `.sparkres.yaml` entries and as a generated attribute property (PascalCase + `File`, e.g., `ModuleFile`).
- **`Required`**: Whether every entry must provide this file.
- **`Primary`**: Marks the default slot for single-file shorthand in YAML.

A generic variant `[UseResourceFile<TResource>]` is available when the resource file has a typed loader implementing `IResourceFile`.

The `ResourceFolder` static property (optional on [`IRegistry`](xref:Sparkitect.Modding.IRegistry)) tells the [`IResourceManager`](xref:Sparkitect.Modding.IResourceManager) where to look for files during processing.

## Identifier System

All registered objects use a three-level identification hierarchy packed into an 8-byte [`Identification`](xref:Sparkitect.Modding.Identification) struct:

| Level | Field | Type | Example |
|-------|-------|------|---------|
| Mod | `ModId` | `ushort` | `"my_mod"` -> 2 |
| Category | `CategoryId` | `ushort` | `"items"` -> 3 |
| Item | `ItemId` | `uint` | `"iron_sword"` -> 42 |

The [`IIdentificationManager`](xref:Sparkitect.Modding.IIdentificationManager) maintains bidirectional mappings between string identifiers (stable, human-readable) and their numeric representations (compact, used at runtime). String form uses colon-separated notation: `my_mod:items:iron_sword`.

Registration attributes only require the item-level key (e.g., `"iron_sword"`). The mod ID and category ID are determined automatically from the declaring mod and target registry.

## Registry Lifecycle

Registries are state-triggered: they are added, processed, and torn down by transition functions rather than a global initialization pass. This makes registration composable across state boundaries.

```csharp
[TransitionFunction("init_items")]
[OnCreateScheduling]
private static void InitItems(IRegistryManager rm)
{
    rm.AddRegistry<ItemRegistry>();
    rm.ProcessAllMissing<ItemRegistry>();
}

[TransitionFunction("cleanup_items")]
[OnDestroyScheduling]
private static void CleanupItems(IRegistryManager rm)
{
    rm.UnregisterAllRemaining<ItemRegistry>();
}
```

[`IRegistryManager`](xref:Sparkitect.Modding.IRegistryManager) methods:

| Method | Purpose |
|--------|---------|
| `AddRegistry<T>()` | Registers the category with the identification system and makes it available for processing |
| `ProcessAllMissing<T>()` | Discovers and processes registration attributes from all loaded mods not yet processed for this registry |
| `ProcessRegistry<T>(modIds)` | Processes only the specified mods (for targeted updates when new mods load) |
| `UnregisterAllRemaining<T>()` | Cleans up all registered objects and removes tracked mods |

The lifecycle within a state transition:
1. `AddRegistry<T>()` registers the category identifier and resource folder
2. `ProcessAllMissing<T>()` scans loaded mods for [`IRegisterMarker`](xref:Sparkitect.Modding.IRegisterMarker) attributes targeting this registry, resolves data from providers or resource files, and calls the registry method for each entry
3. When the state is destroyed, `UnregisterAllRemaining<T>()` calls `Unregister(Identification)` for every tracked object and removes their identification mappings
