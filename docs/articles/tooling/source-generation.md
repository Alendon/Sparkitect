---
uid: sparkitect.tooling.source-generation
title: Source Generation
description: How Sparkitect uses Roslyn source generators to automate boilerplate and enable engine features
---

# Source Generation

Sparkitect uses [Roslyn source generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to shift boilerplate work from runtime to compile time. If you are familiar with C++ `constexpr` evaluation or Rust procedural macros, the concept is the same: the compiler runs additional code during the build that emits new C# source files into your project. The engine uses this to generate DI service factories, registry infrastructure, stateless function wrappers, and more.

This article covers the general source generation capability. Module-specific generator details live in their respective articles:

- [Dependency Injection](xref:sparkitect.core.dependency-injection) for `[StateService]` and generated service factories
- [Registry System](xref:sparkitect.core.registry-system) for `[Registry]` and generated registry infrastructure
- [Stateless Functions](xref:sparkitect.core.stateless-functions) for `[PerFrameFunction]`/`[TransitionFunction]` and generated wrappers

## What Gets Generated

When you build a mod project, the Sparkitect source generators analyze your attributed types and methods, then emit additional C# files into your compilation. Here is what each generator produces.

### StateModuleServiceGenerator

**Trigger:** Classes annotated with `[StateService<TBase, TModule>]`

**Emits:**
- A **service factory** class (`{TypeName}_Factory.g.cs`) that constructs the service with its constructor arguments and required properties resolved from the DI container
- A **configurator** class (`{ModuleName}_ServiceConfigurator.g.cs`) that registers all service factories for the module, including conditional registration guards for services marked with `[OptionalModDependent]`

These generated classes wire your service into the engine's DI container hierarchy without you writing any registration code. See [Dependency Injection](xref:sparkitect.core.dependency-injection) for how services participate in the container lifecycle.

### RegistryGenerator

**Trigger:** Classes annotated with `[Registry(Identifier = "...")]` that implement `IRegistry`

**Emits:**
- Nested **provider attributes** (one per register method) for declarative item registration
- **Registry metadata** as assembly-level attributes, enabling cross-assembly registry discovery
- A **keyed factory** class for the registry itself, resolving it through the DI pipeline
- A **configurator** (partial class with registration method and shell class with entrypoint)
- **ID framework** classes: a static ID container with strongly-typed properties for each registered item
- **Registration classes** for provider-attributed and YAML-defined entries

The RegistryGenerator is the most complex generator in the engine. It handles both source-defined registrations (via provider attributes) and file-defined registrations (via `.registration.yaml` files). See [Registry System](xref:sparkitect.core.registry-system) for the full registration and identification model.

### StatelessFunctionGenerator

**Trigger:** Static methods annotated with a `StatelessFunctionAttribute` derivative (e.g., `[PerFrameFunction("identifier")]`, `[TransitionFunction("identifier")]`) and a scheduling attribute

**Emits:**
- A **wrapper class** (`{IdentifierPascalCase}Func`) nested inside the containing type, implementing `IStatelessFunction` with parameter resolution from the DI container
- **Registration code** that registers the wrapper into the appropriate function registry
- **Scheduling entrypoint code** that applies the scheduling attributes to control when the function executes during state transitions or per-frame updates
- **ID properties** for strongly-typed function identification

See [Stateless Functions](xref:sparkitect.core.stateless-functions) for the attribute API, scheduling model, and ordering system.

### Other Generators

The engine includes additional specialized generators:

- **FacadeMappingGenerator** -- generates facade-to-implementation type mappings for state-scoped service resolution
- **CallerContextGenerator** -- injects call-site tracking into Vulkan API wrappers for debugging
- **LogEnricherGenerator** -- generates Serilog log enricher classes

### Inspecting Generated Code

To see what the generators produce, enable the `EmitCompilerGeneratedFiles` MSBuild property in your project:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files appear under `obj/{Configuration}/{TFM}/generated/Sparkitect.Generator/`. This is useful for debugging registration issues or understanding what the engine generates from your attributes.

## How It Works

### Roslyn Incremental Generators

All Sparkitect generators implement `IIncrementalGenerator`, Roslyn's incremental source generation API. Each generator:

1. **Registers syntax providers** that filter the compilation for relevant attributed types or methods
2. **Transforms** matched syntax nodes into model objects, extracting all necessary symbol data at the pipeline boundary
3. **Renders** C# source from models using the Fluid template engine (Liquid templates)

The incremental pipeline ensures generators only re-run when their specific inputs change, keeping build times proportional to the size of your changes rather than the size of your project.

### The DiPipeline Toolbox

Multiple generators need to produce DI-related code (factories, configurators, registrations). Rather than duplicating this logic, the engine provides `DiPipeline` -- a public static toolbox class in the `Sparkitect.Generator.DI.Pipeline` namespace.

`DiPipeline` provides four core operations:

| Method | Purpose |
|--------|---------|
| `ExtractFactory` | Extracts a `FactoryModel` from an attributed type symbol, capturing constructor arguments, required properties, and optional mod IDs |
| `RenderFactory` | Renders a service or keyed factory class from a `FactoryModel` using Liquid templates (`ServiceFactory.liquid` or `KeyedFactory.liquid`) |
| `ToRegistration` | Converts a `FactoryModel` into a `RegistrationModel` for configurator generation |
| `RenderConfigurator` | Renders a configurator class from an array of `RegistrationModel` entries, handling both unconditional and conditional (mod-dependent) registrations |

The pipeline flow for a typical generator:

```
[Attributed Type] --> ExtractFactory --> FactoryModel
                                            |
                          RenderFactory ----+----- ToRegistration
                              |                        |
                     Factory .g.cs              RegistrationModel
                                                       |
                                              RenderConfigurator
                                                       |
                                              Configurator .g.cs
```

### Supporting Models

The pipeline uses these model types (all in `Sparkitect.Generator.DI.Pipeline`):

- **`FactoryModel`** -- captures the base type, implementation type, constructor arguments, required properties, factory intent (Service or Keyed), and optional mod IDs
- **`RegistrationModel`** -- captures the factory type name and conditional mod IDs for registration guards
- **`ConfiguratorOptions`** -- controls the output class name, namespace, base type, entrypoint attribute, configurator kind, and partial class behavior
- **`FactoryIntent`** -- discriminated union: `Service` (singleton in core container) or `Keyed` (string-keyed in factory container)
- **`ConfiguratorKind`** -- discriminated union: `Service` (core container builder) or `Keyed` (factory container builder with base type)

### Liquid Templates

Code generation uses the [Fluid](https://github.com/sebastienros/fluid) template engine with Liquid syntax. Templates live alongside their generator code:

- `DI/ServiceFactory.liquid` -- service factory class
- `DI/KeyedFactory.liquid` -- keyed factory class (for registries)
- `DI/Configurator.liquid` -- configurator class with optional conditional guards
- `Stateless/StatelessFunctionWrapper.liquid` -- stateless function wrapper
- `Stateless/StatelessFunctionScheduling.liquid` -- scheduling entrypoint
- `Modding/RegistryAttributes.liquid` -- provider attributes
- `Modding/RegistryRegistrations.Unit.liquid` -- registration methods

Templates receive strongly-typed model objects and produce complete, compilable C# source files.

## DiPipeline as a Public Tool

The `DiPipeline` class is explicitly designed as a **public, endorsed tool** for advanced mod developers and engine contributors who want to build new generator pipelines.

Key design characteristics:

- **Static toolbox pattern** -- no instance state, no fields, all public static methods. Call any method independently without setup.
- **Symbol boundary extraction** -- `ExtractFactory` extracts all necessary data from Roslyn symbols into plain model objects. Downstream methods work with models only, never with Roslyn types.
- **Composable** -- each method does one thing. Generators combine them as needed: `StateModuleServiceGenerator` uses `ExtractFactory` + `RenderFactory` + `ToRegistration` + `RenderConfigurator`; `RegistryGenerator` uses the same pipeline but with `FactoryIntent.Keyed` instead of `FactoryIntent.Service`.

The `RegistryWithFactory` wrapper type keeps `RegistryModel` (the registry's domain model) clean of DI pipeline types by holding the `FactoryWithRegistration` separately. This separation means registry-specific code never depends on DI pipeline internals.

### Method Signatures

```csharp
public static FactoryModel? ExtractFactory(
    INamedTypeSymbol symbol, FactoryIntent intent, string baseType)

public static bool RenderFactory(
    FactoryModel model, out string code, out string fileName)

public static RegistrationModel ToRegistration(
    FactoryModel factory, INamedTypeSymbol symbol)

public static bool RenderConfigurator(
    ImmutableValueArray<RegistrationModel> registrations,
    ConfiguratorOptions options,
    out string code, out string fileName)
```

## Design Decisions

### Why Source Generation Over Runtime Reflection

Source generation provides three advantages over runtime reflection or IL emission:

1. **Compile-time safety** -- registration errors surface as build errors, not runtime exceptions
2. **Zero runtime cost** -- all factory and registration code is generated ahead of time; no reflection, no `Activator.CreateInstance`, no expression tree compilation at startup
3. **IDE support** -- generated types are visible to IntelliSense, analyzers, and refactoring tools

### Trade-offs

Some generators intentionally deviate from Roslyn source generator best practices to provide their functionality. For example, the `RegistryGenerator` uses patterns that go beyond typical incremental generator recommendations to support cross-assembly registry discovery and provider attribute generation. These trade-offs are documented in the [Registry System](xref:sparkitect.core.registry-system) article where the specifics are relevant.

The engine also includes companion Roslyn analyzers that validate source generator inputs at edit time, providing immediate feedback when attributes are misconfigured or required patterns are missing.
