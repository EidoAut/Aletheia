# Desktop Charting

Aletheia Desktop uses `ScottPlot.WinForms` 5.1.x for native WinForms plotting.

## Dependency boundary

ScottPlot is referenced only by `Aletheia.Desktop`. Plotting types do not appear in the mathematical, domain, validation or application projects. Desktop wrappers consume presentation models such as `DatedValue`, `StateProjectionPoint`, `AnaloguePath` and validation samples.

## Chart controls

- `TimeSeriesChartControl`
- `DistributionChartControl`
- `DrawdownChartControl`
- `ScatterChartControl`
- `SpectrumChartControl`
- `ForecastChartControl`
- `CalibrationChartControl`

All wrappers inherit the shared `AletheiaChartControl` frame. Each chart is displayed inside a rounded analytical card with:

- an external title;
- a concise axis/context caption;
- a dark plot surface;
- consistent axes, grid and legend styling;
- stronger default line hierarchy;
- an explicit empty-state overlay.

Keeping the title outside ScottPlot reduces plot-area clutter and produces the same visual hierarchy across time series, histograms, spectra, simulations and validation diagnostics.

## Deferred rendering

Analytical pages are populated only after the selected page is attached to the content panel. `AletheiaChartControl` keeps a pending-refresh flag and retries rendering when its handle is created, visibility changes, its parent changes or a usable size is assigned. Refresh requests are coalesced to avoid repeated `BeginInvoke` calls during layout.

This prevents charts from being populated while detached or zero-sized and then remaining blank when the user opens the page.
