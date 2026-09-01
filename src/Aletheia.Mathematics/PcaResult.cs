namespace Aletheia.Mathematics;

/// <summary>
/// Contains the first principal component and projected scores.
/// </summary>
public sealed class PcaResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PcaResult"/> class.
    /// </summary>
    /// <param name="means">The feature means used for centering.</param>
    /// <param name="standardDeviations">The feature standard deviations used for scaling.</param>
    /// <param name="firstComponentLoadings">The normalized first-component loadings.</param>
    /// <param name="scores">The projection of each observation onto the first component.</param>
    /// <param name="explainedVariance">The first eigenvalue of the covariance matrix.</param>
    /// <param name="explainedVarianceRatio">The share of total variance explained by the first component.</param>
    public PcaResult(
        double[] means,
        double[] standardDeviations,
        double[] firstComponentLoadings,
        double[] scores,
        double explainedVariance,
        double explainedVarianceRatio)
    {
        this.Means = means;
        this.StandardDeviations = standardDeviations;
        this.FirstComponentLoadings = firstComponentLoadings;
        this.Scores = scores;
        this.ExplainedVariance = explainedVariance;
        this.ExplainedVarianceRatio = explainedVarianceRatio;
    }

    /// <summary>
    /// Gets the feature means used for centering.
    /// </summary>
    public double[] Means { get; }

    /// <summary>
    /// Gets the feature standard deviations used for scaling.
    /// </summary>
    public double[] StandardDeviations { get; }

    /// <summary>
    /// Gets the normalized loadings of the first principal component.
    /// </summary>
    public double[] FirstComponentLoadings { get; }

    /// <summary>
    /// Gets the projection of each observation onto the first component.
    /// </summary>
    public double[] Scores { get; }

    /// <summary>
    /// Gets the first eigenvalue of the covariance matrix.
    /// </summary>
    public double ExplainedVariance { get; }

    /// <summary>
    /// Gets the share of total variance explained by the first component.
    /// </summary>
    public double ExplainedVarianceRatio { get; }
}
