# Probability Calibration

## Purpose

Calibration compares predicted positive-return probabilities with observed positive-return frequencies.

## Formula

For each probability bin `b`:

$$
\bar{p}_b = \frac{1}{n_b}\sum_{i\in b}p_i
$$

$$
\hat{o}_b = \frac{1}{n_b}\sum_{i\in b}y_i
$$

$$
\operatorname{ECE}
= \sum_b \frac{n_b}{N}\left|\hat{o}_b-\bar{p}_b\right|
$$

## Interpretation

A well-calibrated `0.70` bin should contain positive outcomes roughly 70 percent of the time.

## Assumptions

Bins are equal-width over `[0, 1]` by default. Empty bins are represented explicitly.

## Limitations

Small bins can be noisy. ECE is a diagnostic, not a universal model-quality score.

## Implementation Notes

`CalibrationOptions.BinCount` controls bin count. `CalibrationCalculator` includes empty bins with null observed frequency.
