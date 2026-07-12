---
uid: sparkitect.settings
title: Settings Module
description: Layered, Identification-keyed configuration with ordered sources and typed accessors
---

# Settings Module

Every setting has exactly one declared default plus zero or more ordered sources; the effective value is the highest-priority source that has a value, else the default.

## Ordered Sources

The shipped source order is `CLI > user > engine-config > defaults`. Each source declares `OrderBefore`/`OrderAfter` edges against other source ids — the user source inserts itself between the CLI and engine-config sources rather than the engine hard-coding a fixed list.

## Declaring a Setting

```csharp
[SettingRegistry.RegisterSetting("vulkan_validation")]
[SettingAccessor("graphics", "VulkanValidation", "vulkan_validation")]
public static SettingDefinition<bool> VulkanValidation => new(true, CliOption: "vk-validation");
```

## Reading and Writing

```csharp
Setting<bool> validation = settingsManager.GetSetting(SettingID.Sparkitect.VulkanValidation);
bool current = validation.Value;
validation.Set(false);
IDisposable subscription = validation.OnChanged(newValue => Log.Information("Validation now {Value}", newValue));
```

Settings are primitives only (bool, int, float, enum, string) — there is no structured or reflection-driven binding. Compound values are modeled as separate primitive settings.

## See Also

- [Input](xref:sparkitect.input) for the action layer's Settings-backed default bindings
- [Registry System](xref:sparkitect.core.registry-system) for the generic registration pattern settings build on
