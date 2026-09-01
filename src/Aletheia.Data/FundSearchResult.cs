using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Represents a fund/share-class search result from a catalog provider.
/// </summary>
public sealed record FundSearchResult(
    string ProviderId,
    string ProviderDisplayName,
    FundIdentifier FundIdentifier,
    string FundName,
    string? Isin,
    string? ManagementCompany,
    string? Currency,
    string? Category,
    string? Country,
    bool HasHistoricalData,
    DateOnly? EarliestAvailableObservation,
    DateOnly? LatestAvailableObservation,
    ObservationFrequency? ObservationFrequency,
    string SourceAuthority,
    string? SourceReference);
