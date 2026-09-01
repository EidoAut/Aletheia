# Desktop Architecture

`Aletheia.Desktop` is a WinForms shell over `Aletheia.Application`. The static visual hierarchy lives
in designer-backed partial classes, while runtime behavior stays in code-behind partials.

## Page Map

| Page | Purpose |
| --- | --- |
| Start/Fund Discovery | Search providers, load sample, open CSV. |
| Overview | Investor-facing summary and dataset context. |
| Performance | Historical return metrics and charts. |
| Risk | Return distribution and downside diagnostics. |
| Simulation | Periodic-investment scenario baseline. |
| Dynamics | Current dynamic state and state-path context. |
| Spectral | FFT and spectral stability diagnostics. |
| Analogues | Similar historical states and forward outcomes. |
| Forecast | Current forecast runs and distribution details. |
| Market Timing | Triple-barrier probabilities, decision language, and economic backtest. |
| Model Arena | Walk-forward model comparison. |
| Validation | Validation metrics and eligibility. |
| Predictions | Prediction ledger summaries and details. |
| Aletheia Lab | Research map and theory articles. |

## Runtime Behavior

The shell keeps a single `FundWorkspace` in memory. Pages receive that workspace and optional Arena
results through `SetWorkspace` and `SetArena`. Long operations use cancellation tokens and UI state
transitions so late or cancelled results do not overwrite the current fund.

## Visual Identity

The desktop palette uses near-black graphite surfaces, cyan analytical accents, amber warning/product
accents, and restrained green/red state colors. This Wiki reuses that identity through Material for
MkDocs and `docs/stylesheets/aletheia.css`.

Related engineering notes:

- [Desktop Architecture](../desktop/architecture.md)
- [Desktop Theme](../desktop/theme.md)
- [Desktop Navigation](../desktop/navigation.md)
