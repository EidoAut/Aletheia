namespace Aletheia.Application;

/// <summary>
/// Stores the fund quality score and transparent component breakdown.
/// </summary>
/// <param name="Score">The overall score in [1, 10].</param>
/// <param name="Confidence">The confidence level.</param>
/// <param name="Components">Weighted score components.</param>
/// <param name="Reasons">Positive deterministic explanations.</param>
/// <param name="Warnings">Warnings and caveats.</param>
public sealed record FundScore(
    double Score,
    ConfidenceLevel Confidence,
    IReadOnlyList<ScoreComponent> Components,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings);
