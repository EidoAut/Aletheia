# Getting Started Overview

Aletheia studies investment funds from their NAV history. It loads observations, preserves
provenance, computes descriptive and dynamic diagnostics, runs forecasts, validates models through
walk-forward tests, and surfaces conservative decision language for human review.

!!! warning "Research output"
    Aletheia is not a broker, robo-advisor, or guarantee engine. It can say that a fund looks
    historically strong, that evidence is weak, or that a timing model is not reliable enough.

## What You Can Do First

| Task | Recommended entry point |
| --- | --- |
| Try Aletheia without external data | [First Analysis](first-analysis.md) |
| Use the native research shell | [Desktop Application](../user-guide/desktop-application.md) |
| Run repeatable command-line analyses | [CLI Reference](../reference/cli-reference.md) |
| Understand signals before acting on them | [Decision Signals](../concepts/decision-signals.md) |
| Check scientific rigor | [Validation Philosophy](../validation/philosophy.md) |

## Main Surfaces

=== "Desktop"
    ```powershell
    dotnet run --project src/Aletheia.Desktop
    ```

    The desktop shell is the best surface for exploratory use. It provides fund discovery,
    grouped analytical pages, charts, configurable Model Arena horizon selection, cancellation,
    and Markdown report generation.

=== "CLI"
    ```powershell
    dotnet run --project src/Aletheia.Cli -- sample
    ```

    The CLI is best for repeatable runs, automated checks, and fast inspection from a terminal.

## Repository Versions

| Version type | Value | Source |
| --- | --- | --- |
| Product version | `2.7.3` | `Directory.Build.props`, `AletheiaRelease.ProductVersion` |
| Scientific version | `2.12.0-causal-horizon-integrity` | `AletheiaRelease.ScientificVersion` |

The scientific version is stamped into reproducibility metadata and prediction content. The product
version is the user-facing package/application version.
