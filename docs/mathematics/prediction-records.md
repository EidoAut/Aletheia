# Prediction Records

## Purpose

Prediction records are designed for reproducibility and ledger storage.

A record should answer:

> What was predicted, by which model, from which dataset, at which cutoff, using which feature schema?

## Metadata

`PredictionRecord` contains:

- prediction id;
- fund identifier;
- UTC generation time;
- data cutoff date;
- forecast horizon resolution;
- requested horizon;
- observation frequency;
- effective observation count;
- target date, when known;
- point forecast return;
- point forecast statistic;
- forecast capabilities;
- expected and median return;
- probability of positive return;
- return percentiles;
- model descriptor;
- model parameters;
- Aletheia version;
- state schema version;
- state schema fingerprint;
- dataset identity;
- random seed, when stochastic;
- investment signal and strength, when any;
- feature configuration id.

## Dataset Identity

`DatasetFingerprintCalculator` calculates a deterministic SHA-256 fingerprint over ordered canonical observations:

```text
yyyy-MM-dd|NAV
```

one observation per line. This gives a compact identity for the exact dataset used to generate a prediction.

## Ledger Audit Metadata

Milestone 2 stores `PredictionRecord` inside `PredictionLedgerRecord`, which adds:

- prediction origin, such as `HistoricalWalkForward` or `Live`;
- deterministic logical key for idempotency;
- deterministic content fingerprint for scientific conflict detection;
- model configuration fingerprint;
- training start and end dates;
- training start and end indices;
- prediction cutoff index;
- target index and target date;
- diagnostic metadata.

Realized outcomes are not written back into the prediction row. They are stored separately as `PredictionEvaluationRecord` rows in the ledger.

The logical key identifies the prediction event. The content fingerprint identifies the exact scientific payload produced for that event. Reusing a logical key with different content is an integrity error, not an overwrite.
