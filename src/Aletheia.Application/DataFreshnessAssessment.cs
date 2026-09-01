namespace Aletheia.Application;

/// <summary>
/// Stores freshness diagnostics for the effective scientific dataset.
/// </summary>
/// <param name="GeneratedAt">The report or analysis generation timestamp.</param>
/// <param name="LastEffectiveObservationDate">The last observation used by scientific calculations.</param>
/// <param name="DataAgeDays">Elapsed calendar days from the last effective observation to generation.</param>
/// <param name="Status">The freshness class.</param>
/// <param name="FreshMaxAgeDays">Maximum age still considered fresh.</param>
/// <param name="ActionableMaxAgeDays">Maximum age still allowed for qualified actionability.</param>
/// <param name="Diagnostic">Human-readable deterministic diagnostic.</param>
public sealed record DataFreshnessAssessment(
    DateTimeOffset GeneratedAt,
    DateOnly LastEffectiveObservationDate,
    int DataAgeDays,
    DataFreshnessStatus Status,
    int FreshMaxAgeDays,
    int ActionableMaxAgeDays,
    string Diagnostic)
{
    /// <summary>
    /// Gets a value indicating whether the dataset may support an actionable current decision.
    /// </summary>
    public bool AllowsCurrentActionability => this.Status != DataFreshnessStatus.Stale;
}
