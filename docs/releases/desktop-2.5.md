# Aletheia Desktop 2.5

Aletheia 2.5 is a presentation-only product release over scientific Milestone 2.4. It does not change datasets, formulas, simulation assumptions, forecast capabilities, validation metrics, prediction identity or Model Arena ranking.

## Interface direction

The previous shell exposed the right functions but read visually like a collection of default WinForms controls. Release 2.5 introduces a coherent quantitative-research workstation:

- a persistent 232-pixel sidebar with grouped destinations;
- separate page and active-dataset context in the header;
- clear primary, secondary, ghost and cancellation actions;
- reusable rounded analytical surfaces instead of loose controls;
- metric cards with restrained semantic accents;
- chart cards with titles, axis context and explicit empty states;
- titled grid cards with section rows for long diagnostic outputs;
- a structured fund-discovery dashboard;
- redesigned simulation and theory-reference workspaces.

## Design system

The desktop design system is implemented without a third-party UI framework. The reusable primitives are:

- `SurfacePanel`
- `AletheiaButton`
- `NavigationButton`
- `ActivityBarControl`
- `StatusDotControl`
- `KpiControl`
- `MetricStripControl`
- `DataGridCardControl`
- `AletheiaChartControl`

`ThemePalette`, `ControlStyler` and `DrawingUtilities` keep color, spacing and drawing behavior out of page-level analytical code.

## Interaction behavior

The redesign preserves the existing application boundaries and asynchronous execution model. Dataset loading, provider search, periodic-investment simulation, Model Arena and prediction-ledger reads still run through `AletheiaApplicationService`. The shell continues to own cancellation, stale-operation protection, progress and recoverable error state.

Owner-drawn navigation and action controls retain focus cues, keyboard activation and accessible names. Layout remains dock-based and uses `PerMonitorV2` DPI awareness.
The sidebar and simulation workspace are also sized to remain usable at the declared minimum window dimensions, including when navigation requires vertical scrolling.

## Versioning

- Product version: `2.5.0`
- Scientific version: `2.4.0-milestone2.4`

The unchanged scientific version makes clear that the release does not modify quantitative semantics or reproducibility metadata.
