using Aletheia.Core;

namespace Aletheia.Application;

/// <summary>
/// Presentation summary for a fund discovery result.
/// </summary>
public sealed record FundSearchResultSummary(
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
