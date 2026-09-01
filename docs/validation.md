# Validation

Validation is the gate between descriptive modeling and any current signal. A model may be mathematically available but still excluded from ensemble use when it fails fair out-of-sample comparison.

## Walk-Forward Discipline

The validation stack trains each model only on data available at the cutoff. Predictions are made for a resolved horizon, then evaluated once the realized outcome is known.

Important invariants:

- training data ends at the cutoff;
- prediction horizons distinguish calendar days from observation counts;
- realized outcomes are evaluated separately from prediction creation;
- failed model attempts are typed results, not normal-flow exceptions;
- common-support metrics compare models only where all compared models produced usable forecasts.

## Metric Families

Model Arena reports point, probability, calibration, quantile, interval-coverage, and baseline-relative skill metrics. The research report uses validation loss and calibration penalty for ensemble weighting and treats relative skill as an eligibility signal.

## Non-Overlapping Evaluation

For horizon-sensitive metrics, the Arena can select deterministic non-overlapping subsets. This avoids overstating evidence from heavily overlapping forecasts.

## State-Space No-Lookahead Contract

The state-space forecast adapter refits the Kalman model at each cutoff. Tests mutate future observations after the cutoff and assert the prediction is unchanged. This protects against accidental full-sample leakage.

## Failure Modes

- Insufficient history produces model-level failures and report warnings.
- Missing capability prevents that metric from entering common-support comparison.
- No ranking-eligible model means no validation-gated ensemble.
- Low ensemble reliability prevents confirmed decision labels. A directional estimate may remain visible as `BUY?`, `HOLD?`, or `SELL?`; otherwise the report emits `NO CALL`.
