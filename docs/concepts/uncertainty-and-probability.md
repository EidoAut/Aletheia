# Uncertainty and Probability

Aletheia reports probability and uncertainty because single-point forecasts are rarely enough for
financial interpretation.

## Probability Outputs

| Output | Meaning |
| --- | --- |
| `P(Return > 0)` | Estimated probability that terminal horizon return is positive. |
| `ProbabilityUp` | Estimated probability that the upside barrier is hit first. |
| `ProbabilityDown` | Estimated probability that the downside barrier is hit first. |
| `ProbabilityNoEvent` | Estimated probability that neither barrier is hit before the vertical horizon. |
| Quantiles | Estimated points in a terminal return distribution, such as P10 or P90. |

## Calibration

Calibration asks whether probabilities match observed frequencies. If a model predicts 70% often,
roughly 70% of those events should occur over a well-supported evaluation set. Calibration is useful,
but it is not the same as profitability.

## ReliabilityIndex

`ReliabilityIndex` is a validation-quality index. It is penalized by weak sample evidence, model
disagreement, calibration error, uncertainty in skill, OOD distance, and weight concentration. It is
not the chance that Aletheia will be right.

## Related Pages

- [Calibration](../validation/calibration.md)
- [Forecast Metrics](../validation/forecast-metrics.md)
- [Market Timing](../market-timing.md)
