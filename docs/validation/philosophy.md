# Validation Philosophy

Validation is the gate between "Aletheia can compute this" and "Aletheia may use this as evidence."
The project deliberately distinguishes mathematical correctness, methodological safeguards,
historical validation, and demonstrated economic value.

## Four Different Claims

| Claim type | What it means | Example |
| --- | --- | --- |
| Mathematical correctness | The formula or algorithm is implemented coherently and tested. | Log returns use positive NAV ratios. |
| Methodological safeguard | The workflow blocks a known research error. | Training data ends at the cutoff. |
| Historical validation | A model performed out of sample on historical events. | Walk-forward Brier score beats baseline. |
| Economic value | A decision process survived costs, delay, and comparison baselines. | Delayed timing path outperforms buy-and-hold after costs. |

!!! danger "Do not collapse these claims"
    A calibrated probability is not proof of profit. A good backtest is not a guarantee. A
    `ReliabilityIndex` is not the probability that the next signal is correct.

## Implemented Safeguards

| Risk | Implementation boundary | Test coverage |
| --- | --- | --- |
| Look-ahead in forecasts | `WalkForwardEvaluator` trains on historical prefixes. | `tests/Aletheia.Validation.Tests/WalkForwardEvaluatorTests.cs` |
| Future mutation affecting old state-space forecasts | State-space adapter refits at cutoff and tests cutoff immutability. | `tests/Aletheia.Validation.Tests/StateSpaceForecastModelTests.cs` |
| Label leakage in market timing | Training labels require known `EndIndex`; purge/embargo are applied. | `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs` |
| Horizon mismatch | Forecast ensembles require same-horizon Arena evidence. | `tests/Aletheia.Forecasting.Tests/ForecastEnsembleTests.cs` |
| Comparing models on different samples | Model Arena reports metric-family common support. | `tests/Aletheia.Validation.Tests/ModelArenaTests.cs` |
| Treating missing timing features as zeros | Feature keys remain absent when causal evidence is unavailable. | `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs` |
| Ledger mutation | Predictions and evaluations use separate content fingerprints. | `tests/Aletheia.Persistence.Tests/SqlitePredictionLedgerTests.cs` |
| Economic look-ahead | Backtester executes after configured observation delay. | `tests/Aletheia.Simulation.Tests/TimingDecisionBacktesterTests.cs` |

## Boundary of Protection

These safeguards apply to the implemented univariate fund-history workflows. They do not prove that
the selected fund universe is free from survivorship bias, that a strategy has production capacity,
or that future markets will resemble the validation sample.
