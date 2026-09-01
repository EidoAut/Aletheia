# Calibration

Calibration evaluates whether forecast probabilities behave like observed frequencies.

## Binary Forecast Calibration

For positive-return forecasts, Aletheia evaluates probability bins and Brier score. A probability
forecast that often says 70% should see the event happen about 70% of the time across a sufficiently
large comparable sample.

## Timing Calibration

Market timing is multi-class: upside first, downside first, or no barrier. Aletheia reports Brier
score, log loss, expected calibration error, per-class calibration, reliability bins, and Brier
decomposition for timing candidates.

Platt scaling is fit only from prior out-of-sample predictions. When there are too few samples,
probabilities remain raw and reliability is penalized.

## Interpretation

Good calibration supports probabilistic interpretation. It does not imply positive economic value.
That requires a separate delayed economic backtest.

## Source and Tests

- Source: `src/Aletheia.Validation/CalibrationCalculator.cs`,
  `src/Aletheia.Validation/TimingProbabilityMetrics.cs`,
  `src/Aletheia.Validation/PlattProbabilityCalibrator.cs`
- Tests: `tests/Aletheia.Validation.Tests/ValidationMetricCalculatorTests.cs`,
  `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs`
