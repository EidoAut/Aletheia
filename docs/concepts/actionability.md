# Actionability

Actionability answers whether a visible direction is usable under current evidence. It is separate
from the direction itself.

## Factors That Reduce Actionability

- stale latest observation date;
- no validation-gated ensemble;
- insufficient timing evidence;
- severe out-of-distribution state;
- model disagreement;
- tentative strategic label;
- unavailable or failed model outputs;
- no reliable economic backtest.

## Data Freshness

Application options define freshness thresholds:

| Option | Default |
| --- | --- |
| `FreshDataMaxAgeDays` | 45 calendar days |
| `ActionableDataMaxAgeDays` | 75 calendar days |

A stale dataset can retain historical value while losing current actionability.

## OOD and Disagreement

Out-of-distribution detection asks whether the current feature vector is far from historical training
support. Model disagreement asks whether eligible models point in materially different directions.
Both should reduce confidence.

## Related Pages

- [Decision Signals](decision-signals.md)
- [Uncertainty and Probability](uncertainty-and-probability.md)
- [Validation Limitations](../validation/limitations.md)
