# Claude Agent Guide for Sparkitect

This guide explains how LLM agents should work in this repository. It captures authoritative rules for coding, generators, analyzers, packaging, and documentation, and reflects the current architecture and constraints of the project.

Status: authoritative for agent behavior and repo conventions; areas called out as “planning” are explicitly labeled.


## Purpose and Scope
- Audience: contributors and automation agents making structured changes (code, generators, SDK, docs).
- Stage: early; core systems are in active development. Prefer minimal, deterministic changes that fit the existing patterns.
- Hard constraint: do not attempt to run or rely on `dotnet` commands. Local execution via CLI is not enabled for agents in this repo.


## Quick Rules of Engagement
- Do not run `dotnet build/test/pack` or any `dotnet` command.
- Prefer surgical patches over broad refactors; keep style consistent with nearby code.
- Generators must be deterministic; analyzers report user errors; generators fail silently (emit nothing) rather than throw.
- Respect nullability as errors; treat warnings accordingly (projects elevate nullable warnings).
- If something is ambiguous, stop and ask early rather than guessing.


## Repository Map (high-level)
- `src/Sparkitect`: engine core (DI, Modding, GameState, scaffolds for ECS/Graphics).
- `gen/Sparkitect.Generator`: incremental source generators + analyzers, Fluid templates, utility `ImmutableValueArray<T>`.
- `src/Sparkitect.Sdk`: MSBuild SDK for mod packaging + DevLauncher generation.
- `samples/MinimalSampleMod`: minimal registry + DI sample.
- `tests/`: unit tests (TUnit) + generator snapshot tests (Verify).
- `docs/`: DocFX site (conceptual docs, may include planning content).
- `benchmark/`: microbenchmarks (non-blocking).

Core vs Gameplay split:
- Core: ModManager (+Registry), DI, State Management, bootstrap wiring.
- Gameplay-level systems (ECS, Vulkan, Networking, etc.) are added via states; they are optional and may be omitted by mods.


## Engine Bootstrap (authoritative)
- `EngineBootstrapper` is intentionally minimal: initialize logging, initialize CLI args, enter the root state, clean up.
- Any logic that can be performed by states must live in the state system, not in the bootstrapper.
- Exception: components that must bind directly to `string[] args` (e.g., CLI argument handler) are initialized in the bootstrapper.


## Dependency Injection (authoritative)
- Runtime DI is custom (not DryIoc). Remove or ignore prior mentions of third-party DI in docs.
- Containers are immutable once built; resolution flows through parent-first lookup.
- Builder enforces: DAG for constructor deps; two-pass create→apply-properties for handling cycles via properties.
- Nullability indicates optionality in generator-extracted ctor/property dependencies (optional when annotated).

Factories
- Singleton/service factories: generated `ClassName_Factory` implement `IServiceFactory`.
- Keyed factories: generated `ClassName_KeyedFactory` implement `IKeyedFactory<TBase>`.
- Key contract (current):
    - Keys may be `string` or `Sparkitect.Modding.Identification`.
    - Direct key via attribute property, or key via static property reference.
    - OneOf-based key variants are obsolete (do not introduce or rely on them).

Analyzers vs Generators
- Analyzers (e.g., SPARK1001–1008) validate inputs: single constructor, abstract/interface deps, required init-only, keyed factory key rules.
- Generators must never throw; if inputs are invalid, emit nothing and let analyzers surface diagnostics.


## Modding and Registries (authoritative)
Identification
- Triad: ModId (string↔ushort), CategoryId (string↔ushort), ObjectId (string↔uint). See `IdentificationManager`.
- Category identifiers (strings) must be globally unique.

Registries
- Define registries with `[Registry(Identifier = "snake_case")]` on partial classes implementing `IRegistry`.
- Provider attributes are generated per-registry to support: method, property, type-based providers.
- Resource YAML (`*.sparkres.yaml`) is used at compile time by the SG to generate code; it is not packaged into the mod.
- The previous “registry phases” model is obsolete. Registrations are triggered by state transitions at developer-chosen moments. Phases may return later if needed, but are not core at this time.

Facades
- State facades and registry facades are provided via source-generated entrypoints that can be discovered/queried when required.
- They are not baked into the core DI containers.


## Game State (current intent; still evolving)
- State management is the orchestration layer: transitional/setup states vs gameplay states with main loop.
- `IStateContainerBuilder` supports mapping facades to services and building facade-aware containers.
- State transitions are the hook to trigger registry runs deterministically.
- This system is still being designed; keep changes minimal and aligned to existing patterns.


