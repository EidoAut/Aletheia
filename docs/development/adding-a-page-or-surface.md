# Adding a Page or Surface

Presentation surfaces should consume `Aletheia.Application` models rather than reaching directly into
provider parsing or low-level math details.

## Desktop Page Checklist

1. Add a designer-backed page under `src/Aletheia.Desktop/Pages`.
2. Keep static control layout in `*.Designer.cs`.
3. Keep runtime behavior in the page partial.
4. Expose a stable `PageTitle`.
5. Implement `SetWorkspace` and `SetArena` as needed.
6. Use shared controls such as `DataGridCardControl`, `MetricStripControl`, `KpiControl`, and
   `AletheiaChartControl`.
7. Add empty and insufficient-evidence states.
8. Add desktop tests for layout or state behavior when risk is nontrivial.
9. Update [Desktop Application](../user-guide/desktop-application.md) and
   [Desktop Architecture](../architecture/desktop-architecture.md).

## CLI Surface Checklist

1. Derive command behavior from `AletheiaApplicationService`.
2. Add parsing in `src/Aletheia.Cli/Program.cs`.
3. Emit explicit usage errors for invalid arguments.
4. Preserve conservative wording for unavailable evidence.
5. Update [CLI Reference](../reference/cli-reference.md).
