---
uid: sparkitect.rendergraph
title: Render Graph
description: Render graph module for managed rendering pipelines and GPU resource lifecycle
---

# Render Graph

**Status:** Requirements gathering — collecting stakeholder requirements before design.

The render graph provides the stock engine layer for GPU rendering pipelines: pass execution
ordering, frame-resolved resource views, command recording orchestration, synchronization, and
data flow between rendering stages. The foundational layer stays small; resource and GPU
semantics live in stock or extension-defined graph contracts.

Sparkitect does not ship game-specific rendering passes. You build the pipeline using stock render
graph components and your own passes. Mods extend it by adding passes, injecting effects, or
registering compatible resource views and handlers.

## Documentation

- <xref:sparkitect.rendergraph.requirements> — Stakeholder requirements and design constraints
