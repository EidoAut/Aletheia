# Risk Analysis

Risk pages summarize realized downside and return-distribution behavior. They consume the loaded NAV
history and derived simple/log return series.

## Important Metrics

| Metric | Interpretation |
| --- | --- |
| Annualized volatility | Scaled return variability under the detected observation-frequency convention. |
| Maximum drawdown | Worst peak-to-trough loss in the historical series. |
| Drawdown duration | Time spent below a prior high when available. |
| Sortino ratio | Return per unit of downside volatility. |
| Skewness and kurtosis | Shape diagnostics, not trading signals. |

## What Conclusions Are Justified

Risk analysis can identify unstable histories, severe historical losses, unusual return shapes, and
data that is too limited for strong interpretation.

## What Not To Conclude

Do not conclude that low realized volatility means low future risk. Do not treat drawdown recovery
patterns as guaranteed. Do not mix irregular and regular annualization conventions without checking
the detected cadence.

## Related Scientific Pages

- [Volatility](../mathematics/volatility.md)
- [Drawdown](../mathematics/drawdown.md)
- [Observation Frequency](../concepts/observation-frequency.md)
