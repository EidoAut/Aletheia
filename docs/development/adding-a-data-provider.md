# Adding a Data Provider

New data providers should live behind the provider abstractions in `Aletheia.Data`.

## Provider Interfaces

| Interface | Purpose |
| --- | --- |
| `IFundCatalogProvider` | Search a provider catalog. |
| `IProvenanceAwareFundDataProvider` | Load historical NAV data with provenance. |

## Implementation Checklist

1. Preserve source dates and values.
2. Do not interpolate or forward-fill missing observations.
3. Record provider id, source reference, external identifier, request/return dates, observation
   counts, observation frequency, and dataset fingerprint.
4. Bound downloads and decompression.
5. Validate content type and file format before parsing.
6. Disable unsafe XML features when parsing XML.
7. Support cancellation.
8. Add cache validation and corrupt-cache recovery if remote payloads are cached.
9. Add deterministic tests for malformed payloads, cancellation, provenance, and cadence detection.

## Related Pages

- [Data Layer](../architecture/data-layer.md)
- [Data Provenance](../concepts/data-provenance.md)
- [Loading Data](../user-guide/loading-data.md)
