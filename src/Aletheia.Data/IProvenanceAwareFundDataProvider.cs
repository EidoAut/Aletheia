using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Provides historical fund data with explicit provenance.
/// </summary>
public interface IProvenanceAwareFundDataProvider : IFundDataProvider
{
    /// <summary>
    /// Loads historical NAV observations and data-source provenance.
    /// </summary>
    /// <param name="fundIdentifier">The fund/share-class identifier.</param>
    /// <param name="from">The optional inclusive start date.</param>
    /// <param name="to">The optional inclusive end date.</param>
    /// <param name="cancellationToken">A token used to cancel I/O work.</param>
    /// <returns>The fund history and provenance.</returns>
    Task<FundHistoryResult> GetHistoryWithProvenanceAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
