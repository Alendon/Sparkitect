# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Sparkitect: Modular 3D game engine (.NET 10) where **everything is a mod**. Framework-first, not editor-centric.

**Core Philosophy:** Integration Over Duplication - build atop existing systems rather than parallel implementations.

## Essential Commands

### Build
```bash
dotnet build Sparkitect.sln
```

### Test
**CRITICAL:** Use `dotnet run`, NOT `dotnet test` (incompatible with TUnit framework)
```bash
# Run tests (without rebuilding - assume binary is current unless you changed code)
dotnet run --project tests/Sparkitect.Tests/Sparkitect.Tests.csproj --no-build
dotnet run --project tests/Sparkitect.Generator.Tests/Sparkitect.Generator.Tests.csproj --no-build

# If you made changes, omit --no-build
dotnet run --project tests/Sparkitect.Tests/Sparkitect.Tests.csproj
```

### Run Engine
Currently the engine can not be ran by claude code.

## Critical Architecture Details

### 1. Custom Dependency Injection (DI)

Three container types (all immutable after creation):
- **CoreContainer**: Hierarchical singletons (Base → Root → Game levels)
- **EntrypointContainer**: Discoverable configuration entrypoints for deterministic processing
- **FactoryContainer**: Keyed object factory with caching/recycling

**Key:** Source-generated factories (zero reflection). Property injection for circular dependencies.

```csharp
// Core module services (outside GSM structure)
[CreateServiceFactory<IModManager>]
internal class ModManager : IModManager
{
    public ModManager(ICliArgumentHandler cli) { }
    public required ISomeService Service { get; init; }  // Circular dep
}

// Future module services (within GSM)
[StateService<ITimeApi, TimeModule>]
internal class TimeService : ITimeApi { }

[KeyedFactory<IProcessor>(Key = "json")]
internal class JsonProcessor : IProcessor { }
```

### 2. Game State System

Hierarchical state machine with module-based composition. States composed from modules. State functions are static methods with attribute-based scheduling.

**Unique Pattern - Facade Split:**
```csharp
[StateFacade<ITimeFacade>]      // What state functions see
public interface ITimeApi { }    // What public DI sees

[StateService<ITimeApi, TimeModule>]
internal class TimeService : ITimeApi, ITimeFacade { }
```

**Scheduling:** `[PerFrame]`, `[OnCreate]`, `[OnDestroy]`, `[OnFrameEnter]`, `[OnFrameExit]`
**Ordering:** `[OrderBefore("fn")]` or `[OrderBefore<ModuleType>("fn")]` for cross-scope

### 3. Registry System

Dual identifiers: `"sparkitect:blocks:stone"` ↔ `1:3:42` (human ↔ numeric)
Per-state triggered (not global). Generated attributes on static methods:

```csharp
[DummyRegistry.RegisterValue("hello")]
public static string GetValue() => "Hello World";
```

### 4. Source Generation

Uses Roslyn + Fluid (Liquid) templates. Generates DI factories, state wrappers, facade mappings, registry attributes.
**Important:** Analyzer errors enforce patterns - they're intentional. Generated code in `Sparkitect.CompilerGenerated`.

### 5. Modding System

Everything is a mod. Two types: Root (engine-level) and Game (session-level).
Loaded from `.zip` archives, streams stay open. Project properties: `ModName`, `ModIdentifier`, `ModVersion`, `ModType`

## Key Patterns

**Attribute-Driven Discovery:** Paired attribute + base class for type-safe discovery
```csharp
[CoreContainerConfiguratorEntrypoint]
public class MyConfig : CoreConfigurator { }
```

**Immutability:** Containers never modified post-creation. State transitions create new child containers.

**Unified State Functions:** Single concept for all state work. Static methods, DI via wrappers, attribute-based scheduling.

## Development Quirks

- **Internal by default** - explicit public API
- **TUnit** for testing: `await Assert.That(value).IsTrue()`
- **Snapshot tests:** `.verified.cs` files for generators - only validated by developer
- **Nullable reference types** enabled
- Check `docs/articles/core/` for detailed system explanations

## Workflow Instructions

After each significant block of work, remember:
1. **Precise surgical edits** - modify only what's necessary
2. **Minimal comments** - reduce noise, let code speak
3. **Minimize chat output** - be concise
4. **Use questions actively in thinking:**
   - Start: 1-3 questions to guide research
   - Middle: 5-8 questions to plan implementation
   - End: 1-3 questions to finalize and offer feedback points
5. **Use AskUserQuestion tool** for questions
6. **Ask directly** if unclear about direction