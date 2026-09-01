# Ensembles

Aletheia combines forecasts only after validation evidence supports the participating models for the
same horizon.

## Intuition

An ensemble can reduce dependence on a single model, but only if each member has earned eligibility
under comparable validation evidence.

## Mathematical Definition

Forecast ensemble weights use validation loss and calibration penalty:

\[
w_i = \frac{\exp(-\lambda (L_i + C_i))}{\sum_j \exp(-\lambda (L_j + C_j))}
\]

where \(L_i\) is the validated loss and \(C_i\) is the calibration penalty for eligible model \(i\).

Expected returns and probabilities are combined linearly. Quantiles are obtained by inverting an
approximate weighted mixture CDF rather than averaging percentile values.

## Implementation in Aletheia

`ForecastEnsemble` excludes models with insufficient validation evidence, mismatched horizons,
invalid losses, ineligible rankings, or unsupported capabilities. Evidence from a 90-day Arena does
not weight a 365-day forecast.

## Interpretation

The ensemble is validation-gated evidence, not proof. Model disagreement and weak sample support
lower reliability.

## Assumptions

- Eligible models have comparable same-horizon validation evidence.
- The mixture approximation is adequate for reported quantiles.
- Baseline-relative skill and calibration gates are meaningful for the dataset.

## Limitations

The current ensemble does not prove economic value. Economic timing performance is reported
separately through delayed backtesting.

## Source and Tests

- Source: `src/Aletheia.Forecasting/ForecastEnsemble.cs`,
  `src/Aletheia.Application/FundResearchReportBuilder.cs`
- Tests: `tests/Aletheia.Forecasting.Tests/ForecastEnsembleTests.cs`,
  `tests/Aletheia.Application.Tests`
