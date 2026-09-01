# Adding a Model

A forecast model belongs in `Aletheia.Validation` when it participates in Model Arena.

## Required Contract

Implement `IForecastModel` with:

- stable `ModelDescriptor`;
- deterministic configuration dictionary;
- `ConfigurationFingerprint`;
- declared `ForecastCapabilities`;
- explicit `PointForecastStatistic`;
- `Train` returning success or typed failure;
- `Predict` returning a `ForecastDistribution` or typed failure.

## Validation Checklist

1. Train only on `ForecastTrainingContext.TrainingSeries`.
2. Use `ForecastHorizonResolution` instead of raw horizon assumptions.
3. Declare only capabilities the model truly supports.
4. Return `N/A` through missing capabilities rather than fake metrics.
5. Add walk-forward tests for insufficient data and causal cutoffs.
6. Compare against baselines in Model Arena.
7. Update [Models Overview](../models/overview.md), [Forecast Metrics](../validation/forecast-metrics.md),
   and [Source Map](../reference/source-map.md).

## Common Pitfalls

- using full-sample statistics inside historical cutoffs;
- reusing 90-day validation evidence for another horizon;
- emitting probabilities without calibration diagnostics;
- hiding failed training runs as exceptions.
