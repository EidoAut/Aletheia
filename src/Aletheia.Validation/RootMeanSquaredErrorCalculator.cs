namespace Aletheia.Validation;

/// <summary>
/// Calculates root mean squared error in decimal return units.
/// </summary>
public sealed class RootMeanSquaredErrorCalculator
{
    private readonly MeanSquaredErrorCalculator mseCalculator = new();

    /// <summary>
    /// Calculates RMSE from prediction evaluations.
    /// </summary>
    /// <param name="evaluations">The prediction evaluations.</param>
    /// <returns>The RMSE, or <see langword="null"/> for zero samples.</returns>
    public double? Calculate(IReadOnlyList<PredictionEvaluationRecord> evaluations)
    {
        var mse = this.mseCalculator.Calculate(evaluations);
        return mse.HasValue ? Math.Sqrt(mse.Value) : null;
    }
}
