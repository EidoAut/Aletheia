# Changelog

## Documentation

- Added a complete Material for MkDocs Aletheia Wiki with explicit navigation, search, math rendering, Mermaid diagrams, EIDO/Aletheia styling, and GitHub Pages deployment.
- Added user, concept, model, validation, architecture, development, and reference documentation while preserving the existing scientific notes.
- Updated the root README to point to the Wiki as the canonical documentation entry point.
- Product version remains 2.7.3; scientific version remains `2.12.0-causal-horizon-integrity`.

## Scientific 2.12.0 - Causal Horizon Integrity

- Removed timing-label look-ahead by filtering training labels on realized `EndIndex`, including purging and embargo, instead of using the out-of-sample label's future `TimeToEvent`.
- Added cutoff evaluation for market timing so future NAV mutations after a historical prediction cutoff cannot affect features, labels, calibration, OOD distance, or ensemble output.
- Rebuilt volatility-scaled timing labels around causal volatility availability: unavailable volatility no longer falls back to an artificial fixed threshold or a hard-coded `0.01` path.
- Replaced raw-unit analogue timing distance with robust median/MAD feature scaling and propagated the same robust distance into OOD/reliability penalties.
- Changed forecast ensembling to require same-horizon validation evidence and to invert a deterministic weighted mixture CDF for quantiles instead of averaging member percentile values.
- Preserved Kalman level/trend covariance through forecasts, updated GARCH variance state between refits, and separated HMM filtered online probabilities from smoothed full-sample posteriors.
- Made classifier optimization status explicit: reaching `MaxIterations` is reported as a non-successful usable diagnostic instead of being labeled converged.
- Added sample-evidence, calibration, skill uncertainty, weight concentration, disagreement, and OOD factors to timing `ReliabilityIndex`.
- Marked unconditional competing-risk hazards and spectral timing as non-independent ensemble candidates until they have causal OOS feature reconstruction.
- Added nested walk-forward selection and final frozen holdout protocol helpers for hyperparameter selection and untouched final evaluation.
- Added regression tests for label availability, cutoff immutability, robust analogue scaling, mixture quantiles, horizon-specific ensemble evidence, dynamic-state recursion/covariance/filtering, classifier status, nested selection, and final holdout splitting.
- Scientific version is now `2.12.0-causal-horizon-integrity`; product/package version remains 2.7.3.

## Scientific 2.10.0 - Causal Market Timing Hardening

- Stopped backfilling current spectral and ensemble evidence into historical market-timing feature vectors. Causally unavailable external features are now absent, not neutral-filled.
- Added explicit feature availability helpers and updated classifier, analogue distance and OOD detection paths to avoid treating missing timing features as observed zeros.
- Corrected calendar-day triple-barrier semantics: incomplete calendar horizons no longer become `NoBarrierHit`, and calendar valuation now uses the first observation on or after the requested target date.
- Ensured ensemble eligibility by requiring a non-negative bootstrap lower bound for Brier skill and estimating bootstrap block size from event dependence instead of hard-coding five observations.
- Renamed market-timing presentation language to `ReliabilityIndex`; it remains a heuristic validation-quality index and is not displayed as probability of a correct call.
- Added an economic timing backtester with delayed execution, transaction costs, slippage, turnover, cumulative/annualized return, volatility, Sharpe, Sortino, drawdown and Calmar metrics.
- Hardened the CNMV provider and cache with bounded streaming downloads, content-type checks, ZIP entry/size/ratio limits, DTD-prohibited XML loading, SHA-256 cache metadata, atomic writes and corrupt-cache recovery.
- Corrected buy-and-hold timing so it is invested from the first NAV, made the initial fixed-exposure cost explicit, and aligned backtest annualization with regular-frequency or elapsed-calendar irregular conventions.
- Integrated historical OOS timing predictions into an economic chain from decision date to delayed execution and comparable Aletheia/buy-and-hold/neutral outcomes; insufficient OOS evidence now reports `NO RELIABLE ECONOMIC BACKTEST`.
- Updated the CNMV downloader to use the current `descarga-informacion-individual?ejercicio=YYYY&lang=es` route, scope monthly IIC links to the download table, validate ZIP signatures before cache writes, and invalidate contaminated cache entries before one refetch.
- Added deterministic provider/backtester/application regression tests for redirected CNMV pages, HTML-with-200 responses, bad content types, corrupt and missing-XML ZIPs, ZIP bomb limits, valid and contaminated cache, exact delayed execution, costs, exposure clamping, liquidation/reentry, irregular annualization, and the no-reliable economic gate.
- Product/package version is now 2.7.3; scientific version is now `2.10.0-causal-market-timing`.

