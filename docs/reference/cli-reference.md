# CLI Reference

All examples are derived from `src/Aletheia.Cli/Program.cs` in product version `2.7.3`.

## Source Selection

Most analytical commands accept one of these source forms:

| Form | Example |
| --- | --- |
| Sample dataset | `sample` or no source argument where supported |
| CSV file | `examples/sample-fund.csv` or `analyze examples/sample-fund.csv` for the default command |
| Provider fund | `--provider cnmv-iic --fund ES0000000000 [--from yyyy-MM-dd] [--to yyyy-MM-dd]` |

## Commands

| Command | Purpose | Example |
| --- | --- | --- |
| default sample | Standard analysis on sample data. | `dotnet run --project src/Aletheia.Cli -- sample` |
| `analyze` | Standard analysis from CSV or provider. | `dotnet run --project src/Aletheia.Cli -- analyze examples/sample-fund.csv` |
| `funds search` | Search configured fund catalogs. | `dotnet run --project src/Aletheia.Cli -- funds search mediolanum` |
| `score` | Print fund score/report score summary. | `dotnet run --project src/Aletheia.Cli -- score sample` |
| `forecast` | Print current forecast runs. | `dotnet run --project src/Aletheia.Cli -- forecast sample` |
| `timing` | Print market-timing assessment. | `dotnet run --project src/Aletheia.Cli -- timing sample` |
| `arena` | Run Model Arena. | `dotnet run --project src/Aletheia.Cli -- arena sample` |
| `backtest` | Run timing economic backtest. | `dotnet run --project src/Aletheia.Cli -- backtest sample` |
| `simulate` | Run periodic-investment scenario simulation. | `dotnet run --project src/Aletheia.Cli -- simulate sample --initial 1800 --monthly 100 --years 10 --paths 5000` |
| `stress` | Print deterministic stress scenarios. | `dotnet run --project src/Aletheia.Cli -- stress sample` |
| `report` | Print full research report plus timing assessment. | `dotnet run --project src/Aletheia.Cli -- report sample` |
| `predictions list` | List recent ledger predictions. | `dotnet run --project src/Aletheia.Cli -- predictions list` |
| `predictions show` | Show one ledger prediction and evaluations. | `dotnet run --project src/Aletheia.Cli -- predictions show <prediction-id>` |

## Fund Search

```powershell
dotnet run --project src/Aletheia.Cli -- funds search <name-or-isin>
dotnet run --project src/Aletheia.Cli -- funds search --isin <isin>
```

## Simulation Options

| Option | Default | Meaning |
| --- | --- | --- |
| `--initial` | `1800` | Initial capital. |
| `--monthly` | `100` | Monthly contribution. |
| `--years` | `10` | Investment horizon in years. |
| `--paths` | `5000` | Monte Carlo path count. |
| `--seed` | `161803` | Deterministic simulation seed. |

## Backtest Options

| Option | Default | Meaning |
| --- | --- | --- |
| `--cost` | `0.001` | Transaction cost rate. |
| `--slippage` | `0.0005` | Slippage rate. |
| `--delay` | `1` | Execution delay in observations. |
| `--max-exposure` | `1` | Maximum gross exposure. |
| `--periods-per-year` | Auto | Optional annualization override. |
| `--no-initial-cost` | Off | Do not charge the initial fixed-exposure cost. |

## Environment Variables

| Variable | Purpose |
| --- | --- |
| `ALETHEIA_LEDGER_PATH` | Override the default SQLite prediction ledger path. |

## Conservative Output

The CLI can print `NO RELIABLE SIGNAL`, `NO CALL`, or `NO RELIABLE ECONOMIC BACKTEST`. These are
valid outputs and should not be treated as errors.
