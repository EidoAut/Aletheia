# Simulation

Simulation is a scenario tool. It explores possible investment-plan outcomes using historical
moments or bootstrap paths, but it does not create a validated forecast claim by itself.

## CLI Usage

```powershell
dotnet run --project src/Aletheia.Cli -- simulate sample --initial 1800 --monthly 100 --years 10 --paths 5000
dotnet run --project src/Aletheia.Cli -- simulate examples/sample-fund.csv --initial 10000 --monthly 0 --years 5
dotnet run --project src/Aletheia.Cli -- simulate --provider cnmv-iic --fund ES0000000000 --initial 1800 --monthly 100 --years 10
```

Available simulation options are `--initial`, `--monthly`, `--years`, `--paths`, and `--seed`.

## Desktop Usage

Open the Simulation page after loading a dataset, set initial capital, monthly contribution,
investment horizon, and path count, then run the baseline scenario.

## Interpretation

The output includes contributed capital, terminal-value percentiles, trajectory percentiles, and the
probability that terminal value finishes below contributions. The CLI always prints `NO CALL` for
this section because scenario distribution is not a recommendation.

## Related Pages

- [Simulation Science Note](../simulation.md)
- [Investment-Plan Simulation](../mathematics/investment-plan-simulation.md)
