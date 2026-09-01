using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Describes where a loaded fund dataset came from.
/// </summary>
public sealed record FundDataProvenance(
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
