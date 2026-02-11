---
uid: sparkitect.core.optional-dependencies
title: Optional Dependencies
description: Safely integrating with mods that may not be present at runtime
---

# Optional Dependencies

Optional dependencies let your mod integrate with other mods when they are present, without requiring them to be installed. If the optional mod is missing, your mod still loads and runs normally.

## DI Integration (Recommended)

The most common use case is a <xref:Sparkitect.GameState.StateServiceAttribute`2> that depends on an optional mod's types. Add [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) to the service class and the source generator handles everything else: conditional registration, guard methods, and type isolation.

### Declaring the Service

```csharp
using StatsMod;

// The generator produces conditional registration automatically.
// This service only registers when stats_mod is loaded.
[StateService<IStatsIntegration, MyModule>]
[OptionalModDependent("stats_mod")]
internal class StatsIntegration : IStatsIntegration
{
    private readonly StatsApi _api;

    public StatsIntegration(StatsApi api)
    {
        _api = api;
    }

    public void ShowStats() => _api.Render();
}
```

The interface lives in your mod and contains no optional mod types:

```csharp
public interface IStatsIntegration
{
    void ShowStats();
}
```

### What Gets Generated

The source generator produces a configurator that checks `loadedMods` before registering:

```csharp
// Generated configurator (simplified)
void ConfigureServices(ICoreContainerBuilder builder, IReadOnlySet<string> loadedMods)
{
    // Unconditional services registered here...

    if (loadedMods.Contains("stats_mod"))
        Register_StatsIntegration_Factory(builder);
}

[ModLoadedGuard("stats_mod")]
private void Register_StatsIntegration_Factory(ICoreContainerBuilder builder)
{
    builder.Register<StatsIntegration_Factory>();
}
```

The guard method and `loadedMods` check are both generated. You do not write this code.

### Consuming the Optional Service

Consumers resolve the interface through `TryResolve`. When the optional mod is absent, the service was never registered and resolution returns null:

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
        _stats?.ShowStats();
    }
}
```

No [`IsModLoaded`](xref:Sparkitect.GameState.IGameStateManager.IsModLoaded(System.String)) checks needed on the consumer side. The null-conditional pattern handles absence cleanly.

## Manifest Declaration

Declare optional dependencies in your project file with `IsOptional="true"`:

```xml
<ItemGroup>
  <ModProjectDependency Include="..\CoreMod\CoreMod.csproj" />
  <ModProjectDependency Include="..\StatsMod\StatsMod.csproj" IsOptional="true" />
</ItemGroup>
```

This tells the engine that your mod can load without `stats_mod` being present. See the [Project SDK](xref:sparkitect.tooling.sdk) guide for the full project file reference.

## Isolation Pattern

For code outside the DI system (manual initialization, event handlers, direct API calls), isolate optional mod types in dedicated classes marked with <xref:Sparkitect.Modding.OptionalModDependentAttribute>.

### Integration Class

```csharp
using StatsMod;

[OptionalModDependent("stats_mod")]
internal class StatsModIntegration
{
    private readonly StatsApi _api = new();

    public void RegisterCustomStats()
    {
        _api.RegisterStat("my_mod_score", () => GetCurrentScore());
    }

    private int GetCurrentScore() => /* ... */;
}
```

The [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) attribute marks this class as containing optional mod type references. The optional dependency analyzer validates that these types do not leak into unmarked code.

### Guard Entry Points

Call into the integration class through methods marked with <xref:Sparkitect.Modding.ModLoadedGuardAttribute>. Always check <xref:Sparkitect.GameState.IGameStateManager.IsModLoaded(System.String)> before entering the guard:

```csharp
public class MyMod
{
    private readonly IGameStateManager _gsm;

    public void Initialize()
    {
        if (_gsm.IsModLoaded("stats_mod"))
        {
            InitializeStatsIntegration();
        }
    }

    [ModLoadedGuard("stats_mod")]
    private void InitializeStatsIntegration()
    {
        var integration = new StatsModIntegration();
        integration.RegisterCustomStats();
    }
}
```