## Scientific 2.9.0 - Probabilistic Market Timing

- Added a probabilistic market-timing engine with triple-barrier event labels, causal feature generation, competing-risk hazards, regularized event classification, regime-transition timing, historical analogue timing, spectral timing candidates, and baseline-relative ensemble weighting.
- Added market-timing assessment models, deterministic explanations, CLI `timing` output, report integration, and a native WinForms `Market Timing` page.
- Added causality and calibration regression tests for timing labels, features, classifiers, hazards, arena behavior, application orchestration, and desktop navigation.
- Scientific version is now `2.9.0-probabilistic-market-timing`; product/package version remains 2.7.2.

## 2.7.2

- Rewrote the WinForms designer partials as designer-serializable control graphs: no runtime helper factories, interpolated strings, or manually computed controls inside `InitializeComponent`.
- Promoted shell and page layout elements to named fields so Visual Studio can load, select, move and persist them on the design surface.
- Moved dynamic release-version text back to the runtime partial while keeping static design-time placeholders.
- Kept `WorkspacePageBase` concrete and parameterlessly instantiable. Scientific calculations are unchanged.

## 2.7.1

- Made `WorkspacePageBase` concrete and parameterlessly instantiable so the WinForms designer can create the inherited design surface for every analytical page.
- Replaced the abstract `PageTitle` and `SetWorkspace` contract with designer-safe virtual defaults; all runtime pages keep their existing overrides.
- Added a regression test asserting that the shared workspace page base is non-abstract and can be instantiated on an STA thread.
- Scientific calculations are unchanged; scientific version remains `2.5.0-irregular-cadence`.

## 2.7.0

- Refactored the WinForms desktop shell and all analytical pages into designer-backed partial classes with `*.Designer.cs` and `*.resx` companions.
- MainForm now contains application/navigation behavior only; its static control hierarchy lives in `MainForm.Designer.cs`.
- Fund Discovery, Overview, Performance, Risk, Simulation, Dynamics, Spectral, Analogues, Forecast, Model Arena, Validation, Predictions, and Aletheia Lab are grouped as editable WinForms UserControls in Visual Studio.
- Header actions are ordinary designer-declared WinForms `Button` controls with native caption rendering while preserving the dark EIDO/amber visual language.
- Added designer-safe parameterless constructors where runtime services/delegates are normally injected.
- Scientific calculations and data semantics are unchanged; scientific version remains `2.5.0-irregular-cadence`.


## 2.6.6

- Replaced the bespoke header-caption renderer with the same `AletheiaButton` rendering path already proven by visible actions such as Search and Open CSV.
- Header actions now inherit the shared `TextRenderer.DrawText` implementation instead of maintaining a separate GDI+/native caption path.
- Scientific calculations and data semantics are unchanged.
- The product/package version is 2.6.6.

## 2.6.5

### Fixed

- Replaced the header actions with a fully owner-drawn `Control`; native `Button`/`Label` caption rendering is no longer used.
- The visible caption is painted explicitly with GDI+ from an immutable constructor value, so `Funds`, `Sample`, `Open CSV`, `Run Arena`, and `Cancel` cannot disappear because of a native text-rendering path.
- Preserved rounded states, hover/pressed feedback, disabled-state contrast, keyboard activation, focus cues, accessibility metadata, and DPI-aware geometry.
- Added a desktop regression assertion for the immutable rendered caption.

### Reproducibility

