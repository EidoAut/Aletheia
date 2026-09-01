# Market Timing

The Market Timing page presents probabilistic triple-barrier evidence and a conservative
investor-facing timing label.

## What It Consumes

- NAV history and returns;
- causal timing features;
- per-horizon triple-barrier labels;
- model-arena timing candidates;
- calibration, OOD, disagreement, and sample-evidence diagnostics;
- optional economic backtest results.

## What It Displays

| Area | Meaning |
| --- | --- |
| Action guide | Visible label such as `BUY?`, `HOLD?`, `SELL?`, or `NO CALL`. |
| Timing windows | Upside, downside, and no-barrier probabilities by horizon. |
| Why | Evidence, counter-evidence, warnings, and deterministic reasons. |
| Advanced diagnostics | Model weights, calibration, OOD, hazards, and reliability. |
| Economic backtest | Historical OOS decisions converted to delayed exposure paths. |

## Interpretation

The engine estimates events, not certainty. `ReliabilityIndex` measures validation support after
penalties. It is not the probability that the timing label will be correct.

!!! warning "Tactical is not strategic"
    Market timing describes current conditions over configured horizons. It does not replace the
    long-run fund score or the separate strategic decision signal.

## Related Pages

- [Probabilistic Market Timing](../market-timing.md)
- [Decision Signals](../concepts/decision-signals.md)
- [Economic Backtesting](../validation/economic-backtesting.md)
