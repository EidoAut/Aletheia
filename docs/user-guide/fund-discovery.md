# Fund Discovery

Fund Discovery is the first desktop workflow. It searches configured provider catalogs and lets the
user load an exact fund or share-class history.

## Data Consumed

The current implementation configures the CNMV IIC provider. It can search fund names, exact ISINs,
partial ISINs, and management companies. History loading requires an exact valid ISIN.

## How To Use It

=== "Desktop"
    1. Open the desktop application.
    2. Search by fund name, ISIN, partial ISIN, or manager.
    3. Select a result.
    4. Load its history.

=== "CLI"
    ```powershell
    dotnet run --project src/Aletheia.Cli -- funds search mediolanum
    dotnet run --project src/Aletheia.Cli -- funds search --isin ES0000000000
    dotnet run --project src/Aletheia.Cli -- analyze --provider cnmv-iic --fund ES0000000000
    ```

## Interpretation

Search results are catalog matches, not analytical conclusions. After loading a fund, inspect the
provenance and data-quality sections before interpreting performance or forecasts.

## Provider Boundaries

CNMV payloads are bounded, cached, and validated before use. Aletheia preserves reported dates and
does not interpolate missing fund valuations. See [Data Provenance](../concepts/data-provenance.md)
and [Data Layer](../architecture/data-layer.md).
