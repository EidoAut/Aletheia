# Simulation page

The desktop Simulation page exposes the periodic-investment baseline through the shared application layer.

## Configuration workspace

A dedicated left card shows:

- the active dataset and its essential metadata;
- initial capital;
- monthly contribution;
- investment horizon;
- Monte Carlo path count;
- a high-emphasis Run baseline scenario action;
- an explicit methodology warning.

Each field includes a concise semantic description. The warning states that the output is a baseline distribution rather than a validated forecast.

## Results workspace

The result area contains:

- contributed capital, median, P10, P90 and mean terminal values;
- probability of finishing below contributed capital;
- monthly contributed-capital, P10, median and P90 trajectories;
- a sectioned diagnostics card covering dataset, plan, return scaling, distribution and scientific discipline;
- dataset fingerprint, seed, cadence conversion and historical/scaled moment assumptions;
- an explicit `NO CALL` interpretation.

Results are cleared whenever the dataset fingerprint changes, preventing a scenario from one fund from remaining visible after another fund is loaded.

## Execution

The shell owns execution, cancellation, logging and global busy state. While a scenario runs, competing navigation and actions are disabled, Cancel remains available and both the global status bar and local page state report progress.

The page intentionally does not infer a buy or sell decision. See `docs/mathematics/investment-plan-simulation.md` for formulas and limitations.
