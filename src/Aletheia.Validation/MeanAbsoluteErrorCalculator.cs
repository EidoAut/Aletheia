namespace Aletheia.Validation;

/// <summary>
/// Calculates mean absolute error in decimal return units.
/// </summary>
public sealed class MeanAbsoluteErrorCalculator
{
    /// <summary>
    /// Calculates the arithmetic mean of absolute forecast errors.
    /// </summary>
    /// <param name="evaluations">The prediction evaluations.</param>
    /// <returns>The mean absolute error, or <see langword="null"/> for zero samples.</returns>
    public double? Calculate(IReadOnlyList<PredictionEvaluationRecord> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);
        return evaluations.Count == 0 ? null : evaluations.Average(item => item.AbsoluteError);
    }
}
