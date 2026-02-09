---
uid: sparkitect.core.registry-system
title: Registry System
description: Centralized tracking and management of game objects using a three-level identification hierarchy
---

# Registry System

The Registry System provides a mechanism for tracking and managing game objects and resources. It ensures that all components can consistently reference objects across the engine and mods.

Registries are state-triggered - they're added, processed, and unregistered by state functions rather than through a global initialization pass.

## Core Structure

### Registry Management Components

The Registry System includes:

- **RegistryManager**: Coordinates registry build and teardown at the request of state logic
- **IdentificationManager**: Handles the mapping between string IDs and numeric IDs
- **Registry Categories**: Type-specific registries for different kinds of game objects
- **Registry Entries**: The actual registered objects

### State-Triggered Registration

Registries are managed by state functions rather than a single global pass:

- **Module/State Creation** (`[OnCreateScheduling]` functions): Registries are added and processed when needed
- **Module/State Destruction** (`[OnDestroyScheduling]` functions): Registries are cleaned up and unregistered

State functions (marked with `[TransitionFunction]` + a scheduling attribute) control when registries are active, making registration deterministic and composable.

## Identifier System

### Three-Level Hierarchy

Identifiers use a three-level hierarchical structure:

- **ModId**: Identifies the source mod (string mapped to numeric ID)
- **CategoryId**: Identifies the registry category (string mapped to numeric ID)
- **ObjectId**: Identifies the specific object within the category (string mapped to numeric ID)

### Dual Representation

The identification system maintains dual representations:

- **String Identifiers**: Human-readable, stable across sessions (e.g., "core:block:stone")
- **Numeric Identifiers**: Compact, efficient runtime representation (e.g., 1:3:42)

The `IdentificationManager` handles bidirectional mapping between these representations.

## Defining Registries

Registries are DI-instantiated partial classes marked with `[Registry]`:

```csharp
[Registry(Identifier = "items")]
public partial class ItemRegistry(IItemManager manager) : IRegistry
{
    public static string Identifier => "items";

    // Define registration methods
    [RegistryMethod]
    public void RegisterItem(Identification id, ItemData data)
    {
        manager.AddItem(id, data);
    }

    // Required: cleanup method
    public void Unregister(Identification id)
    {
        manager.RemoveItem(id);
    }
}
```

**Registry Components:**
- `[Registry(Identifier = "...")]`: Marks the class as a registry with a unique category identifier
- `partial class`: Required for source generator extensions
- Constructor injection: Dependencies injected via DI
- `[RegistryMethod]`: Marks methods that can register objects
- `Unregister(Identification)`: Required method for cleanup

**Category Identifiers:**
- Must be globally unique across all registries
- Used to identify the registry category in the identification system
- Duplicate identifiers cause exceptions during processing

### Generated Registration Attributes

Source generators create registration attributes (marked `[CompilerGenerated]`) for each `[RegistryMethod]`:

```csharp
// Your registry method
[RegistryMethod]
public void RegisterItem(Identification id, ItemData data)
{
    // ...
}

// Generated attribute (CompilerGenerated):
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class RegisterItemAttribute : Attribute
{
    public string Key { get; }
    // ...
}
```

### Valid Registry Method Signatures

Methods marked with `[RegistryMethod]` must follow specific signature patterns for the source generator to create corresponding registration attributes.

**Required Parameters:**

| Parameter | Required | Purpose |
|-----------|----------|---------|
| `Identification id` | Yes | First parameter must be Identification |
| Additional data | Optional | Any additional parameters become attribute constructor parameters |

**Valid Signatures:**

```csharp
// Minimal: just identification
[RegistryMethod]
public void RegisterItem(Identification id) { }

// With single data parameter
[RegistryMethod]
public void RegisterItem(Identification id, ItemData data) { }

// With multiple data parameters
[RegistryMethod]
public void RegisterBlock(Identification id, BlockData data, BlockBehavior behavior) { }
```

**Invalid Signatures:**

```csharp
// Missing Identification as first parameter
[RegistryMethod]
public void RegisterItem(ItemData data) { } // ERROR

// Identification not first
[RegistryMethod]
public void RegisterItem(ItemData data, Identification id) { } // ERROR

// Non-public method
[RegistryMethod]
private void RegisterItem(Identification id) { } // ERROR
```

