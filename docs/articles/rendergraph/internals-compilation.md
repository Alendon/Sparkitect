---
uid: sparkitect.rendergraph.internals-compilation
title: Internals — Compilation
description: The staged Link phase that turns the ledger into a GPU-free compiled plan, with diagnostics returned as DU cases
---

# Internals: Compilation

> [!NOTE]
> This page describes engine internals, not the mod-author surface. Authors encounter compilation only as the diagnostics in <xref:sparkitect.rendergraph.data-flow-ordering>.

Compilation is the ledger's Link phase: [`GraphCompiler`](xref:Sparkitect.Graphing.Compile.GraphCompiler)`.Link()` returns a `Result<CompiledPlan, CompileError>`, and every structural diagnostic — [`Fork`, `Cycle`, `UnproducibleRead`, `UndefinedMoment`, `DuplicateMoment`, `DescriptionReuse`](xref:Sparkitect.Graphing.Compile.CompileError) — is returned as a DU case carrying its provenance, never thrown. Link is staged: detect fork and unproducible reads, bind each referenced moment to its single marked increment, build the ordering graph from Read edges plus increment anti-dependencies, run a deterministic topological sort, then resolve the symbolic epochs into a plan.

The resulting [`CompiledPlan`](xref:Sparkitect.Graphing.Compile.CompiledPlan) is GPU-free: ordered nodes plus each chain's epochs resolved to concrete ordinals, with no barrier, layout, or command-recording concerns. Turning that plan into Vulkan barriers and command recording is L2's job, downstream of the core.

## See Also

- <xref:sparkitect.rendergraph.internals-ledger-epochs> for the symbolic epochs Link resolves
- <xref:sparkitect.rendergraph.internals-graphing-core> for why the plan stays GPU-free
- <xref:sparkitect.rendergraph.data-flow-ordering> for the author-facing view of the diagnostics
- <xref:sparkitect.rendergraph.requirements> for the compile-pipeline rationale
