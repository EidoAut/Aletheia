namespace Aletheia.Validation;

/// <summary>
/// Calculates mean squared error in squared decimal return units.
/// </summary>
public sealed class MeanSquaredErrorCalculator
{
    /// <summary>
    /// Calculates the arithmetic mean of squared forecast errors.
    /// </summary>
    /// <param name="evaluations">The prediction evaluations.</param>
    /// <returns>The mean squared error, or <see langword="null"/> for zero samples.</returns>
    public double? Calculate(IReadOnlyList<PredictionEvaluationRecord> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        return evaluations.Count == 0 ? null : evaluations.Average(item => item.SquaredError);
    }
}
