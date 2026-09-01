using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Defines a replaceable source of fund metadata and historical NAV observations.
/// </summary>
public interface IFundDataProvider
{
    /// <summary>
    /// Finds a fund by ISIN.
    /// </summary>
    /// <param name="isin">The ISIN to search for.</param>
    /// <param name="cancellationToken">A token used to cancel I/O work.</param>
    /// <returns>The matching fund, or <see langword="null"/> when the provider has no match.</returns>
    Task<Fund?> FindByIsinAsync(string isin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads historical NAV observations for a fund.
    /// </summary>
    /// <param name="fundIdentifier">The provider-independent fund identifier.</param>
    /// <param name="from">The optional inclusive start date.</param>
    /// <param name="to">The optional inclusive end date.</param>
    /// <param name="cancellationToken">A token used to cancel I/O work.</param>
    /// <returns>The fund history.</returns>
    Task<FundHistory> GetHistoryAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
