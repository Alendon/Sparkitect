# Repository Guidelines

## Project Structure & Module Organization
- `src/Sparkitect`: Main executable; enables nullable reference types and uses Roslyn source generators.
- `gen/Sparkitect.Generator`: Source generator + analyzers used by the engine and samples.
- `src/Sparkitect.Sdk`: MSBuild SDK tasks/targets consumed by mods.
- `tests/`: Unit tests (`Sparkitect.Tests`) and generator tests (`Sparkitect.Generator.Tests`) with snapshots.
- `benchmark/`: Microbenchmarks for performance validation.
- `samples/MinimalSampleMod`: Minimal mod that references the generator.
- `docs/`: DocFX site (`docs/docfx.json`) and articles.

## Build, Test, and Development Commands
- Build solution: `dotnet build Sparkitect.sln -c Release`
- Run engine: `dotnet run --project src/Sparkitect`
- Run tests (all): `dotnet test -c Release`
- Run benchmarks: `dotnet run -c Release --project benchmark/Sparkitect.Benchmark`
- Build docs (requires docfx): `docfx docs/docfx.json`

Notes
- The repo pins the SDK via `global.json` (10.0.0); ensure `dotnet --version` matches.
- Packages for projects that enable `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>` are produced on build.

## Coding Style & Naming Conventions
- Language: C# (net10.0, preview where specified).
- Indentation: 4 spaces; braces on new lines (standard C# style).
- Naming: PascalCase for types/methods; camelCase for locals/params; interfaces prefixed with `I`; namespaces under `Sparkitect.*` for core project.
- Nullability: enabled; treat nullable warnings seriously (some projects set `WarningsAsErrors=Nullable`).
- Respect analyzer warnings (e.g., TUnit analyzers) and keep generator output namespaces consistent (`SgOutputNamespace` msbuild property).

## Testing Guidelines
- Framework: TUnit with async-friendly assertions; generator tests use Verify snapshots in `tests/**/TestResults`.
- File naming: `*Tests.cs`; method naming: `Method_Scenario_Expected`.
- Run: `dotnet test`; add/adjust tests with every behavior change. For snapshot updates, review diffs carefully and commit only intentional changes.

## Commit & Pull Request Guidelines
- Commits: Prefer Conventional Commits (e.g., `feat:`, `fix:`, `docs:`, `test:`). Keep messages imperative and scoped.
- PRs: Include a clear description, linked issues, and rationale. Add test coverage, update docs when user-facing behavior or APIs change, and ensure CI passes.

## Source Generator & Analyzer Guidelines

### Core Principles
- Follow the incremental pipeline model: Input → Transform → Model → Output
- All models must be value-equatable for proper caching
- Keep transformations pure and stateless
- Do Error Reporting in Roslyn Analyzers alongside the Generators
- Generators silently fail but never throw exceptions (eg by returning null)
- Use custom .NotNull extension method to automatically filter null values and transform type to non nullable
- Determinism: avoid nondeterministic iteration (HashSet/Dictionary); sort inputs before emission; never depend on time/random.

### Pipeline Architecture
- Use `ForAttributeWithMetadataName` for attribute-driven generators (99x more efficient than CreateSyntaxProvider)
- Extract symbols to value-equatable models immediately, never store ISymbol/SyntaxNode in models
- Use `Collect()` sparingly - only when you need all items at once
- Combine providers using `Combine()` for related data that needs to be processed together
- Register outputs in the order of dependencies (e.g., attributes before metadata)
- Only use `ImmutableValueArray<T>` (custom immutable collection with value equality) instead of regular collections
- Never use any other collection type, for passing data between pipeline steps
- Apply `WithComparer(...)` for custom DTO equality and `WithTrackingName(...)` on key providers.
- Respect `CancellationToken` in transforms and syntax provider callbacks.

### Model Design
- Use records or implement proper value equality for all models
- Only use `ImmutableValueArray<T>`, never `ImmutableArray<T>` or other, for value comparison semantics
- Keep models flat and simple when possible
- Extract only the data you need from symbols (names, types as strings, not full symbols; BREAKS pipeline otherwise!!!)
- Replace any `ValueCompareSet<T>` usage with `ImmutableValueArray<T>`; define whether semantics are set-like (order-insensitive) or ordered (pre-sort) and ensure hash/equality match.

### Build Properties
- Use GetModBuildSettings extension method, to access build settings provider
- Extend Sdk.targets file, when adding more build settings
- Expose every consumed MSBuild property via `<CompilerVisibleProperty>` so generators can read them.

### Code Generation
- ALWAYS Use Fluid templates (.liquid files) for code generation
- Follow naming convention: `{TypeName}_{Purpose}.g.cs`
- Place generated code in appropriate namespaces using `SgOutputNamespace` MSBuild property
- Always use full qualification for ALL types (global::)
- Namespacing: when `SgOutputNamespace` is set, always use it; otherwise fall back to the containing namespace consistently.
- Sort template inputs (methods, members, files) before rendering to keep outputs stable.

### Additional Files
- Filter strictly (e.g., `*.sparkres.yaml`) using `AdditionalTextsProvider`.
- Parse only when content changes; transform to immutable DTOs; attach a comparer if needed.
- Invalid content or schema violations must be reported by analyzers; do not throw or silently swallow in generators.

### Performance Best Practices
- Batch related outputs when they share the same data source
- Avoid heavy processing in Select operations - extract to separate methods
- Cache expensive computations by ensuring proper value equality

### Common Anti-Patterns to Avoid
- ❌ Don't modify existing user code
- ❌ Don't perform I/O in transformations (except AdditionalTexts)
- ❌ Don't use mutable state or static fields
- ❌ Don't put non-equatable types in pipeline models
- ❌ Don't scan for indirect interface implementations without marker attributes

### Testing Source Generators
- Use Verify for snapshot testing (`TestResults/*.verified.cs`)
- Snapshots are always validated by the Developer, never by the LLM!!!
- Test incremental behavior - unchanged inputs should reuse cached outputs
- Test empty/null inputs and edge cases
- Use `RegistryGeneratorTests` as reference for test patterns
- Cover `.editorconfig` build options and `AdditionalTexts` scenarios.
- Add incrementality tests for order-insensitivity and unrelated-change no-op.

### Analyzer Guidelines
- Analyzers should complement generators by validating user input
- Report diagnostics for invalid attribute usage
- Validate resource file formats (YAML, JSON) when used with generators
- Use appropriate diagnostic severity levels (Error, Warning, Info)
- Coverage (concise): DI constraints (single ctor, abstract/interface deps, required init-only); keyed-factory rules (exactly one key source, valid key type); registry rules (in namespace, unique names); resource schema (mutually exclusive `File` vs `Files`).

### RegistryGenerator Notes (temporary exceptions)
- Aggregation: central configurator intentionally aggregates across registries; keep DTOs minimal/immutable and use `Collect()` only where truly required.
- Attribute staging: emit dependent attributes first to stabilize later analysis; keep staged outputs deterministic.

## Security & Configuration Tips
- Never commit secrets. DocFX publishing runs via GitHub Actions—use repository secrets.
- Local builds rely on the pinned SDK; avoid downgrading language features or frameworks.
