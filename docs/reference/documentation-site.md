# Documentation Site

This Wiki is built with MkDocs and Material for MkDocs.

## Local Preview

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs serve
```

Open the local URL printed by MkDocs, usually:

```text
http://127.0.0.1:8000
```

## Strict Build

```powershell
mkdocs build --strict
```

The generated `site/` directory is not source documentation and should not be committed. It is a
static site that can be served from any basic HTTP server.

!!! note "Offline assets"
    MathJax and Mermaid are pinned and bundled under `docs/javascripts/vendor/` so generated pages
    can render formulas and diagrams without depending on a public CDN. Opening complex generated
    pages directly with `file://` may still be less reliable than serving `site/` over local HTTP.

## GitHub Pages Publication

The repository includes `.github/workflows/docs.yml`. To publish:

1. In the GitHub repository, open `Settings`.
2. Open `Pages`.
3. Set `Build and deployment` source to `GitHub Actions`.
4. Push to the default branch or run the `Docs` workflow manually.

The workflow checks navigation coverage, validates local Markdown links, scans for unfinished
markers, builds with strict mode, and deploys the generated site through GitHub Pages.

## Raw Markdown

All content remains readable directly under `docs/`. Relative links between documentation pages are
kept valid for GitHub browsing as well as the generated MkDocs site.
