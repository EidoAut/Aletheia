# Common Support

Common support means models are compared on the same evaluation events.

## Why It Matters

If Model A has forecasts for easy periods and Model B has forecasts for hard periods, comparing their
overall averages is unfair. Common support restricts a metric family to events where all compared
models produced usable outputs for that capability.

## Metric Families

Aletheia tracks support separately for:

- point forecasts;
- probability forecasts;
- calibration;
- quantiles;
- interval coverage.

Unsupported capabilities show `N/A` rather than fake values.

## Model Arena

Model Arena reports all-sample metrics, common-support metrics, non-overlapping subsets, capability
coverage, failure reasons, and ranking eligibility. Direct point ranking uses point-common-support
metrics when enough events exist.

## Related Pages

- [Model Arena](../user-guide/model-arena.md)
- [Common-Support Evaluation Notes](../mathematics/validation/common-support-evaluation.md)
- [Forecast Capabilities](../mathematics/validation/forecast-capabilities.md)
