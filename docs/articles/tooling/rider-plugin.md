---
uid: sparkitect.tooling.rider-plugin
title: Rider Plugin
description: Early proof-of-concept Rider plugin for navigating Sparkitect registrations
---

# Rider Plugin

The Rider plugin adds navigation between Sparkitect registrations and the generated identification symbols they produce. It is an early proof of concept with a small feature set: Go to Registration, Find Usages redirection, and `.sparkres.yaml` navigation. There is no settings UI and no packaging beyond a manually built plugin archive.

## Install

The plugin is not published to a marketplace. Build the archive and install it from disk.

Building needs a JDK 21 and the .NET SDK. The first build downloads the matching Rider SDK. From `tools/RiderPlugin/`:

```bash
./gradlew buildPlugin
```

The build compiles the ReSharper backend (a `dotnet msbuild` of `src/dotnet/Sparkitect.RiderPlugin.slnx`), assembles the frontend, and writes a plugin archive under `build/distributions/`.

Install it in Rider:

1. **Settings → Plugins**
2. The gear menu → **Install Plugin from Disk…**
3. Select the archive from `build/distributions/`
4. Restart Rider when prompted

The plugin targets Rider 2026.1.x and requires a restart to load.

## Go to Registration

**Go to Registration** is an editor action added to the **Go To** context submenu, next to Go to Definition. Invoke it on a generated identification usage (an ID-tree leaf such as `SomeCategoryID.MyMod.MyEntry`) and it navigates to the site that registered that identity. The destination is a single target, not a multi-target popup: either the C# id-string literal on the registration attribute, or the entry key in a resource file.

It recognizes four registration surfaces:

| Surface | Registration site the action lands on |
|---------|----------------------------------------|
| Type registration | The registered concrete type that owns the identification |
| Value providers | The method or property member that supplies a registered value |
| External functions | Stateless functions (per-frame, transition) and ECS system functions |
| Resource keys | The entry key in a `.sparkres.yaml` file |

Detection reads the registration category and the generated `[RegisteredFrom]` coordinate from structured metadata; it does not guess from type or namespace names.

## Find Usages

Run **Find Usages** on a registration identifier — a C# id-string literal on a `[Register…]` attribute, or a scalar in a `.sparkres.yaml` file — and the search redirects to the generated `{Mod}{Category}IDs.{Entry}` identity the identifier names. You get the identity's usages directly instead of first navigating to the leaf yourself.

The redirect keeps the identity's search domain whole, so results compose across surfaces: the leaf's C# usages and every `.sparkres.yaml` scalar that resolves to the same identity appear together in one result set.

## YAML Resource Navigation

The plugin maps `*.sparkres.yaml` onto a Rider-backed file type so backend references and highlightings render in the editor. Plain `*.yaml` files keep the stock YAML editor.

In a `.sparkres.yaml` file, **Go to Declaration** (F12) from an entry-ID scalar navigates to the registration site the entry names, crossing the C#/YAML boundary through the same symbol table the C# reference uses.

Recognized scalars are highlighted under a shared, configurable inspection severity: the top-level `{registry}.{method}:` keys and the entry-ID scalars nested beneath them. The highlight tooltip states that F12 navigates to the registration site and Find Usages lists the consumers.

## GSM Debugger Tool Window

The GSM Debugger is a debugger-time tool window that discovers running engine processes through a per-user rendezvous file, then opens a socket connection to the selected process. It displays the live composed module-set stack for that process, with per-frame stateless functions and their scheduling-origin badges. Double-clicking a row navigates to the module class or the stateless function method, through the same registration-metadata lookup Go to Registration uses.

## Explorer Tool Window

The Explorer is a solution-scoped, stateless tree view of every registered category and entry across all loaded mod projects. It is independent of any running game process — it walks static registration metadata rather than opening a socket — and refreshes on solution-wide compilation changes. Rows navigate to their registration site the same way the Debugger's rows do.

## Build/Live Boundary

Go to Registration, Find Usages redirection, YAML resource navigation, and the Explorer all work without a running game process — they read static registration metadata from the compiled solution. The GSM Debugger is the one feature that requires a live process: it has nothing to show without an active socket connection.

## Native-IDE Doctrine

The plugin extends existing Rider mechanisms — the Go To submenu, Find Usages, F12, standard highlighting severities — rather than inventing custom UI where an existing IDE action already fits. New tool windows exist only where no native Rider concept covers the need, which is why the Debugger and Explorer are dedicated windows while registration navigation rides the standard Go To/Find Usages actions.

## See Also

- <xref:sparkitect.core.registry-system> for how registrations produce identifications
- <xref:sparkitect.tooling.sdk> for building mods that carry these registrations
