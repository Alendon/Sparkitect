---
uid: sparkitect.rendergraph.internals-ledger-epochs
title: Internals — Ledger and Epochs
description: The declaration ledger as single compile truth, and the symbolic epoch chains it records
---

# Internals: Ledger and Epochs

> [!NOTE]
> This page describes engine internals, not the mod-author surface. Authors declare Read and Increment through descriptions; see <xref:sparkitect.rendergraph.descriptions-and-moments>.

The [`DeclarationLedger`](xref:Sparkitect.Graphing.Ledger.DeclarationLedger) is the single source of compile truth: it stores ledger nodes and per-resource epoch chains, records the Read, Increment, and moment-read edges, and mints opaque [`ResourceRef<T>`](xref:Sparkitect.Graphing.Ledger.ResourceRef`1)s eagerly with symbolic — unresolved — epoch positions. The full resource graph is reconstructable from declarations alone, without executing a frame, which is what lets compilation run entirely off the ledger.

An [`Epoch`](xref:Sparkitect.Graphing.Ledger.Epoch) is a symbolic position in a resource's intra-frame dataflow, not a resolved integer at collect time — authored code never sees an epoch number. Each resource starts at its base epoch, which is holdable but never readable because it has no producing increment; every increment advances the chain one symbolic step. Concrete ordinals are assigned only during compilation.

## See Also

- <xref:sparkitect.rendergraph.internals-compilation> for how symbolic epochs resolve to ordinals
- <xref:sparkitect.rendergraph.internals-resource-model> for the facts the ledger stores
- <xref:sparkitect.rendergraph.data-flow-ordering> for the author-facing Read/Increment ordering rules
- <xref:sparkitect.rendergraph.requirements> for the ledger-as-truth rationale
