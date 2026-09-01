# Forecasting

The Forecast page shows current forecast runs for the loaded fund. Application-visible forecasts use
30, 90, 180, and 365 calendar-day horizons.

## What It Consumes

- loaded fund history;
- forecast horizon resolution;
- default forecast models;
- model capabilities and point-statistic metadata.

## What It Displays

| Output | Meaning |
| --- | --- |
| Horizon | Requested calendar-day window and resolved effective observation count. |
| Status | Whether model training/prediction succeeded or returned a typed failure. |
| Point forecast | Present only if the model declares point-forecast capability. |
| Expected return | Mean simple return when supported. |
| Median | Median simple return when supported. |
| `P(Return > 0)` | Positive-return probability when supported. |
| Quantiles | Distribution summaries when sufficient support exists. |

## Interpretation

Forecasting is model-agnostic. A forecast can exist before it is validated well enough to influence a
confirmed signal. Unsupported capabilities display `N/A` instead of fabricated metrics.

## Related Pages

- [Forecast Horizons](../concepts/forecast-horizons.md)
- [Forecasting Science Note](../forecasting.md)
- [Ensembles](../models/ensembles.md)
- [Model Arena](model-arena.md)
