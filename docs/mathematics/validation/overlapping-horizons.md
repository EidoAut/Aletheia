# Overlapping Horizons

## Purpose

Dense long-horizon forecasts produce target windows that overlap. These should not be treated as independent experiments.

## Formula

For prediction `i`:

$$
W_i = (\operatorname{cutoff\_index}_i,\operatorname{target\_index}_i]
$$

Two forecasts overlap when the next window starts on or before the previous target index.

## Interpretation

All-sample metrics show every generated forecast. Common-support metrics show the intersection of events shared by compared models. Non-overlapping metrics show a deterministic subset with non-overlapping target windows.

## Assumptions

Target indices are known. Calendar horizons are mapped to observed target dates before selection.

## Limitations

The subset can be much smaller than the all-sample view, especially for long horizons. Non-overlap reduces one source of dependence but does not prove statistical independence.

## Implementation Notes

`NonOverlappingForecastSelector` takes the earliest eligible forecast, skips overlapping windows, then repeats.
