using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Represents one CNMV registered fund share class.
/// </summary>
public sealed record CnmvFundRegistration(
    string FundName,
    string Isin,
    string? ManagerName,
    string? FundType,
    int? FundRegisterNumber,
    int? CompartmentNumber,
    int? ClassNumber,
    string? ClassName,
    string SourceReference)
{
    /// <summary>
    /// Converts the CNMV share-class registration into a search result.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="providerDisplayName">The provider name.</param>
    /// <param name="latestObservation">The latest available observation date.</param>
    /// <param name="frequency">The observation frequency.</param>
    /// <param name="hasHistory">Whether history was observed for this ISIN.</param>
    /// <returns>The search result.</returns>
    public FundSearchResult ToSearchResult(
        string providerId,
        string providerDisplayName,
        DateOnly? latestObservation,
        ObservationFrequency? frequency,
        bool hasHistory)
    {
        return new FundSearchResult(
            providerId,
            providerDisplayName,
            new FundIdentifier(FundIdentifierKind.Isin, this.Isin),
            this.FundName,
            this.Isin,
            this.ManagerName,
            null,
            this.FundType,
            "ES",
            hasHistory,
            null,
            latestObservation,
            frequency,
            "CNMV official IIC XML publication",
            this.SourceReference);
    }
}
