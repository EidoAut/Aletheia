# Milestone 2.4

Milestone 2.4 closes the first product-hardening pass over the native desktop shell and activates the previously isolated simulation subsystem.

## Delivered

- EIDO Automation / Aletheia code-rendered brand mark and graphite/amber technical palette.
- Global indeterminate progress indicator for fund search, dataset loading, simulation and Model Arena execution.
- Busy-state locking and cancellation recovery across the shell.
- Deferred ScottPlot refresh when pages receive a handle, become visible or acquire a usable size.
- Lazy page population so charts are rendered after the selected page is attached to the visual tree.
- Explicit empty states for forecast, validation, Model Arena and prediction-ledger surfaces.
- Deterministic periodic-investment simulation with initial capital, monthly contributions, percentile trajectories and downside-to-contributions probability.
- Scientific transparency for historical moments, monthly scaling, seed and methodology.
- Simulation access from both WinForms and CLI.
- Workload and non-finite-input guards.
- Holiday-tolerant business-daily cadence detection without interpolation or forward-fill.
- Central product/scientific release identifiers used by provider and prediction metadata.
- Central release identifiers, build/publish scripts and self-contained Windows x64 CI packaging.

## Scientific boundary

The new simulator is an IID Gaussian historical-moment baseline. It is not admitted to Model Arena as a predictive model, does not produce a trade signal and excludes fees, taxes and inflation. These constraints are displayed in the UI, CLI and mathematical documentation.

## Verification boundary

The repository includes regression tests for the new application and simulation paths. The canonical verification command is `./scripts/build.ps1`; CI runs it on Windows before publishing the desktop ZIP.
