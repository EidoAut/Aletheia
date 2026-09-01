# Research Reports

Aletheia can generate Markdown research reports from the desktop shell or through the CLI.

## CLI Usage

```powershell
dotnet run --project src/Aletheia.Cli -- report sample
dotnet run --project src/Aletheia.Cli -- report examples/sample-fund.csv
dotnet run --project src/Aletheia.Cli -- report --provider cnmv-iic --fund ES0000000000
```

The report command runs standard analysis, Model Arena, market timing, and report synthesis.

## Desktop Usage

Load a dataset, optionally run Model Arena for the desired horizon, then select `Generate Report`.
The desktop writes a Markdown report with dataset metadata, scientific version, score, signal,
timing evidence, forecasts, validation audit, and warnings.

## How To Read a Report

1. Confirm dataset provenance and latest effective observation date.
2. Review data quality and freshness.
3. Read the long-run fund score separately from current attractiveness.
4. Inspect Model Arena and ensemble audit before trusting a directional summary.
5. Treat warnings and `NO CALL` states as first-class results.

## Related Pages

- [Decision Signals](../concepts/decision-signals.md)
- [Scoring](../scoring.md)
- [Validation Philosophy](../validation/philosophy.md)
