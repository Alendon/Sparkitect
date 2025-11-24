---
status: planning
title: Internal TODO (Unlisted)
---

Note: This file is for personal/internal tracking of potential follow-ups. It is not authoritative and should not be added to any TOC.

- Documentation alignment
  - Clarify that registry “phases” are obsolete; registries run on state transitions.
  - Document that KeyedFactory keys are restricted to `string` or `Sparkitect.Modding.Identification`.
  - State that DevLauncher is required for IDE runs and not packaged into the mod.

- Generators/Analyzers
  - Consider tightening analyzers/templates to enforce key types (string/Identification) and remove acceptance of OneOf.

- State system docs
  - Add authoritative guidance for SG-provided state/registry facades and discovery/ordering once the state system is finalized.

- Docs governance
  - Adopt front-matter/status conventions (authoritative vs planning) across docs pages.

