---
uid: sparkitect.ecs
title: ECS Module
description: Entity Component System framework and emergent features
---

# ECS Module

**Status:** Design — modeling the framework before implementation.

The ECS follows the same design philosophy as the rest of Sparkitect: minimal structural
contracts at the core, concrete behavior emerges from implementations. The pattern that drives
Stateless Functions, Scheduling, and the Registry system applies here in the same way.

## Documentation

- <xref:sparkitect.ecs.ecs-requirements> — Design decisions and constraints
- <xref:sparkitect.ecs.ecs-design> — Semantic design model covering core contracts, emergent
  features, and how they relate

## Design Approach

The ECS is designed by modeling concrete emergent features first (archetype storage,
relationships, spatial queries), then extracting the minimal framework contracts those
features require. The core is shaped by real usage rather than speculative abstraction.