- The product/package version is 2.6.5.
- The scientific version remains `2.5.0-irregular-cadence`; this hotfix changes desktop rendering only.

## 2.6.4

- Replaced the header action surface/child-label composition with direct native Label controls.
- Header captions are assigned directly to the native label controls at construction time, avoiding the failed parent-to-child caption propagation path.
- Kept keyboard activation, hover/pressed feedback, accessibility roles, and operation-state behavior.
- The product/package version is 2.6.4; the scientific version remains unchanged.

## 2.6.3

### Fixed

- Rendered every header-action caption through a dedicated GDI+ label layered above the button surface, bypassing both owner-drawn TextRenderer and native Button text paths.
- Kept the complete action surface clickable and preserved hover, pressed, disabled, keyboard, DPI, and rounded-border behavior.
- Added the product version to the window title so a stale executable is immediately identifiable.

### Reproducibility

- The product/package version is 2.6.3.
- The scientific version remains `2.5.0-irregular-cadence`; this hotfix changes desktop rendering only.

## 2.6.2

### Fixed

- Replaced owner-drawn header commands with dedicated native-text WinForms buttons so Funds, Sample, Open CSV, Run Arena, and Cancel remain visible on the affected display configuration.
- Preserved the existing dark hierarchy, hover states, DPI-scaled layout, rounded bounds, keyboard focus, and disabled-state contrast.

### Reproducibility

- The product/package version is 2.6.2.
- The scientific version remains `2.5.0-irregular-cadence`; this hotfix changes desktop rendering only.

## 2.6.1

### Fixed

- Replaced the regressed percentage-row header action layout with a DPI-scaled, explicitly positioned action strip.
- Made Funds and Sample fully bordered secondary actions and increased disabled-button text contrast so all four header commands remain legible.
- Added desktop smoke checks for the text and renderable bounds of every header action.

### Reproducibility

- The product/package version is 2.6.1.
- The scientific version remains `2.5.0-irregular-cadence`; this hotfix changes desktop layout and rendering only.

## 2.6.0

### Fixed

- Rebuilt the shell header with a taller shared header/brand row, deterministic page-title and dataset-metadata rows, and zero implicit label margins so text is not clipped at high DPI.
- Increased the sidebar width and made the vector wordmark scale from the active monitor DPI, including automatic fitting of the EIDO Automation caption.
- Kept the four-action header grid centered vertically while preserving the complete Run Arena/Cancel slot at the minimum supported window size.
- Added desktop smoke assertions that compare the rendered single-line label heights with their measured text height and verify the brand remains above its minimum bounds.

### Reproducibility

- The product/package version is 2.6.0.
- The scientific version remains `2.5.0-irregular-cadence`; this release changes desktop layout and rendering only.

## 2.5.3

### Fixed

- Prevented the primary header action from being clipped at the right edge by reserving a deterministic four-column action area and overlaying Run Arena/Cancel in one stable slot.
- Replaced vertically stretched Fund Discovery secondary actions with compact centered buttons and restored breathing room above the status bar.
- Allowed provider histories with genuinely irregular timestamps to complete analysis instead of failing during annualization.

### Scientific semantics

- Added an explicit elapsed-time annualization estimator based on observed intervals divided by actual calendar span; source observations remain unchanged and no missing dates are synthesized.
- Propagated the effective irregular cadence through risk metrics, dynamic-state volatility, periodic-investment simulation, current forecasts and walk-forward Model Arena horizon resolution.
- Improved cadence detection for dense calendar/business-daily histories, weekly reports and monthly reports with isolated missing periods while retaining genuinely uneven data as irregular.
- The product/package version is 2.5.3.
- The scientific version is `2.5.0-irregular-cadence` because annualization and horizon-resolution semantics changed.

## 2.5.2

### Fixed

- Removed the unused broad `ScottPlot` namespace import from `AletheiaChartControl`, so Windows Forms `Font`, `FontStyle` and related drawing types resolve unambiguously.
- Corrected static UI-builder calls in `SimulationPage` and `TheoryPanel`; static members are no longer accessed through `this`.
- Restored generation of the `Aletheia.Desktop` reference assembly, eliminating the downstream `CS0006` reported by `Aletheia.Desktop.Tests` once the desktop project builds.