[`[ModLoadedGuard]`](xref:Sparkitect.Modding.ModLoadedGuardAttribute) tells the analyzer that this method is a controlled entry point into optional mod code. The analyzer enforces type isolation everywhere else but allows optional mod types inside guarded methods.

## Common Mistakes

The optional dependency analyzer catches most type leakage at compile time. The examples below show patterns that would cause `TypeLoadException` at runtime if the analyzer did not flag them.

### Type Reference in Same Method as Guard

```csharp
// WRONG: TypeLoadException even though the check comes first
public void BadExample(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        var api = new StatsModApi();  // JIT resolves this type when compiling the method
    }
}
```

**Fix:** Move the type reference to a separate [`[ModLoadedGuard]`](xref:Sparkitect.Modding.ModLoadedGuardAttribute) method.

### Field Type from Optional Mod

```csharp
// WRONG: Field types resolve when the class is instantiated
public class BadExample
{
    private StatsModApi? _api;  // TypeLoadException when BadExample is created
}
```

**Fix:** Keep typed fields in [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) classes only, or use `object?` in non-isolated classes.

### Generic Type Parameters

```csharp
// WRONG: Generic constraint references optional type
public void Process<T>(T item) where T : IStatsProvider { }
```

**Fix:** Move generics to an isolated class, or use a non-generic interface.

### Lambda/Closure Captures

```csharp
// WRONG: Lambda body references optional type
if (gsm.IsModLoaded("stats_mod"))
{
    Action action = () => new StatsModApi().DoThing();  // TypeLoadException
}
```

**Fix:** Extract the lambda body to a method in an isolated class.

## Testing

Always test with the optional mod absent:

1. Build your mod
2. Remove the optional mod from the mods directory
3. Run the engine
4. Verify your mod loads and runs without the optional functionality

If you see `TypeLoadException` mentioning the optional mod's assembly, you have a type leakage problem. Check the common mistakes above.

## CLR Lazy Loading: Why This Works

> [!NOTE]
> This section explains the .NET runtime behavior behind the patterns above. You do not need this information to use optional dependencies; follow the patterns and the analyzer will catch mistakes.

The .NET JIT compiler compiles methods to native code on first invocation. It resolves **all** type references in a method body at compile time, before any code in that method executes.

```csharp
// The JIT resolves StatsModApi when compiling this method, not when
// the if-block runs. If the assembly is missing: TypeLoadException.
public void Example(IGameStateManager gsm)
{
    if (gsm.IsModLoaded("stats_mod"))
    {
        var api = new StatsModApi();
    }
}
```

The solution is to keep the guard check and the type usage in separate methods. The JIT only compiles a method when it is actually called, so a guarded method that is never invoked is never compiled and its type references are never resolved.

This is why [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) classes and [`[ModLoadedGuard]`](xref:Sparkitect.Modding.ModLoadedGuardAttribute) methods exist: they create method boundaries that the JIT respects. The DI integration takes this further by generating these boundaries automatically.

## Quick Reference

| Pattern | When to Use |
|---------|-------------|
| [`[StateService]`](xref:Sparkitect.GameState.StateServiceAttribute`2) + [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) | DI services that depend on optional mods (most common) |
| [`[OptionalModDependent]`](xref:Sparkitect.Modding.OptionalModDependentAttribute) class | Non-DI code that references optional mod types |
| [`[ModLoadedGuard]`](xref:Sparkitect.Modding.ModLoadedGuardAttribute) method | Entry points that cross into optional mod code |
| `TryResolve` | Consuming optional DI services |
| [`IsModLoaded`](xref:Sparkitect.GameState.IGameStateManager.IsModLoaded(System.String)) | Manual runtime checks outside DI |

## See Also

- [Dependency Injection](xref:sparkitect.core.dependency-injection) for the DI container hierarchy and service registration
- [External Dependencies](xref:sparkitect.core.external-dependencies) for NuGet and third-party library dependencies
- [Source Generation](xref:sparkitect.tooling.source-generation) for how the generator produces conditional registrations
