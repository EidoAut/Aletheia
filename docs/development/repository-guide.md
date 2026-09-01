# Repository Guide

The repository is organized as a .NET solution with source projects under `src/`, tests under
`tests/`, documentation under `docs/`, and build/publish scripts under `scripts/`.

## Important Root Files

| File | Purpose |
| --- | --- |
| `Aletheia.sln` | Complete solution. |
| `Directory.Build.props` | Shared .NET build settings and product package version. |
| `global.json` | SDK resolver policy. |
| `README.md` | Short entry point and project overview. |
| `CHANGELOG.md` | Product and scientific change history. |
| `mkdocs.yml` | Explicit documentation-site navigation and rendering configuration. |
| `requirements-docs.txt` | Pinned Python documentation dependencies. |

## Source Layout

Use [Project Structure](../architecture/project-structure.md) as the source project map.

## Generated Outputs

| Path | Created by | Commit? |
| --- | --- | --- |
| `bin/`, `obj/` | .NET build/test | No |
| `artifacts/` | publish/packaging scripts | No |
| `data/` | local prediction ledger/cache workflows | No |
| `site/` | `mkdocs build` | No |

Generated outputs should stay out of source archives and normal commits.

## Documentation Source of Truth

The implementation is the source of truth. Documentation should describe actual code paths, options,
limits, and tests. Do not add a user-facing promise unless a corresponding implementation and
validation boundary exist.
