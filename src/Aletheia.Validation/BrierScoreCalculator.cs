namespace Aletheia.Validation;

/// <summary>
/// Calculates Brier score for positive-return probability forecasts.
/// </summary>
public sealed class BrierScoreCalculator
{
    /// <summary>
    /// Calculates the mean squared probability error.
    /// </summary>
    /// <param name="evaluations">The prediction evaluations.</param>
    /// <returns>The Brier score, or <see langword="null"/> for zero samples.</returns>
    public double? Calculate(IReadOnlyList<PredictionEvaluationRecord> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        return evaluations.Count == 0 ? null : evaluations.Average(item => item.BrierContribution);
    }
}