**Generated Attribute Usage:**

For a registry method with signature:
```csharp
[RegistryMethod]
public void RegisterItem(Identification id, ItemData data)
```

The generated attribute can be used as:
```csharp
// Key becomes part of Identification, data comes from property/method
[ItemRegistry.RegisterItem("iron_sword")]
public static ItemData IronSword => new ItemData { ... };
```

The attribute:
- Takes a string key as the constructor parameter
- Targets methods or properties that return the data type(s)
- The string key is combined with the mod identifier and category to form the full Identification

### Using Registration Attributes

Mods use the generated attributes to register objects:

```csharp
// In your mod code or state
[ItemRegistry.RegisterItem("iron_sword")]
public static ItemData IronSword => new ItemData
{
    Name = "Iron Sword",
    Damage = 10
};
```

The attribute-based registration:
- Works on static methods or properties
- The method/property provides the data to register
- The attribute parameter becomes part of the object's identification
- Registration happens when the registry is processed

### Managing Registries in State Functions

Registries are added and processed in state functions (static methods with `[TransitionFunction]` + scheduling attributes):

```csharp
public static class MyModule : IStateModule
{
    [TransitionFunction("add_registry")]
    [OnCreateScheduling]
    private static void AddRegistry(IRegistryManager registryManager)
    {
        // Add the registry
        registryManager.AddRegistry<ItemRegistry>();

        // Process registrations from loaded mods
        registryManager.ProcessAllMissing<ItemRegistry>();
    }

    [TransitionFunction("remove_registry")]
    [OnDestroyScheduling]
    private static void RemoveRegistry(IRegistryManager registryManager)
    {
        // Clean up all registered objects
        registryManager.UnregisterAllRemaining<ItemRegistry>();
    }
}
```

**Registry Lifecycle:**
1. `AddRegistry<T>()`: Makes the registry available
2. `ProcessAllMissing<T>()`: Discovers and processes registration attributes from all loaded mods
3. `Unregister AllRemaining<T>()`: Cleans up all registered objects
4. The registry is removed when the module/state is destroyed

## Integration with Other Systems

The Registry System integrates with:

- **Dependency Injection**: Registry classes are DI-instantiated and resolved from the current state's container
- **Game State**: Registries are managed by state functions (`[TransitionFunction]` + `[OnCreateScheduling]`/`[OnDestroyScheduling]`)
- **Modding**: Registration attributes are discovered across all loaded mods
- **Identification**: Registry categories use the identification system for object IDs

## Best Practices

### Registry Design

- Keep registries focused on a single object type
- Use descriptive category identifiers
- Inject dependencies via constructor for registry operations
- Implement `Unregister` to properly clean up objects

### Registration Timing

- Add and process registries in `[OnCreateScheduling]` functions
- Unregister in `[OnDestroyScheduling]` functions to ensure cleanup
- Process registries after loading mods but before using registered objects

### Object Identification

- Use the three-level identification hierarchy consistently
- Register objects with meaningful string keys
- The identification system automatically maps strings to numeric IDs

## Example: Complete Registry Flow

```csharp
// 1. Define the registry
[Registry(Identifier = "blocks")]
public partial class BlockRegistry(IBlockManager manager) : IRegistry
{
    [RegistryMethod]
    public void RegisterBlock(Identification id, BlockData data)
    {
        manager.AddBlock(id, data);
    }

    public void Unregister(Identification id)
    {
        manager.RemoveBlock(id);
    }
}

// 2. Register objects using generated attributes
[BlockRegistry.RegisterBlock("stone")]
public static BlockData Stone => new BlockData
{
    Hardness = 1.5f,
    Texture = "stone.png"
};

// 3. Manage registry lifecycle in state functions
[TransitionFunction("init_blocks")]
[OnCreateScheduling]
private static void InitBlocks(IRegistryManager rm)
{
    rm.AddRegistry<BlockRegistry>();
    rm.ProcessAllMissing<BlockRegistry>();
}

[TransitionFunction("cleanup_blocks")]
[OnDestroyScheduling]
private static void CleanupBlocks(IRegistryManager rm)
{
    rm.UnregisterAllRemaining<BlockRegistry>();
}
```
