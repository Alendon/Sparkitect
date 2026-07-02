---
uid: sparkitect.ecs
title: ECS Module
description: Framework-first Entity Component System with capability-driven storage, source-generated queries, and concurrent system scheduling
---

# ECS Module

The ECS is a framework-first Entity Component System: the core defines minimal structural contracts, and storage, identity, iteration, and scheduling all emerge from implementations that plug into those contracts. It follows the same design mentality as the rest of Sparkitect — a type-safe protocol at the core, concrete behavior in the pieces built on top.

You write components, queries, and systems as small declarations. Source generators turn them into typed entity handles, capability filters, and a concurrent execution plan.

## Usage

Read these in order:

- <xref:sparkitect.ecs.components> — declare component structs and register them.
- <xref:sparkitect.ecs.worlds-storage> — create a world, add archetype storage, spawn entities.
- <xref:sparkitect.ecs.queries> — declare component queries and iterate matched entities.
- <xref:sparkitect.ecs.systems> — write systems, group them, order and toggle their execution.
- <xref:sparkitect.ecs.command-buffers> — defer structural changes through command buffers.

## Internals

Engine internals, separate from the mod-author usage pages:

- <xref:sparkitect.ecs.internals-architecture> — the three layers and how they map to the namespaces.
- <xref:sparkitect.ecs.internals-capabilities> — the capability contract and filter resolution.
- <xref:sparkitect.ecs.internals-storage> — the reference SoA storage.
- <xref:sparkitect.ecs.internals-query-sg> — what the query source generator emits.
- <xref:sparkitect.ecs.internals-scheduling> — system ordering and the sequential executor.

## Design

- <xref:sparkitect.ecs.ecs-requirements> — design decisions and constraints, condensed to intent.
- <xref:sparkitect.ecs.ecs-design> — the layered design model and forward-looking work.
