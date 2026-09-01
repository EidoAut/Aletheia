namespace Aletheia.Application;

/// <summary>
/// Combines strategic evidence, tactical timing, and data freshness into an actionable-confidence view.
/// </summary>
/// <param name="Status">The actionable status.</param>
/// <param name="Confidence">The overall actionable confidence.</param>
/// <param name="EffectiveDate">The date as of which the signal is actually known.</param>
/// <param name="Reasons">Deterministic reasons for the status.</param>
public sealed record ActionabilityAssessment(
    string Status,
    ConfidenceLevel Confidence,
    DateOnly EffectiveDate,
    IReadOnlyList<string> Reasons)
{
    /// <summary>
    /// Gets the structured actionability level.
    /// </summary>
    public SignalActionabilityLevel Level => DecisionSignalLabels.ToActionabilityLevel(this.Status);
}
