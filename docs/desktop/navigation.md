# Desktop Navigation

The desktop shell uses a persistent analytical sidebar and a contextual top header.

## Startup

First launch opens Fund Discovery. The start dashboard presents official fund search as the primary workflow and keeps Sample/Open CSV as secondary entry points. Model Arena does not run automatically.

The sidebar keeps the Aletheia/EIDO identity visible and groups destinations by intent:

```text
Portfolio
  Overview
  Performance
  Risk
  Simulation

Research
  Dynamics
  Spectral
  Analogues

Models
  Forecast
  Market Timing
  Model Arena
  Validation
  Predictions

System
  Aletheia Lab
```

## Header

The header separates three kinds of context:

1. the current page and its purpose;
2. the active dataset, identifier, currency, provider, observation count and frequency;
3. actions ordered by emphasis.

Available actions are:

- Funds
- Sample
- Open CSV
- Generate Report
- Run Arena
- Cancel

The header also exposes the calendar-day horizon selector used by Model Arena. `Ctrl+O` opens the CSV
picker, `Ctrl+L` opens Fund Discovery and `F5` refreshes the visible page from the in-memory
workspace.

## State and cancellation

During provider search, dataset loading, simulation or Model Arena execution, the shell:

- displays a custom indeterminate activity bar;
- keeps the current analytical page visible;
- disables competing navigation and actions;
- leaves Cancel available;
- restores the prior analytical state after cancellation or recoverable failure.

Late results are rejected through the existing operation identity and workspace-reference checks, so an older request cannot overwrite a newer fund.
