# Data Provenance

Milestone 2.3 introduces provider-aware fund discovery and explicit provenance for every loaded dataset.

## Provider Boundary

`Aletheia.Data` owns source-specific work:

- `IFundCatalogProvider` searches a provider catalog.
- `IProvenanceAwareFundDataProvider` loads historical NAV data and returns `FundHistoryResult`.
- `FundDataProvenance` records provider id, display name, retrieval time, external identifier, ISIN, source URI/reference, requested and returned date ranges, observation frequency, original and normalized observation counts, dataset fingerprint, cache state, and cache key.

`Aletheia.Application` exposes provider-neutral summaries to CLI and Desktop. UI projects do not parse provider payloads.

## CNMV IIC Provider

The first official provider is CNMV IIC, based on the CNMV "Descarga de informacion individual" publications: <https://www.cnmv.es/portal/publicaciones/descarga-informacion-individual?ejercicio=2024&lang=es>. The legacy `.aspx` page can redirect, so provider tests cover redirected index pages while production uses the current route generated per exercise year.

The provider reads official ZIP archives and parses:

- `FONDREGISTRO_YYYYMM.xml` for registered fund/share-class identity, ISIN, manager, type, register, compartment, and class references.
- `FONDMENS_YYYYMM.xml` for reported daily NAV fields (`VL_DiaN`) for exact ISIN matches.

Search supports fund name, exact ISIN, partial ISIN, and management company. History loading requires an exact valid ISIN.

Document discovery is scoped to the CNMV download table (`grdDescargas`) and accepts only rows whose first cell resolves to a month and whose document cell points at the CNMV `webservices/verdocumento/ver` endpoint with matching month metadata. Non-month rows and non-document links are ignored before download. Candidate documents are de-duplicated, sorted by period, filtered to the requested date range, and cached in memory so the same archive is not downloaded repeatedly in one provider instance.

## Temporal Semantics

Provider data is not interpolated, forward-filled, or calendar-repaired. Aletheia preserves the dates reported by the source and detects observation frequency from those dates. Business-daily classification tolerates isolated missing weekdays, such as market holidays, when reported observations still cover at least 80% of weekdays across the range; sparse weekly-like series remain irregular. When the data is truly irregular, annualized analytics still require an explicit defensible convention instead of guessing.

## Cache

Remote provider payloads pass through `LocalProviderCache`. Cache keys are SHA-256 hashes over provider id and source URI. The cache stores raw bytes plus metadata and reports whether a result came from cache through provenance.

Cache metadata now includes retrieval time, content length, and SHA-256 of the raw payload. Reads verify hash and length before returning bytes. Corrupt, partial, stale, or unreadable entries are discarded and safely refetched. Writes are atomic: payload and metadata are written to temporary files and moved into place only after the write completes.

Payload validation happens before atomic cache writes. Index pages may be HTML; monthly IIC documents must not be `text/html`, must have a ZIP signature (`PK\x03\x04`, `PK\x05\x06`, or `PK\x07\x08`), must open as a bounded ZIP archive, and must contain the expected `FONDMENS_YYYYMM.xml`. If an older cache entry passes SHA validation but is semantically invalid for its role, such as cached HTML where a ZIP is expected, Aletheia invalidates that cache key and retries the network download once. A second failure returns the real provider error with diagnostics instead of looping.

The default cache path is under the current user's local application data directory:

```text
Aletheia/cache
```

## Provider Safety

CNMV HTTP responses are streamed with cancellation and a configurable maximum byte count. Redirects are followed explicitly up to the configured limit so diagnostics can report both requested and final URIs. Response content type is checked according to payload role. ZIP archives are bounded by number of XML entries, per-entry decompressed bytes, total decompressed bytes, and decompressed/compressed ratio. XML parsing uses `XmlReaderSettings` with DTD processing disabled and a document character limit.

## Error Handling

Provider failures use `FundProviderException` with typed error kinds for not found, unavailable provider, timeout, invalid response, and no usable history. Rejection messages include year, period, requested URI, final URI, content type, received byte count, network/cache origin, and exact rejection reason. Callers can cancel HTTP and file work through `CancellationToken`.
