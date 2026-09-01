# Directional Accuracy

## Purpose

Directional accuracy measures whether the forecast and realized return have the same sign.

## Formula

$$
\mathrm{DA}
= \frac{
  \#\{i:\mathrm{direction\_rule}(\mathrm{forecast}_i)
  = \mathrm{direction}(y_i)\}
}{N}
$$

## Interpretation

Higher is better, but direction alone ignores magnitude. A model can have good directional accuracy and poor return error.

## Assumptions

Returns are classified as positive, negative, or flat using a configurable flat-return tolerance.

## Limitations

If most realized returns are positive, a simple positive-bias model may score well without useful magnitude forecasts.

## Implementation Notes

`DirectionClassifier` treats $|\mathrm{return}| \le \mathrm{tolerance}$ as flat. The default tolerance is zero. `DirectionPredictionRule` records whether predicted direction came from the point forecast, median, or probability-positive threshold.
