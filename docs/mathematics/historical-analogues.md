# Historical Analogues

## Purpose

Historical analogue search asks:

> Which previous states were mathematically similar to the current state?

## Distance

Milestone 1 uses standardized Euclidean distance:

$$
d(x,y) = \sqrt{\sum_{i=1}^{k}\left(\frac{x_i-y_i}{s_i}\right)^2}
$$

where `s_i` is the historical standard deviation of dimension `i`.

## State Construction

Historical and current states are built by the same `IStateFeaturePipeline`. When building the state at index `i`, the pipeline receives only observations `0..i`. Future observations are not used for returns, rolling volatility, trend, momentum, smoothing, or derivatives.

State schema metadata identifies the dimensions, feature parameters, and deterministic fingerprint. Analogue comparisons require identical schema fingerprints.

## Look-Ahead Rule

The analogue finder excludes observations at or after the current state date. Outcome analysis only uses returns that occur after each historical analogue date.

For a query state, standardized Euclidean normalization is estimated from candidate states strictly before the query date. The query state is not included in the normalization sample, and future candidate dates are excluded.

The search result reports candidate counts, schema-compatible counts, schema-rejected counts, missing-dimension rejections, and the deterministic dimension order used in distance calculation.

## Limitations

The current normalization strategy is safe for a single query-date analogue search, but it is not a complete walk-forward analogue engine. Future validation should run the entire analogue search separately at each prediction cutoff and record normalization diagnostics.
