# Loading Data

Aletheia accepts three practical data paths: the deterministic sample provider, a local CSV file, and
provider-backed CNMV IIC history.

## Sample Data

```powershell
dotnet run --project src/Aletheia.Cli -- sample
```

The sample dataset is deterministic and business-daily. It is useful for smoke testing, tutorials,
and reproducing UI behavior without network access.

## CSV Data

```powershell
dotnet run --project src/Aletheia.Cli -- analyze examples/sample-fund.csv
```

The CSV reader creates a `FundHistory` and detects observation frequency from the loaded dates. CSV
data is local provenance, so remote cache metadata is not involved.

## Provider Data

```powershell
dotnet run --project src/Aletheia.Cli -- analyze --provider cnmv-iic --fund ES0000000000 --from 2024-01-01 --to 2024-12-31
```

The provider path records provider id, source reference, external identifier, request dates,
returned dates, observation count, frequency, dataset fingerprint, retrieval time, and cache state.

## What Aletheia Does Not Do To Data

- It does not repair missing provider dates.
- It does not forward-fill missing NAV values.
- It does not interpolate returns into unobserved dates.
- It does not turn provider availability into survivorship-adjusted universe evidence.

These choices make the output more conservative and more auditable.
