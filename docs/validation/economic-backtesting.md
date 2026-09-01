# Economic Backtesting

Economic backtesting is separate from probability validation. It asks whether historical
out-of-sample timing decisions would have survived execution delay, transaction costs, slippage, and
comparison baselines.

## CLI Usage

```powershell
dotnet run --project src/Aletheia.Cli -- backtest sample
dotnet run --project src/Aletheia.Cli -- backtest examples/sample-fund.csv --cost 0.001 --slippage 0.0005 --delay 1
dotnet run --project src/Aletheia.Cli -- backtest --provider cnmv-iic --fund ES0000000000 --from 2024-01-01 --to 2024-12-31
```

Options are `--cost`, `--slippage`, `--delay`, `--max-exposure`, `--periods-per-year`, and
`--no-initial-cost`.

## Execution Semantics

The backtester first realizes returns into the execution NAV using the previous exposure. It then
charges costs and changes exposure for later intervals. This prevents a signal from benefiting from
the return that precedes its execution.

## Comparisons

| Path | Meaning |
| --- | --- |
| Aletheia timing | Exposure path driven by historical out-of-sample timing signals. |
| Buy-and-hold | Full exposure from the first NAV, with initial fixed-exposure cost by default. |
| Neutral/no-action | Zero exposure and no trades. |

## `NO RELIABLE ECONOMIC BACKTEST`

If there are too few usable historical out-of-sample timing decisions, Aletheia reports
`NO RELIABLE ECONOMIC BACKTEST`. That is the correct output when the evidence is insufficient.

## Source and Tests

- Source: `src/Aletheia.Simulation/TimingDecisionBacktester.cs`,
  `src/Aletheia.Application/AletheiaApplicationService.cs`
- Tests: `tests/Aletheia.Simulation.Tests/TimingDecisionBacktesterTests.cs`,
  `tests/Aletheia.Application.Tests`

Related science note: [Economic Timing Backtesting](../backtesting.md).
