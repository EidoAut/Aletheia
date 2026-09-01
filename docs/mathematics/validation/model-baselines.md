# Model Baselines

## Purpose

Different metric families may require different baselines.

## Point Baseline

The point baseline is `Zero Return`. It provides a deterministic zero simple-return point forecast.

## Probability Baseline

The probability baseline is `Historical Probability Climatology`. It estimates `P(return > 0)` from completed horizon outcomes inside the training window only:

\[
p_t = \frac{\#\{i:R_{i,h}>0\}}{N}
\]

It is a Brier/calibration baseline and does not claim point forecasts or quantiles.

## Implementation Notes

`ModelArenaOptions` selects `PointForecastBaselineModelId` and `ProbabilityBaselineModelId`. Baseline selection is by model id, not registration order.