### Reproducibility

- The product/package version is 2.5.2.
- The scientific version remains `2.4.0-milestone2.4`; this hotfix changes only compile-time name resolution and method invocation.

## 2.5.1

### Fixed

- Qualified the Windows Forms `Label` controls explicitly in `AletheiaChartControl`, removing the ambiguity with `ScottPlot.Label`.
- Restored compilation of `Aletheia.Desktop`; the downstream `CS0006` error in `Aletheia.Desktop.Tests` disappears once the desktop reference assembly is generated.

### Reproducibility

- The product/package version is 2.5.1.
- The scientific version remains `2.4.0-milestone2.4`; this hotfix changes only source-name resolution.

## 2.5.0

### Redesigned

- Replaced the narrow utility navigation with a grouped 232-pixel analytical sidebar, persistent Aletheia/EIDO identity and an explicit scientific-mode footer.
- Rebuilt the application header around separate page context, active-dataset context and hierarchical actions.
- Reworked fund discovery into a structured start dashboard with a primary catalogue workflow, research-principle card and secondary sample/CSV entry points.
- Introduced reusable rounded surfaces, owner-drawn action and navigation buttons, status indicators, an indeterminate activity bar, metric cards and titled data-grid cards.
- Wrapped every ScottPlot view in a consistent chart card with contextual captions, stronger hierarchy and explicit empty states.
- Reorganized Overview, Performance, Risk, Simulation, Dynamics, Spectral, Analogues, Forecast, Model Arena, Validation, Predictions and Aletheia Lab around the shared design system.
- Rebuilt the investment-simulation configuration and theory-reference panels so parameters, assumptions and output diagnostics are visually separated.

### Accessibility and interaction

- Preserved keyboard navigation, focus cues, accessible names and high-DPI layout behavior in the owner-drawn controls.
- Hardened the minimum-size layout so sidebar scrolling does not introduce horizontal chrome and simulation status remains visible without maximizing the window.
- Kept cancellation, fund loading, page switching, prediction-ledger refresh and Model Arena behavior behind the existing application-service boundaries.

### Reproducibility

- The product/package version is 2.5.0.
- The scientific version remains `2.4.0-milestone2.4`; this release changes presentation and interaction only.

## 2.4.1

### Fixed

- Relaxed SDK resolution so any .NET SDK from 8.0.100 onward can load the solution, preferring the nearest compatible .NET 8 SDK and falling back to a later major only when necessary.
- Allowed prerelease SDKs as a local fallback for Visual Studio Preview installations.
- Kept CI reproducible by installing the latest stable .NET 8 SDK explicitly.

### Reproducibility

- The product/package version is 2.4.1.
- The scientific version remains `2.4.0-milestone2.4` because this patch changes only toolchain resolution, not calculations, datasets, forecasts, metrics or simulation semantics.

## 2.4.0

### Added

- Periodic-investment Monte Carlo scenario service, desktop page and CLI command.
- Monthly percentile trajectory, terminal downside probability and transparent scaled-moment diagnostics.
- EIDO Automation / Aletheia product mark and corporate technical palette.
- Indeterminate operation progress and shared busy-state control.
- Reproducible build, test and desktop-publish scripts plus Windows CI.

### Fixed

- Charts that could remain blank when populated before their page became visible.
- Stale forecast, validation, arena and prediction-ledger visual state after workspace changes.
- Fund search controls remaining disabled after successful searches.
- Business-daily provider series being misclassified as irregular because of isolated weekday gaps such as market holidays.
- Provider and validation metadata retaining stale hard-coded product versions.
- Cancellation and recoverable errors leaving shell controls or coarse state inconsistent.

### Scientific constraints

- The investment simulator remains an IID Gaussian baseline and emits no investment signal.
- Irregular observation cadence requires an explicit future convention and is rejected.
- Workloads above 12,000,000 path-months are rejected to protect interactive use.
