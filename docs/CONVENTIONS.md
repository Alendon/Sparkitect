# Documentation Conventions

## Adding a New Module

1. Create folder: `docs/articles/{module-name}/` (kebab-case, lowercase)
2. Create `toc.yml` listing all pages in reading order
3. Create `index.md` (20-40 lines) with module overview
4. Add topic pages (one per system area)
5. Add module to `docs/articles/toc.yml`
6. Run `docfx build` in `docs/` to verify no broken links

## File Conventions

- **Filenames**: kebab-case, `.md` extension (e.g., `shader-compilation.md`)
- **Folders**: kebab-case, lowercase (e.g., `articles/vulkan/`)

## YAML Frontmatter (required on every article)

```yaml
---
uid: sparkitect.{module}.{page-name}
title: Page Title
description: One-line description
---
```

UID format: `sparkitect.{module}` for index pages, `sparkitect.{module}.{page-name}` for topic pages.

Top-level articles (not inside a module folder) use `sparkitect.getting-started.{page}` as a pseudo-module pattern (e.g., `sparkitect.getting-started.intro`, `sparkitect.getting-started.overview`).

## Linking

- **Inter-article links**: Always use `<xref:sparkitect.{module}.{page}>` syntax
- **Custom link text**: `[Link Text](xref:sparkitect.{module}.{page})`
- **API type references**: `<xref:Full.Namespace.TypeName>` on first mention per page
- **NEVER** use relative file paths for inter-article links

## Writing Style

- Concise and scannable
- Code examples over prose
- If something can be said in 2 sentences, say it in 2 sentences
- Mix of short focused pages (~50-150 lines) and medium system pages

## Module Overview Pages (index.md)

- 20-40 lines
- Module purpose in 1-2 sentences
- Links to all topic pages using xref
- UID: `sparkitect.{module}`

## Sidebar Ordering

- Ordered logically for reading, not alphabetically
- Each module starts with overview (index.md)
- Group by system area within modules
