using Aletheia.Core;

namespace Aletheia.Application;

/// <summary>
/// Presentation summary for dataset provenance.
/// </summary>
public sealed record DatasetProvenanceSummary(
    string ProviderId,
    string ProviderDisplayName,
    DateTimeOffset RetrievalTimestampUtc,
    FundIdentifier ExternalFundIdentifier,
    string? Isin,
    Uri? SourceUri,
    string? SourceReference,
    ObservationFrequency ObservationFrequency,
    DateOnly? RequestedStartDate,
    DateOnly? RequestedEndDate,
    DateOnly? ReturnedStartDate,
    DateOnly? ReturnedEndDate,
    int OriginalObservationCount,
    int NormalizedObservationCount,
    string DatasetFingerprintSha256,
    bool IsFromCache,
    string? CacheKey);
