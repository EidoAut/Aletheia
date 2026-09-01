# Predictions Ledger

The prediction ledger records immutable forecasts and separate realized evaluations. It is Aletheia's
accountability mechanism for historical walk-forward predictions.

## CLI Usage

```powershell
dotnet run --project src/Aletheia.Cli -- predictions list
dotnet run --project src/Aletheia.Cli -- predictions show <prediction-id>
```

By default, the SQLite ledger lives at:

```text
data/aletheia.db
```

Set `ALETHEIA_LEDGER_PATH` to change the path.

## Interpretation

`Prediction` and `PredictionEvaluation` are different records. Prediction rows are never updated with
actual outcomes; evaluations are linked separately. Re-running the same logical prediction with
identical content is idempotent. Reusing a logical key with different scientific content is an
integrity error.

## Related Pages

- [Prediction Ledger](../architecture/prediction-ledger.md)
- [Prediction Ledger Integrity](../architecture/prediction-ledger-integrity.md)
- [Prediction Records](../mathematics/prediction-records.md)
