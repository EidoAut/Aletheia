# Forecast Metrics

Aletheia reports metrics by forecast capability. A metric is shown only when the model supports the
required output.

## Point Metrics

| Metric | Definition |
| --- | --- |
| MAE | Mean absolute error. |
| MSE | Mean squared error. |
| RMSE | Square root of MSE. |
| Directional accuracy | Fraction of predictions whose direction matched the realized direction. |

## Probability Metrics

| Metric | Meaning |
| --- | --- |
| Brier score | Mean squared error of probability forecasts for binary positive-return events. Lower is better. |
| Expected calibration error | Gap between predicted probability bins and observed frequencies. |
| Brier skill | Improvement relative to the selected probability baseline. |

## Quantile and Interval Metrics

| Metric | Meaning |
| --- | --- |
| Pinball loss | Quantile forecast loss for requested percentiles. |
| Interval coverage | Whether realized returns fall inside predicted intervals at expected rates. |

## Interpretation

No single metric is the final truth. The Arena reports several views because models can excel at one
capability and fail another.

## Related Mathematical Notes

- [MAE, MSE, and RMSE](../mathematics/validation/mae-rmse.md)
- [Directional Accuracy](../mathematics/validation/directional-accuracy.md)
- [Brier Score](../mathematics/validation/brier-score.md)
- [Pinball Loss](../mathematics/validation/pinball-loss.md)
- [Interval Coverage](../mathematics/validation/interval-coverage.md)
