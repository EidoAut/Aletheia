# Model Arena

## Purpose

The Model Arena compares models under identical walk-forward rules, horizons, datasets, and metrics.

## Formula

```text
Model -> walk-forward predictions -> evaluation-event keys -> metric-family common support -> metric summary -> ranking view
```

## Interpretation

The Arena reports all-sample metrics, point/probability/quantile common-support metrics, non-overlapping metrics, coverage, failure reasons, capabilities, and baseline-relative skill. Direct ranking uses point-common-support forecast metrics when enough common events exist.

## Assumptions

Models expose stable descriptors, deterministic configuration fingerprints, typed failure statuses, forecast capabilities, and point-forecast semantics.

## Limitations

Milestone 2.2 does not create a single universal score or a final model confidence probability. Ranking is transparent, sample-gated, and unavailable when common support is too small. CRPS is deferred until Aletheia has forecast distributions rich enough to support it without pretending sparse quantiles define a full distribution.

## Implementation Notes

`ModelArena` no longer uses the first registered model as a baseline. `ModelArenaOptions` selects point and probability baselines by model id. Point ranking sorts eligible models by minimum point-common-support MAE, then RMSE, then maximum directional accuracy. Probability diagnostics use probability-common-support metrics and are shown separately.
