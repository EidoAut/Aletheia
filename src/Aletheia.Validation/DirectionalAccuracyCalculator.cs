namespace Aletheia.Validation;

/// <summary>
/// Calculates the fraction of predictions with matching forecast and actual direction.
/// </summary>
public sealed class DirectionalAccuracyCalculator
{
    /// <summary>
    /// Calculates directional accuracy.
    /// </summary>
    /// <param name="evaluations">The prediction evaluations.</param>
    /// <returns>The matching-direction rate, or <see langword="null"/> for zero samples.</returns>
    public double? Calculate(IReadOnlyList<PredictionEvaluationRecord> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        return evaluations.Count == 0
            ? null
            : evaluations.Count(item => item.DirectionCorrect) / (double)evaluations.Count;
    }
}
