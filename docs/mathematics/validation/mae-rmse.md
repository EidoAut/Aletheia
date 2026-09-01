# MAE, MSE, and RMSE

## Purpose

Point-forecast error metrics compare the forecast return with the realized simple return over the same horizon.

## Formula

$$
\mathrm{MAE} = \frac{1}{N}\sum_{i=1}^{N}|y_i-\hat{y}_i|
$$

$$
\mathrm{MSE} = \frac{1}{N}\sum_{i=1}^{N}(y_i-\hat{y}_i)^2
$$

$$
\mathrm{RMSE} = \sqrt{\mathrm{MSE}}
$$

## Interpretation

MAE and RMSE are in decimal return units. CLI output formats them as percentages, so `5.0%` means `0.05` decimal return error.

## Assumptions

Forecast and actual return units are cumulative simple returns over the resolved horizon.

## Limitations

RMSE penalizes large errors more heavily than MAE. Neither metric evaluates probability calibration or interval quality.

## Implementation Notes

`MeanAbsoluteErrorCalculator`, `MeanSquaredErrorCalculator`, and `RootMeanSquaredErrorCalculator` return `null` for zero samples rather than inventing a value.
