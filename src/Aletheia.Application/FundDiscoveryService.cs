using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Application;

/// <summary>
/// Coordinates fund discovery and provider-backed history loading.
/// </summary>
public sealed class FundDiscoveryService
{
    private readonly IReadOnlyList<IFundCatalogProvider> catalogProviders;
    private readonly IReadOnlyDictionary<string, IProvenanceAwareFundDataProvider> historyProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="FundDiscoveryService"/> class.
    /// </summary>
    /// <param name="catalogProviders">Catalog providers.</param>
    /// <param name="historyProviders">History providers keyed by provider id.</param>
    public FundDiscoveryService(
        IReadOnlyList<IFundCatalogProvider> catalogProviders,
        IReadOnlyDictionary<string, IProvenanceAwareFundDataProvider> historyProviders)
    {
        this.catalogProviders = catalogProviders;
        this.historyProviders = historyProviders;
    }

    /// <summary>
    /// Searches all configured fund catalogs.
    /// </summary>
    /// <param name="query">The user query.</param>
    /// <param name="maximumResults">The maximum total result count.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>Search results.</returns>
    public async Task<IReadOnlyList<FundSearchResultSummary>> SearchAsync(
        string query,
        int maximumResults = 50,
        CancellationToken cancellationToken = default)
    {
        var normalized = FundSearchQuery.FromUserText(query);
        if (normalized.IsEmpty)
        {
            return Array.Empty<FundSearchResultSummary>();
        }

        var results = new List<FundSearchResultSummary>();
        foreach (var provider in this.catalogProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var providerResults = await provider.SearchAsync(
                normalized,
                Math.Max(1, maximumResults - results.Count),
                cancellationToken).ConfigureAwait(false);
            results.AddRange(providerResults.Select(ToSummary));
            if (results.Count >= maximumResults)
            {
                break;
            }
        }

        return results
            .OrderByDescending(item => item.Isin is not null && item.Isin == normalized.Isin)
            .ThenBy(item => item.FundName, StringComparer.OrdinalIgnoreCase)
            .Take(maximumResults)
            .ToArray();
    }

    /// <summary>
    /// Loads a selected provider fund history.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="fundIdentifier">The selected fund identifier.</param>
    /// <param name="from">The optional start date.</param>
    /// <param name="to">The optional end date.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The history and provenance.</returns>
    public async Task<FundHistoryResult> LoadHistoryAsync(
        string providerId,
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!this.historyProviders.TryGetValue(providerId, out var provider))
        {
            throw new ArgumentException($"No history provider is configured for '{providerId}'.", nameof(providerId));
        }

        return await provider.GetHistoryWithProvenanceAsync(fundIdentifier, from, to, cancellationToken).ConfigureAwait(false);
    }

    private static FundSearchResultSummary ToSummary(FundSearchResult result)
    {
        return new FundSearchResultSummary(
            result.ProviderId,
            result.ProviderDisplayName,
            result.FundIdentifier,
            result.FundName,
            result.Isin,
            result.ManagementCompany,
            result.Currency,
            result.Category,
            result.Country,
            result.HasHistoricalData,
            result.EarliestAvailableObservation,
            result.LatestAvailableObservation,
            result.ObservationFrequency,
            result.SourceAuthority,
            result.SourceReference);
    }
}
