# Brier Score

## Purpose

Brier score evaluates probability forecasts for `R > 0`.

## Formula

$$
y_i =
\begin{cases}
1, & R_i > 0,\\
0, & R_i \le 0
\end{cases}
$$

$$
\mathrm{BS} = \frac{1}{N}\sum_{i=1}^{N}(p_i-y_i)^2
$$

## Interpretation

Lower is better. Perfect is `0`; worst possible is `1`.

## Assumptions

Predicted probabilities must be finite values in `[0, 1]`, and the model must advertise `ForecastCapabilities.ProbabilityPositive`. Invalid probabilities are rejected rather than silently clamped.

## Limitations

Brier score combines calibration and sharpness. It should be read alongside calibration bins and point-forecast metrics. Point-only models report Brier as `N/A`.

## Implementation Notes

`BrierScoreCalculator` averages per-prediction Brier contributions for probability-capable samples. The default probability baseline is `Historical Probability Climatology`, which uses completed training-window horizon outcomes only.
