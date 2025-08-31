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

## Security & Configuration Tips
- Never commit secrets. DocFX publishing runs via GitHub Actions—use repository secrets.
- Local builds rely on the pinned SDK; avoid downgrading language features or frameworks.
