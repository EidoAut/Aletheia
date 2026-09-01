namespace Aletheia.Application;

/// <summary>
/// Stores current attractiveness separately from long-run fund quality.
/// </summary>
/// <param name="Score">The attractiveness score in [1, 10].</param>
/// <param name="Category">The qualitative category.</param>
/// <param name="Confidence">The confidence level.</param>
/// <param name="Evidence">Supporting evidence.</param>
/// <param name="Warnings">Warnings and caveats.</param>
public sealed record CurrentOpportunityAssessment(
    double Score,
    CurrentAttractivenessCategory Category,
    ConfidenceLevel Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> Warnings);
