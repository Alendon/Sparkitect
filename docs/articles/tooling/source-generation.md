---
uid: sparkitect.tooling.source-generation
title: Source Generation
description: Fundamental source generation patterns and infrastructure used across all Sparkitect engine components
---

# Source Generation

Sparkitect uses [Roslyn source generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to shift boilerplate from runtime to compile time. If you are familiar with C++ `constexpr` evaluation or Rust procedural macros, the concept is the same: the compiler runs additional code during the build that emits new C# source files into your project. Nearly every engine subsystem (DI, registries, stateless functions, game state) relies on source generation to wire things up automatically.

This article covers the **fundamental structures and patterns** that Sparkitect's generators share. For how source generation applies to a specific subsystem, see the relevant module article:

- [Dependency Injection](xref:sparkitect.core.dependency-injection): [`[StateService]`](xref:Sparkitect.GameState.StateServiceAttribute`2) and generated service factories
- [Registry System](xref:sparkitect.core.registry-system): [`[Registry]`](xref:Sparkitect.Modding.RegistryAttribute) and generated registry infrastructure
- [Stateless Functions](xref:sparkitect.core.stateless-functions): [`[PerFrameFunction]`](xref:Sparkitect.Stateless.PerFrameFunctionAttribute)/[`[TransitionFunction]`](xref:Sparkitect.Stateless.TransitionFunctionAttribute) and generated wrappers

## What This Means For You

As a mod author, source generation mostly works through **attributes**. You annotate your types and methods, and the engine generates the necessary wiring code at compile time. You never write factory classes, registration methods, or configurator boilerplate by hand. Some generators, like log enrichment, work implicitly on all matching call sites without requiring any annotation.

For example, annotating a class with [`[StateService<IMyService, MyModule>]`](xref:Sparkitect.GameState.StateServiceAttribute`2) causes the engine to generate a factory that constructs it with all dependencies resolved, plus a configurator that registers it into the correct DI container. You write the service class; the generator handles the rest.

### Viewing Generated Output

Most IDEs (Rider, Visual Studio) provide an integrated view of Roslyn source generator output under the project's analyzer dependencies. This is the easiest way to inspect what gets generated.

If you prefer file-system access, enable the `EmitCompilerGeneratedFiles` MSBuild property:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files then appear under `obj/{Configuration}/{TFM}/generated/Sparkitect.Generator/`.

### Fail-Fast Generators and Companion Analyzers

The generators themselves are written as **fail-fast**. They do not try to recover from misconfigured input or assume corrections. If a type does not meet the requirements (missing constructor, wrong attribute arguments, unsupported shape), the generator silently ignores it and produces no output for that type.

To compensate, the engine includes Roslyn **analyzers** alongside its generators. These validate source generator inputs at edit time. If an attribute is misconfigured or a required pattern is missing, you get a build error or IDE warning immediately rather than silently missing output. The analyzers are the error reporting layer; the generators are the code emission layer.

## Fundamental Patterns

The following patterns appear across all Sparkitect generators. Understanding them helps when reading generated code, building new generators, or contributing to the engine.

### Incremental Pipeline

All generators implement `IIncrementalGenerator` and follow the same three-stage pipeline:

1. **Filter**: `ForAttributeWithMetadataName` selects syntax nodes that carry the trigger attribute
2. **Transform**: A lambda extracts all necessary data from Roslyn symbols into a plain model record
3. **Output**: `RegisterSourceOutput` receives the model and renders source code

```
ForAttributeWithMetadataName("Attribute")
    |-- predicate: node is ClassDeclarationSyntax
    +-- transform: (syntaxContext) => extract model from INamedTypeSymbol
            |
            v
    IncrementalValuesProvider<TModel>
            |
            |-- RegisterSourceOutput (per item)
            |       +-- render individual file (e.g. factory)
            |
            +-- .Collect() -> RegisterSourceOutput (grouped)
                    +-- group by key, render aggregate file (e.g. configurator)
```

The incremental pipeline ensures generators only re-run when their specific inputs change, keeping build times proportional to the size of your changes rather than the size of your project.

### Symbol Boundary Crossing

A strict rule across all generators: **extract everything from Roslyn symbols early, then work only with plain model records downstream.**

In the transform step, the generator reads all needed data from `INamedTypeSymbol` (type names, constructor parameters, attributes, namespace) and packs it into a record. Nothing after the transform ever touches Roslyn's `ISymbol` types.

This matters because Roslyn's incremental pipeline caches transform results between compilations. If a model held a reference to a Roslyn symbol, the cache would retain stale compiler state. Plain records with value equality are cache-safe.

### Model Records With Value Equality

Generator models are C# records, which gives them structural equality out of the box. Two models with the same data are considered equal. This is what makes incremental caching work: if the model didn't change between builds, the output step is skipped entirely.

For collection properties, Sparkitect uses `ImmutableValueArray<T>` instead of `ImmutableArray<T>`. The standard `ImmutableArray<T>` uses reference equality, which would defeat incremental caching. `ImmutableValueArray<T>` provides ordered sequence equality:

```csharp
// Two arrays with the same elements are equal
var a = new[] { "x", "y" }.ToImmutableValueArray();
var b = new[] { "x", "y" }.ToImmutableValueArray();
a.Equals(b); // true, element-by-element comparison
```

It implements `IReadOnlyCollection<T>`, `IEquatable<T>`, and `IStructuralEquatable`, and has a `Builder` class for incremental construction. Every model that holds a list of items uses this type.

### Template-Driven Code Emission

All generators emit code through Liquid templates (via the [Fluid](https://github.com/sebastienros/fluid) engine) rather than string concatenation. Templates are embedded as assembly resources and rendered through `FluidHelper`:

```csharp
FluidHelper.TryRenderTemplate("DI.ServiceFactory.liquid", model, out var code);
```

Templates receive the model record as their context and produce complete, compilable C# files. This keeps rendering logic readable and separate from extraction logic.

The templates are organized by domain alongside their generator code:

| Domain | Templates |
|--------|-----------|
| DI | `ServiceFactory.liquid`, `KeyedFactory.liquid`, `Configurator.liquid` |
| Stateless | `StatelessFunctionWrapper.liquid`, `StatelessFunctionScheduling.liquid` |
| Registry | `RegistryAttributes.liquid`, `RegistryRegistrations.Unit.liquid`, `RegistryIdContainer.Framework.liquid`, and others |
| Infrastructure | `CallerContextInjector.liquid`, `LogEnricher.liquid` |

### Render Pattern

Generator output methods follow a consistent signature:

```csharp
public static bool RenderSomething(TModel model, out string code, out string fileName)
```

The `bool` return indicates whether rendering succeeded. Callers use it as a guard:

```csharp
if (DiPipeline.RenderFactory(model, out var code, out var fileName))
    ctx.AddSource(fileName, code);
```

This pattern avoids exceptions in the generator pipeline. A failed render is silently skipped rather than crashing the build.

### Null Filtering

The transform step returns `null` when a syntax node doesn't match requirements (wrong type, missing data). A shared extension filters these out:

```csharp
public static IncrementalValuesProvider<T> NotNull<T>(
    this IncrementalValuesProvider<T?> provider) where T : class
```

Every generator chains `.NotNull()` after its transform to produce a clean `IncrementalValuesProvider<TModel>` with no nulls.

### Display Formats

When converting Roslyn symbols to string type names for models, generators use `DisplayFormats.NamespaceAndType`. This is a predefined `SymbolDisplayFormat` that produces a fully-qualified name without the `global::` prefix and without nullable annotations. It ensures consistent type name representation across all extracted models.

For code emission (in templates and rendering methods), the `global::` prefix is inserted again to produce unambiguous type references in generated C# files.

## The DiPipeline Toolbox

Multiple generators need to produce DI-related code: factories, configurators, registrations. Rather than each generator implementing this independently, the engine provides [`DiPipeline`](xref:Sparkitect.DI.Pipeline.DiPipeline), a public static toolbox in `Sparkitect.Generator.DI.Pipeline`.

### Design

[`DiPipeline`](xref:Sparkitect.DI.Pipeline.DiPipeline) is a **static toolbox**: no instance state, no fields, all public static methods. Call any method independently without setup. This makes it composable. Generators pick the methods they need and combine them freely.

### Core Operations

| Method | Input | Output | Purpose |
|--------|-------|--------|---------|
| `ExtractFactory` | `INamedTypeSymbol`, `FactoryIntent`, base type | `FactoryModel?` | Crosses the symbol boundary: extracts constructor args, required properties, optional mod IDs into a plain model |
| `RenderFactory` | `FactoryModel` | `bool` + code + fileName | Renders a factory class via Liquid template (dispatches to `ServiceFactory.liquid` or `KeyedFactory.liquid` based on intent) |
| `ToRegistration` | `FactoryModel`, `INamedTypeSymbol` | `RegistrationModel` | Converts a factory into a registration entry for configurator generation |
| `RenderConfigurator` | `ImmutableValueArray<RegistrationModel>`, `ConfiguratorOptions` | `bool` + code + fileName | Renders a configurator class with unconditional and conditional (mod-dependent) registrations |

The typical flow through the pipeline:

```
[Attributed Type] -> ExtractFactory -> FactoryModel
                                           |
                         RenderFactory ----+---- ToRegistration
                             |                        |
                      Factory .g.cs            RegistrationModel
                                                       |
                                              RenderConfigurator
                                                       |
                                              Configurator .g.cs
```

### Models

```csharp
// What kind of factory to generate
public abstract record FactoryIntent
{
    public sealed record Service : FactoryIntent;
    public sealed record Keyed(string Key) : FactoryIntent;
}

// What kind of DI container the configurator targets
public abstract record ConfiguratorKind
{
    public sealed record Service : ConfiguratorKind;
    public sealed record Keyed(string BaseType) : ConfiguratorKind;
}

// Extracted factory data: all strings, no Roslyn types
public record FactoryModel(
    string BaseType,
    string ImplementationTypeName,
    string ImplementationNamespace,
    ImmutableValueArray<ConstructorArgument> ConstructorArguments,
    ImmutableValueArray<RequiredProperty> RequiredProperties,
    FactoryIntent Intent,
    ImmutableValueArray<string> OptionalModIds);

// Registration entry for a configurator
public record RegistrationModel(
    string FactoryTypeName,
    ImmutableValueArray<string> ConditionalModIds);

// Configurator rendering options
public record ConfiguratorOptions(
    string ClassName, string Namespace, string BaseType,
    string EntrypointAttribute, ConfiguratorKind Kind,
    bool IsPartial = false, string? MethodName = null,
    string? ModuleTypeFullName = null);
```

`FactoryIntent` and `ConfiguratorKind` are discriminated unions (sealed record hierarchy with private constructor). This pattern appears throughout the generator codebase for type-safe branching without raw enums.

### Using DiPipeline in a Generator

Here is the complete pattern, using the state module service generator as an example of how the pieces compose:

```csharp
// 1. Filter: find classes with the trigger attribute
var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
    "Sparkitect.GameState.StateServiceAttribute`2",
    (node, _) => node is ClassDeclarationSyntax,
    (syntaxContext, _) =>
    {
        var classSymbol = syntaxContext.TargetSymbol as INamedTypeSymbol;
        // 2. Transform: extract model at symbol boundary
        var factory = DiPipeline.ExtractFactory(
            classSymbol, new FactoryIntent.Service(), baseType);
        var registration = DiPipeline.ToRegistration(factory, classSymbol);
        return new MyData(new FactoryWithRegistration(factory, registration), ...);
    }).NotNull();

// 3a. Output individual factories
context.RegisterSourceOutput(provider, (ctx, data) =>
{
    if (DiPipeline.RenderFactory(data.Factory, out var code, out var fileName))
        ctx.AddSource(fileName, code);
});

// 3b. Output grouped configurators
context.RegisterSourceOutput(provider.Collect(), (ctx, all) =>
{
    foreach (var group in all.GroupBy(x => x.GroupKey))
    {
        var registrations = group.Select(x => x.Registration)
            .ToImmutableValueArray();
        if (DiPipeline.RenderConfigurator(registrations, options,
            out var code, out var fileName))
            ctx.AddSource(fileName, code);
    }
});
```

This same composition is used by the registry generator (with `FactoryIntent.Keyed`), the stateless function generator, and the facade mapping generator. The [`DiPipeline`](xref:Sparkitect.DI.Pipeline.DiPipeline) is explicitly designed as a **public, endorsed tool**. Advanced mod developers and engine contributors can use it to build new generator pipelines.

## Design Decisions

### Why Source Generation Over Runtime Reflection

1. **Compile-time safety**: registration errors surface as build errors, not runtime exceptions
2. **Zero runtime cost**: no reflection, no `Activator.CreateInstance`, no expression tree compilation at startup
3. **IDE integration**: generated types are visible to IntelliSense, analyzers, and refactoring tools

### Why Liquid Templates Over String Builders

Templates keep rendering logic readable and maintainable. A factory template reads like the C# file it produces, with Liquid `{% for %}` loops and `{{ variable }}` interpolation. The alternative (nested `StringBuilder.AppendLine` chains) becomes unreadable quickly and is error-prone for complex output like configurators with conditional registration guards.

### Why ImmutableValueArray Over ImmutableArray

Roslyn's incremental pipeline compares transform outputs between compilations to decide whether to re-run the output step. `ImmutableArray<T>` uses reference equality, so two arrays with identical contents are considered "changed". `ImmutableValueArray<T>` compares element-by-element, so unchanged data correctly skips regeneration.
