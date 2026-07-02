---
uid: sparkitect.rendergraph.data-flow-ordering
title: Data-Flow Ordering
description: How Read and Increment edges in the ledger derive pass execution order
---

# Data-Flow Ordering

Execution order is derived entirely from the Read and Increment edges recorded during declaration. You declare data usage, not execution order — the graph computes a topological order from the edges and never asks you for a pass index.

Two rules produce every ordering edge:

- **Every Read orders the reader after the epoch's producing increment.** A pass that reads an epoch runs after the pass that produced it.
- **Every Increment orders after its source epoch and after that epoch's declared readers.** The reader edge is an anti-dependency: an increment cannot advance a resource while an earlier reader is still consuming it, so the backing is safe to reuse.

The anti-dependency is what makes epoch advancement safe without copies. If pass A reads epoch 1 and pass B increments epoch 1 to epoch 2, B orders after A — B cannot clobber the value A is reading.

## No Access Modes

There are no access-mode enums (read/write/read-write) in the ordering model. Ordering comes from Read and Increment alone. Richer access information — image layouts, pipeline stages, access masks — lives in each resource type's static metadata and is consumed by synchronization hooks, never by ordering.

## Edge Semantics

The compile validates structure before deriving order. Each violation is a structured [`CompileError`](xref:Sparkitect.Graphing.Compile.CompileError), returned as a `Result`, never thrown:

| Situation | Outcome |
|-----------|---------|
| Read of an epoch with a producing increment | Reader orders after the producer |
| Read of an epoch with no producing increment (e.g. the base epoch) | `UnproducibleRead` |
| A cycle in the derived order | `Cycle`, naming the participating nodes |
| Two increments from one source epoch (a fork) | `Fork` — hard error |
| A referenced moment with no marked increment | `UndefinedMoment` |
| Two increments marked with the same moment | `DuplicateMoment` |

A fork is a hard error because two writers of one epoch are a data race the model refuses to express. Multi-writer is structurally inexpressible: to branch, declare new resources on a shared epoch rather than incrementing one epoch twice. Each epoch has exactly one producing increment and one linear successor.

## Determinism

Among nodes with no ordering constraint between them, the graph breaks ties by declaration (mint) order. The emitted order is deterministic regardless of the order edges were inserted, so a rebuild that declares the same usage produces the same schedule.

## Explicit Ordering Is a Smell

The model supports supplementary class-level ordering metadata as an escape hatch. Treat it as a smell and a migration target: if a pass needs explicit ordering, the data dependency it actually has should be expressed as a Read or an Increment instead. Ordering derived from data flow stays correct when mods restructure the pipeline; hand-written ordering does not.

## See Also

- <xref:sparkitect.rendergraph.pass-authoring> for where Read/Increment are declared
- <xref:sparkitect.rendergraph.descriptions-and-moments> for the two-relation grammar
- <xref:sparkitect.rendergraph.requirements> for the compile-pipeline rationale
