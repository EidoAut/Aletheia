# Baseline Models

Baselines are simple reference models. Complex models must compete against them before Aletheia gives
their evidence interpretive weight.

## Intuition

A baseline asks, "Does the model do better than a simple answer?" If not, the complex model has not
earned trust.

## Implemented Baselines

| Baseline | Purpose | Source |
| --- | --- | --- |
| Zero Return | Point-forecast benchmark with zero cumulative simple return. | `src/Aletheia.Validation/ZeroReturnForecastModel.cs` |
| Historical Probability Climatology | Probability benchmark from historical positive-return base rate for the same horizon. | `src/Aletheia.Validation/HistoricalProbabilityBaselineForecastModel.cs` |
| Historical event rate | Market-timing baseline for triple-barrier outcomes. | `src/Aletheia.Validation/MarketTimingModelArena.cs` |

## Mathematical Definition

Zero-return point forecast:

$$
\hat{r}_{t,h} = 0
$$

Historical positive-return probability:

$$
\hat{p}_{t,h} = \frac{1}{n}\sum_{i=1}^{n} I(r_{i,h} > 0)
$$

where $r_{i,h}$ is a completed horizon return inside the training window and $I$ is an indicator.

## Implementation in Aletheia

The zero-return baseline does not advertise probability capability because deterministic zero return
would imply a probability statement that is not the intended neutral Brier baseline. The climatology
baseline does the opposite: it advertises probability capability but not point forecasts or quantiles.

## Validation Requirements

Baselines participate in Model Arena. Nontrivial models are compared against them through
common-support metrics and baseline-relative skill.

## Limitations

Baselines are not meant to be investment models. They are scientific controls.

## Related Tests

- `tests/Aletheia.Validation.Tests/ModelArenaTests.cs`
- `tests/Aletheia.Validation.Tests/ValidationMetricCalculatorTests.cs`
