namespace Aletheia.Data;

/// <summary>
/// Defines fund-discovery operations for a provider/source.
/// </summary>
public interface IFundCatalogProvider
{
    /// <summary>
    /// Gets the stable provider identifier.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Gets the human-readable provider name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets provider capability metadata.
    /// </summary>
    FundCatalogCapabilities Capabilities { get; }

    /// <summary>
    /// Searches the provider catalog.
    /// </summary>
    /// <param name="query">The normalized search query.</param>
    /// <param name="maximumResults">The maximum result count.</param>
    /// <param name="cancellationToken">A token used to cancel I/O work.</param>
    /// <returns>The matching fund/share-class results.</returns>
    Task<IReadOnlyList<FundSearchResult>> SearchAsync(
        FundSearchQuery query,
        int maximumResults = 50,
        CancellationToken cancellationToken = default);
}
