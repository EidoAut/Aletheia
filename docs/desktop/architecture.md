# Desktop Architecture

`Aletheia.Desktop` is a native Windows Forms analytical shell. It references `Aletheia.Application` and presentation-facing contracts, while the mathematical projects remain UI independent.

## Shell

`MainForm` owns:

- the grouped sidebar and navigation state;
- page and dataset context in the header;
- hierarchical dataset/model actions;
- the analytical content panel;
- application status and custom progress indication;
- coarse application state;
- cancellation for long-running operations;
- fund discovery;
- lazy page population and deferred chart refresh.

Normal analysis flow is:

```text
WinForms event
    -> AletheiaApplicationService
    -> mathematical/domain services
    -> FundWorkspace
    -> page controls
```

Fund discovery flow is:

```text
StartPage query
    -> AletheiaApplicationService.SearchFundsAsync
    -> provider catalog abstraction
    -> result grid
    -> AletheiaApplicationService.LoadProviderWorkspaceAsync
    -> FundWorkspace
```

No mathematical calculations live in click handlers. Heavy operations run through `async`/`await` and `Task.Run` so the UI remains responsive.

## Design-system boundary

Page classes arrange analytical content but do not implement drawing primitives. Shared appearance and interaction live in:

```text
ThemePalette / ControlStyler / DrawingUtilities
        -> SurfacePanel / buttons / status controls
        -> KPI, grid and chart cards
        -> analytical pages
```

This keeps page-level changes focused on evidence and model output while preserving a consistent shell.

## Pages

Implemented pages:

- Overview
- Performance
- Risk
- Simulation
- Dynamics
- Spectral
- Analogues
- Forecast
- Model Arena
- Validation
- Predictions
- Aletheia Lab

Pages consume `FundWorkspace`, `FundAnalysisResult`, `ModelArenaResult` and prediction-ledger presentation models.

## State

The shell uses a coarse `AppViewState`:

- `NoDataset`
- `Loading`
- `Searching`
- `Analyzing`
- `AnalysisAvailable`
- `ArenaRunning`
- `ArenaAvailable`
- `Error`

The current workspace is kept in memory and reused when switching pages. Pages are updated when they become the active visual surface instead of all being populated while detached. This keeps expensive controls lazy and gives ScottPlot a valid handle and size before refresh.

Simulation execution follows the same shell-owned operation pattern as provider loading and Model Arena. The page supplies a request; `MainForm` owns cancellation, progress, error recovery and logging; `AletheiaApplicationService` maps the result back to presentation models.
