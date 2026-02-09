---
uid: sparkitect.tooling.mod-specification
title: Mod Specification
description: Format specification for Sparkitect mod archives and manifests
---

# Sparkitect Mod Specification

This document specifies the format for Sparkitect mod archives and manifests.

## Archive Format

### File Extension

Mod archives use the `.sparkmod` extension.

### Archive Layout

A `.sparkmod` file is a standard ZIP archive containing:

```
modname.sparkmod/
  manifest.json      # Mod metadata (required, at root)
  ModName.dll        # Main mod assembly (required, at root)
  lib/               # Dependency DLLs (optional)
    SomeDependency.dll
  resources/         # Mod resources (optional)
    textures/
    shaders/
```

## Manifest Format

### manifest.json

The manifest.json file contains mod metadata in JSON format.

#### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string | Yes | Unique mod identifier (snake_case) |
| Name | string | Yes | Display name |
| Description | string | Yes | Mod description |
| Version | string | Yes | Semantic version (e.g., "1.0.0") |
| Authors | string[] | Yes | List of author names |
| Relationships | Relationship[] | No | Mod dependencies and incompatibilities |
| ModAssembly | string | Yes | Main assembly filename |
| RequiredAssemblies | string[] | No | List of dependency DLL filenames |
| IsRootMod | boolean | No | Whether this mod can be loaded directly |

#### Relationship Object

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string | Yes | Target mod identifier |
| VersionRange | string | Yes | Acceptable version range (semver) |
| IsOptional | boolean | No | Whether this dependency is optional (default: false) |
| IsIncompatible | boolean | No | Whether this marks an incompatibility (default: false) |

#### Relationship Semantics

- **Required dependency** (default): `IsOptional=false, IsIncompatible=false`
  - The target mod MUST be present and loaded
  - Loading fails if dependency is missing

- **Optional dependency**: `IsOptional=true, IsIncompatible=false`
  - The target mod MAY be present
  - Mod loads successfully even if dependency is missing
  - Use `IsModLoaded()` API to check at runtime

- **Incompatibility**: `IsIncompatible=true`
  - The target mod MUST NOT be present
  - Loading fails if incompatible mod is detected

#### Version Range Format

Version ranges follow semantic versioning:

| Pattern | Meaning |
|---------|---------|
| `*` | Any version |
| `1.0.0` | Exactly 1.0.0 |
| `^1.0.0` | Compatible with 1.0.0 (>=1.0.0 <2.0.0) |
| `~1.0.0` | Approximately 1.0.0 (>=1.0.0 <1.1.0) |
| `>=1.0.0` | 1.0.0 or higher |
| `>=1.0.0 <2.0.0` | Range |

### Example Manifest

```json
{
  "Id": "background_color_mod",
  "Name": "Background Color",
  "Description": "Modifies Pong background color",
  "Version": "1.0.0",
  "Authors": ["Alendon"],
  "Relationships": [
    {
      "Id": "sparkitect",
      "VersionRange": "^1.0.0",
      "IsOptional": false,
      "IsIncompatible": false
    },
    {
      "Id": "pong_mod",
      "VersionRange": "^1.0.0",
      "IsOptional": false,
      "IsIncompatible": false
    },
    {
      "Id": "color_provider_mod",
      "VersionRange": "^1.0.0",
      "IsOptional": true,
      "IsIncompatible": false
    }
  ],
  "ModAssembly": "BackgroundColorMod.dll",
  "RequiredAssemblies": [],
  "IsRootMod": false
}
```

## Versioning

The manifest format is considered stable. No version field is included in the manifest as the format is not expected to change in backward-incompatible ways.

## Runtime Loading

The Sparkitect runtime loads mods from directories specified via `-addModDirs` CLI argument. Each directory is scanned for `.sparkmod` files.

Loading order:
1. Parse all manifests
2. Validate dependencies (fail if required deps missing)
3. Build dependency graph
4. Load in topological order (dependencies first)

---
*Specification version: 1.0*
*Last updated: Phase 18.1*
