---
uid: sparkitect.core.optional-dependencies
title: Optional Dependencies
description: Safely integrating with mods that may not be present at runtime
---

# Optional Dependencies

Optional dependencies allow your mod to integrate with other mods when they're present, without requiring them to be installed.

## Overview

When building mods for Sparkitect, you may want to enhance your mod's functionality when certain other mods are available. For example:
- A HUD mod that shows extra stats when a statistics mod is installed
- A game mode mod that adds special items when an expansion mod is present
- A utility mod that provides enhanced debugging when a developer tools mod is available

Optional dependencies let you:
1. Declare that your mod CAN use another mod (but doesn't require it)
2. Check at runtime if that mod is loaded
3. Safely call into the optional mod's code without crashing when it's absent

**Good news:** The optional dependency system is designed to be safe by default. The recommended patterns (DI integration, isolated classes) work without requiring deep knowledge of .NET internals. The optional dependency analyzer catches most mistakes at compile time.

## CLR Lazy Loading: Background

> **Note:** This section explains WHY the system works the way it does. You don't need to memorize this -- just follow the patterns and let the analyzer catch mistakes. This background is useful if you're curious or troubleshooting.

### How the JIT Compiler Works

The .NET JIT (Just-In-Time) compiler compiles methods to native code when they're first called. Here's the critical detail: **the JIT compiles the entire method body at once**, which means it resolves all type references in that method.

```csharp
// DANGEROUS: This method will crash even if the if-block is never entered
public void MaybeUseOptionalMod(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        // When this method is JIT compiled, the runtime IMMEDIATELY tries
        // to load the StatsModApi type - before any code executes!
        var api = new StatsModApi();  // TypeLoadException here!
        api.ShowStats();
    }
}
```

When `MaybeUseOptionalMod` is called:
1. JIT compiler starts compiling the method
2. It sees `StatsModApi` type reference
3. It tries to load the assembly containing `StatsModApi`
4. If stats_mod isn't installed, **TypeLoadException** - the method never even starts executing

### The Solution: Separate Methods

The fix is simple: put the guard check and the type usage in **separate methods**:

```csharp
// SAFE: Guard check and type usage are in separate methods
public void MaybeUseOptionalMod(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        UseStatsModInternal();  // Only called if mod is loaded
    }
}

private void UseStatsModInternal()
{
    // This method is only JIT compiled when called
    // By that point, we've already verified the mod is loaded
    var api = new StatsModApi();
    api.ShowStats();
}
```

Now when `MaybeUseOptionalMod` is called:
1. JIT compiles `MaybeUseOptionalMod` - no problem, it only references `UseStatsModInternal`
2. The IsModLoaded check runs
3. If false, we return - `UseStatsModInternal` is never called, never JIT compiled
4. If true, `UseStatsModInternal` is called, JIT compiled, and `StatsModApi` loads successfully

## Manifest Declaration

Declare optional dependencies in your mod's csproj file:

```xml
<ItemGroup>
  <!-- Required dependency (default behavior) -->
  <ModProjectDependency Include="..\CoreMod\CoreMod.csproj" />

  <!-- Optional dependency - mod will load even if this isn't present -->
  <ModProjectDependency Include="..\StatsMod\StatsMod.csproj" IsOptional="true" />
</ItemGroup>
```

The `IsOptional="true"` metadata tells the mod loader:
- Don't fail if this mod isn't installed
- Don't require this mod to be loaded before yours

## Runtime API

Check if an optional mod is loaded using `IGameStateManager.IsModLoaded`:

```csharp
public class MyMod
{
    private readonly IGameStateManager _gsm;

    public MyMod(IGameStateManager gsm)
    {
        _gsm = gsm;
    }

    public void Initialize()
    {
        // Always safe - just returns true/false
        if (_gsm.IsModLoaded("stats_mod"))
        {
            // Now safe to call code that uses stats_mod types
            InitializeStatsIntegration();
        }
    }

    private void InitializeStatsIntegration()
    {
        // Types from stats_mod can be safely used here
    }
}
```

## Isolation Pattern

For larger integrations, use the isolation pattern with dedicated integration classes:

### Step 1: Create an Integration Class

```csharp
using StatsMod;  // Types from the optional mod

[OptionalModDependent("stats_mod")]
public class StatsModIntegration
{
    private readonly StatsApi _api;

    public StatsModIntegration()
    {
        _api = new StatsApi();
    }

    public void RegisterCustomStats()
    {
        _api.RegisterStat("my_mod_score", () => GetCurrentScore());
    }

    private int GetCurrentScore() => /* ... */;
}
```

The `[OptionalModDependent("stats_mod")]` attribute:
- Documents that this class uses optional mod types
- Enables the optional dependency analyzer to validate correct usage
- Signals to readers that this class requires the mod to be loaded

### Step 2: Guard All Entry Points

```csharp
public class MyMod
{
    private readonly IGameStateManager _gsm;
    private StatsModIntegration? _statsIntegration;

    [ModLoadedGuard("stats_mod")]
    private void InitializeStatsIntegration()
    {
        _statsIntegration = new StatsModIntegration();
        _statsIntegration.RegisterCustomStats();
    }

    public void Initialize()
    {
        if (_gsm.IsModLoaded("stats_mod"))
        {
            InitializeStatsIntegration();
        }
    }
}
```

The `[ModLoadedGuard("stats_mod")]` attribute:
- Documents that this method is a **drawbridge** -- only called when the mod is loaded
- The drawbridge only lowers when there's a target on the other side
- Inside this method, you're responsible for ensuring code is safe (no analyzer validation inside)

> **The Drawbridge Pattern:** Think of `[ModLoadedGuard]` as a drawbridge. The analyzer puts rails everywhere else -- you can't accidentally reference optional mod types. But the drawbridge is the controlled entry point where you cross into optional mod territory. The guard check (`if (IsModLoaded(...))`) ensures the drawbridge only lowers when the destination exists.

The DI integration pattern below uses this same drawbridge pattern behind the scenes -- conditional registration is just a formalized `[ModLoadedGuard]`.

### Why Use Dedicated Classes?

1. **Clear boundaries**: All optional mod code is in one place
2. **Easier testing**: Test with/without the optional mod by including/excluding the integration class
3. **Analyzer-enforced**: The optional dependency analyzer enforces that optional types only appear in marked classes
4. **Self-documenting**: The attribute makes dependencies explicit

## DI Integration

For dependency injection scenarios, use conditional registration. This is the **recommended approach** -- it's the drawbridge pattern formalized into a clean structure.

> **Under the hood:** DI integration uses the exact same drawbridge pattern:
> 1. Entry point has the `if (IsModLoaded(...))` check
> 2. Separate `[ModLoadedGuard]` method contains the registration code (type references)
> 3. Entry point calls the guarded method
>
> This separation is required by CLR lazy loading -- the check and the type references must be in different methods.

### Interface (Safe)

```csharp
// This interface is in YOUR mod - no optional mod types
public interface IStatsIntegration
{
    void RegisterStats();
    void UpdateStats();
}
```

### Implementation (Isolated)

```csharp
using StatsMod;

[OptionalModDependent("stats_mod")]
public class StatsIntegration : IStatsIntegration
{
    private readonly StatsApi _api;

    public StatsIntegration(StatsApi api)
    {
        _api = api;
    }

    public void RegisterStats()
    {
        _api.RegisterStat("score", () => GetScore());
    }

    public void UpdateStats() => _api.Refresh();

    private int GetScore() => /* ... */;
}
```

### Conditional Registration

The key pattern is to check `IsModLoaded` in your configurator entrypoint, then call a `[ModLoadedGuard]` method that contains the actual registration using `Register<TServiceFactory>()`:

```csharp
// In your configurator entrypoint (pseudo-code showing the pattern):
// 1. Check if the optional mod is loaded
if (gsm.IsModLoaded("stats_mod"))
{
    RegisterStatsIntegration(builder);
}

// 2. Guarded method contains the type references
[ModLoadedGuard("stats_mod")]
private void RegisterStatsIntegration(ICoreContainerBuilder builder)
{
    // Register using the actual builder API
    builder.Register<StatsIntegrationServiceFactory>();
}
```

> [!NOTE]
> The exact registration mechanism depends on how your service factory is structured. Sparkitect's DI uses source-generated `[StateService]` configurators for most services. The pattern above illustrates the drawbridge principle -- the guard check and the type-referencing registration must be in separate methods.

Consumer code can then use `TryResolve`:

```csharp
public class MyFeature
{
    private readonly IStatsIntegration? _stats;

    public MyFeature(ICoreContainer container)
    {
        container.TryResolve(out _stats);  // null if stats_mod not loaded
    }

    public void Update()
    {
        _stats?.UpdateStats();  // Safe - null-conditional handles missing integration
    }
}
```

## Common Mistakes

> **Note:** The analyzer catches most of these at compile time -- there are rails everywhere. These examples show what can go wrong inside the drawbridge (inside `[ModLoadedGuard]` methods), where you're responsible for ensuring safety.

### Mistake 1: Type Reference in Same Method as Guard

```csharp
// WRONG - TypeLoadException even though check comes first!
public void BadExample(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        var api = new StatsModApi();  // Crash happens here
    }
}
```

**Fix:** Separate method for the type usage.

### Mistake 2: Field Type from Optional Mod

```csharp
// WRONG - Field types are resolved when the class is instantiated
public class BadExample
{
    private StatsModApi? _api;  // TypeLoadException when BadExample is created!

    public void Initialize(IGameStateManager gsm)
    {
        if (gsm.IsModLoaded("stats_mod"))
        {
            _api = new StatsModApi();
        }
    }
}
```

**Fix:** Use `object?` or keep typed fields in `[OptionalModDependent]` classes only.

```csharp
// CORRECT - Object field, cast when needed
public class GoodExample
{
    private object? _api;

    public void Initialize(IGameStateManager gsm)
    {
        if (gsm.IsModLoaded("stats_mod"))
        {
            InitializeStats();
        }
    }

    private void InitializeStats()
    {
        _api = new StatsModApi();
    }

    private void UseStats()
    {
        ((StatsModApi)_api!).DoThing();  // Cast inside guarded method
    }
}
```

### Mistake 3: Generic Type Parameters

```csharp
// WRONG - Generic constraint references optional type
public class BadExample
{
    public void Process<T>(T item) where T : IStatsProvider  // IStatsProvider from optional mod
    {
        // TypeLoadException when this method is called with ANY type
    }
}
```

**Fix:** Move generics to isolated class, or use non-generic interface.

### Mistake 4: Lambda/Closure Captures

```csharp
// WRONG - Lambda captures require type resolution
public void BadExample(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        Action action = () => new StatsModApi().DoThing();  // Captured in closure
        action();  // TypeLoadException
    }
}
```

**Fix:** Extract lambda to separate method in isolated class.

## Testing Optional Dependencies

Always test with the optional mod **absent**:

1. Build your mod
2. Remove the optional mod from the mods directory
3. Run the game
4. Verify your mod loads and runs without the optional functionality

Common test scenarios:
- Mod loads without optional mod present
- Optional features gracefully disabled
- No TypeLoadException in logs
- DI resolution returns null for optional interfaces

If you see `TypeLoadException` mentioning the optional mod's assembly, you have a type leakage problem. Check the common mistakes above.

## Summary

| Pattern | When to Use |
|---------|-------------|
| Separate methods | Always - guard check and type usage must be in different methods |
| `[OptionalModDependent]` | Classes that reference optional mod types |
| `[ModLoadedGuard]` | Methods that are entry points to optional mod code |
| Conditional DI registration | When integrating via dependency injection |
| `object?` fields | When you need to store optional mod instances in non-isolated classes |

Remember: the JIT compiler doesn't care about your if-statements. It compiles entire methods. Keep optional type references isolated.

## See Also

- [External Dependencies](xref:sparkitect.core.external-dependencies) - Managing NuGet and third-party dependencies
- [Modding Framework](xref:sparkitect.core.modding-framework) - Mod structure, loading, and lifecycle
