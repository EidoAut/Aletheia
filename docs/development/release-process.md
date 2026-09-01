# Release Process

Aletheia keeps product and scientific versions separate.

## Version Locations

| Version | Location |
| --- | --- |
| Product/package version | `Directory.Build.props` and `AletheiaRelease.ProductVersion` |
| Scientific version | `AletheiaRelease.ScientificVersion` |

Do not change either version for documentation-only work unless the release policy explicitly calls
for it.

## Release Checklist

1. Update version constants and build properties when behavior or packaging warrants it.
2. Add `CHANGELOG.md` entries describing product and scientific changes separately.
3. Update documentation pages affected by behavior changes.
4. Run `./scripts/build.ps1 -Configuration Release`.
5. Run `mkdocs build --strict`.
6. Publish the desktop package with `./scripts/publish-desktop.ps1`.
7. Confirm GitHub Pages deployment if documentation changed.

## Scientific Versioning

Use the scientific version for methodological changes that affect reproducibility, data semantics,
validation rules, forecast content, prediction identity, or decision-signal interpretation.