## SDK & Packaging (authoritative)
- The SDK (`Sparkitect.Sdk`) provides tasks:
    - `GenerateModManifest` (manifest.json generation).
    - `ParseDependencyFile` (detect direct/transitive deps from `.deps.json`).
    - `CreateModArchive` (produces `.sparkmod`).
- DevLauncher
    - The SDK generates a DevLauncher source file so mods can be “Run” in the IDE.
    - DevLauncher is required for local debug runs; it is not intended to be packaged into the mod archive.
- Resource YAML
    - `*.sparkres.yaml` files must be discoverable by the generator via AdditionalFiles; they are not included in the packaged output.


## Source Generators & Pipeline Rules (authoritative)
- Follow incremental model: Input → Transform → Model → Output.
- Models must be value-equatable; avoid holding `ISymbol` or `SyntaxNode` in models.
- Keep transforms pure/stateless; respect `CancellationToken`.
- Use `ForAttributeWithMetadataName` for attribute-driven scans; `Collect()` sparingly.
- Combine providers with `Combine()` only when coupled; register outputs in dependency order.
- Always fully-qualify emitted types (`global::`) and sort inputs before rendering.
- Template naming: `{TypeName}_{Purpose}.g.cs` and use `SgOutputNamespace` when provided.
- Additional files: strictly filter (e.g., `*.sparkres.yaml`), parse on-change, attach comparers when needed.

Determinism (generators)
- Use `ImmutableValueArray<T>` for pipeline models and equality semantics.
- Sort all inputs prior to emission; avoid nondeterministic iteration (e.g., enumerating `Dictionary`/`HashSet`).

Common anti-patterns
- Do not modify user code.
- No I/O in transforms (AdditionalTexts is the exception).
- No mutable static state in generators.
- Do not store non-equatable types in pipeline models.
- Do not depend on nondeterministic operations or clock/time/random.


## Testing (authoritative)
- Unit tests: TUnit.
- Generator tests: Verify snapshot tests live under `tests/**/TestResults`.
- Expectations for generator changes:
    - Deterministic output; order-insensitivity where specified.
    - Incrementality: unrelated changes should no-op.
    - Snapshots are updated by the developer (not by the LLM agent).


## Documentation Governance
Because systems change frequently, separate “authoritative” docs from “planning” docs:
- Use a front-matter or banner convention to mark each page’s status explicitly (e.g., `status: authoritative` or `status: planning`).
- Prefer keeping normative rules in a small set of authoritative docs (like this file, and key generator/SDK readmes).
- When code and docs conflict, code is the source of truth; open a follow-up task to align docs.

## Do / Don’t (checklist)
Do:
- Fully qualify types in generated code; sort inputs before emit.
- Treat nullability warnings as errors; mark truly-optional DI deps as nullable in signatures.
- Keep generator transforms pure; report errors via analyzers.
- Ask questions early for ambiguous requirements.

Don’t:
- Don’t run `dotnet` commands.
- Don’t throw in generators; don’t perform I/O (except AdditionalTexts).
- Don’t introduce nondeterminism; don’t depend on transient environment state.
- Don’t modify existing user code when emitting generated code.


## Build Properties (reference)
Compiler-visible properties consumed by generators (no additions planned right now):
- `ModName`
- `ModIdentifier`
- `RootNamespace`
- `SgOutputNamespace`
- `DisableLogEnrichmentGenerator` (default: enrichment enabled)


## Current Limitations (context)
- State system design is in-flight; transitional vs gameplay states and ordering rules are evolving.
- ECS and Vulkan code are stubs and out-of-scope for now.
- Registry phases are removed; state-driven registration is the model.


## Agent Workflow (when implementing changes)
1. Confirm scope and whether the change is authoritative or planning.
2. For generators:
    - Add/change models using `ImmutableValueArray<T>`.
    - Ensure analyzers cover invalid input; make generator return no output on invalid input.
    - Keep transforms pure; sort inputs; fully qualify output types.
3. For SDK/targets:
    - Keep tasks deterministic; don’t embed environment-specific paths beyond allowed inputs/props.
4. For docs:
    - Mark doc status (authoritative/planning); keep authoritative docs concise and normative.
5. Validate locally by reasoning about tests and determinism; do not attempt to run `dotnet`.
6. Submit focused patches; avoid unrelated changes.
 
