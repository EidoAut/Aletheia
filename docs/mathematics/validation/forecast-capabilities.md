# Forecast Capabilities

## Purpose

Forecast capabilities define which quantities a model actually supports.

## Supported Quantities

`ForecastCapabilities` currently includes:

```text
PointForecast
ExpectedReturn
Median
ProbabilityPositive
Quantiles
FullDistribution
```

Metrics are capability-gated. MAE and RMSE require `PointForecast`; Brier score and calibration require `ProbabilityPositive`; pinball loss and interval coverage require `Quantiles`.

## Zero-Return Baseline

The zero-return model is a point baseline. It does not advertise `ProbabilityPositive` and does not fabricate quantiles. A deterministic forecast $R = 0$ cannot also be used as a neutral $P(R > 0) = 0.5$ probability forecast.

## Implementation Notes

Unsupported metrics surface as `MetricStatus.NotSupported` and CLI output displays `N/A`.
