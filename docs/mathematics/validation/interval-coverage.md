# Prediction Interval Coverage

## Purpose

Prediction interval coverage checks whether realized returns fall inside a forecast interval at the expected frequency.

## Formula

For `[q10, q90]`:

$$
\mathrm{ObservedCoverage}
= \frac{\#\{i:q_{10,i}\le y_i \le q_{90,i}\}}{N}
$$

$$
\mathrm{NominalCoverage}=0.80
$$

$$
\mathrm{CoverageError}
= \mathrm{ObservedCoverage}-\mathrm{NominalCoverage}
$$

## Interpretation

A nominal 80 percent interval should contain about 80 percent of realized outcomes. Average width is reported because extremely wide intervals can achieve high coverage without useful precision.

## Assumptions

The lower and upper quantiles are available and monotonic.

## Limitations

Coverage requires enough samples to be meaningful. Overlapping target periods reduce independence.

## Implementation Notes

`IntervalCoverageCalculator` reports sample count, observed coverage, coverage error, and average interval width.
