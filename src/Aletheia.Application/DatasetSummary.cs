using Aletheia.Core;

namespace Aletheia.Application;

/// <summary>
/// Summarizes the currently loaded fund dataset.
/// </summary>
public sealed record DatasetSummary(
    string FundName,
    FundIdentifier Identifier,
    string? Provider,
    string? Currency,
    DateOnly StartDate,
    DateOnly EndDate,
    int ObservationCount,
    ObservationFrequency ObservationFrequency,
    string DatasetFingerprint,
    string? SourcePath,
    DatasetProvenanceSummary? Provenance = null,
    int SourceObservationCount = 0,
    int SyntheticObservationCount = 0,
    DateOnly? SourceStartDate = null,
    DateOnly? SourceEndDate = null,
    DateOnly? LastEffectiveObservationDate = null,
    string EffectiveObservationPolicy = "Source observations.",
    DataFreshnessAssessment? Freshness = null)
{
    /// <summary>
    /// Gets the effective observation count used by scientific calculations.
    /// </summary>
    public int EffectiveObservationCount => this.ObservationCount;
}
