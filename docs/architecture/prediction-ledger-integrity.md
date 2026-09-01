# Prediction Ledger Integrity

## Purpose

The ledger separates logical prediction identity from scientific content identity.

## Identities

`PredictionLogicalKey` answers:

```text
Which prediction event is this?
```

`PredictionContentFingerprint` answers:

```text
What exact prediction did the model make?
```

The same logical key with identical content is idempotent. The same logical key with different content is an integrity error.

## Evaluations

Evaluation rows follow the same rule. Re-storing an identical realized evaluation is idempotent. Reusing the same prediction/evaluation identity with different actual return, direction, error, or Brier content raises `PredictionLedgerIntegrityException`.

## Implementation Notes

SQLite schema version 2 stores prediction and evaluation content fingerprints. Existing version 1 ledgers are migrated by adding capability and fingerprint columns.
