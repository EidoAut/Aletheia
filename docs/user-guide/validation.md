# Validation Page

The Validation page exposes whether model evidence is strong enough to support interpretation.

## What It Shows

- Model Arena availability;
- walk-forward sample counts;
- common-support diagnostics;
- non-overlapping subset information;
- probability, calibration, and quantile availability;
- model failures and ranking eligibility.

## How To Use It

Start here whenever the final signal is surprising. If validation is absent, weak, or horizon-mismatched,
the decision layer should be tentative or unavailable.

## Common States

| State | Meaning |
| --- | --- |
| Not run | No Arena result is attached to the workspace. |
| Insufficient samples | Historical support is too small for the configured gate. |
| Missing capability | The model does not claim the metric being inspected. |
| Common support unavailable | Direct comparison would use different event sets. |

## Related Pages

- [Validation Philosophy](../validation/philosophy.md)
- [Forecast Metrics](../validation/forecast-metrics.md)
- [Prediction Records](../mathematics/prediction-records.md)
