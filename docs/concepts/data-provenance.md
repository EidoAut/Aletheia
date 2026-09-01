# Data Provenance

Data provenance records where a dataset came from, what dates were requested, what observations were
returned, and how the source payload was cached or fingerprinted.

## Why It Matters

Two analyses can use the same fund name but different provider records, date windows, or source
payloads. Aletheia records provenance so reviewers can reconstruct the analytical context.

## Recorded Metadata

- provider id and display name;
- retrieval timestamp;
- external fund identifier and ISIN where available;
- source URI/reference;
- requested and returned date ranges;
- observation frequency;
- source and effective observation counts;
- dataset fingerprint;
- cache status and cache key when applicable.

## Provider Limits

CNMV IIC is the first official provider, not a global fund universe. Provider coverage and
survivorship properties must be reviewed outside Aletheia before making broad scientific claims.

## Related Pages

- [Data Provenance Architecture](../architecture/data-provenance.md)
- [Loading Data](../user-guide/loading-data.md)
- [Data Layer](../architecture/data-layer.md)
