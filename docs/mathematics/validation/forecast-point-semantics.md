# Forecast Point Semantics

## Purpose

Point forecast metrics must state which statistic is being evaluated.

## Loss Semantics

For squared-error loss, the population-optimal forecast is generally:

\[
\hat{y}_{\mathrm{MSE}} = \mathbb{E}[Y\mid X]
\]

For absolute-error loss, the population-optimal forecast is generally:

\[
\hat{y}_{\mathrm{MAE}} = \operatorname{Median}(Y\mid X)
\]

Aletheia does not yet optimize separately for every loss family. Each forecast records a `PointForecastStatistic` so MAE/RMSE diagnostics identify the principal point value being scored.

## Current Policy

Empirical and AR forecasts use the forecast mean as the primary point statistic. Zero return uses an explicit model point. Probability climatology has no point forecast.

Directional classification is separately controlled by `DirectionPredictionRule`, so direction does not silently mean `sign(ExpectedReturn)` for every model.
