# First Analysis

The fastest way to understand Aletheia is to run the bundled sample dataset. It avoids provider
network access and exercises the standard analysis pipeline.

## Run the Sample

```powershell
dotnet run --project src/Aletheia.Cli -- sample
```

This command loads deterministic sample NAV data and prints:

- dataset identity, observation count, source count, cadence, and provenance;
- data-quality diagnostics;
- performance and risk metrics;
- dynamic-state diagnostics;
- AR(1), spectral, analogue, and current forecast summaries;
- a final signal section.

!!! tip "Read the signal last"
    The signal is only meaningful after reading quality, validation, and warnings. Aletheia is
    designed so weak evidence can end in `NO CALL` instead of forcing a direction.

## Generate a Research Report

```powershell
dotnet run --project src/Aletheia.Cli -- report sample
```

The report path runs the standard analysis, Model Arena, market-timing assessment, and Markdown
report writer. It is slower than `sample` because validation work is included.

## Inspect Market Timing

```powershell
dotnet run --project src/Aletheia.Cli -- timing sample
dotnet run --project src/Aletheia.Cli -- backtest sample
```

`timing` reports current triple-barrier probabilities and validation diagnostics. `backtest`
converts historical out-of-sample timing predictions into a delayed economic simulation when enough
usable signals exist.

## Use the Desktop

```powershell
dotnet run --project src/Aletheia.Desktop
```

From the first screen:

1. Choose `Sample` to load the bundled dataset.
2. Review `Overview`, `Performance`, and `Risk`.
3. Open `Forecast` and `Market Timing` for current probabilistic evidence.
4. Set the Model Arena horizon in the header and run `RUN <days>D`.
5. Use `Generate Report` to save a Markdown research report.

## What Not To Conclude

- The sample run does not prove a profitable strategy.
- A forecast distribution is not a certainty.
- A tentative label such as `BUY?` or `SELL?` is not a validated recommendation.
- `NO RELIABLE ECONOMIC BACKTEST` is a valid result, not a failure of the program.
