namespace Aletheia.Validation;

/// <summary>
/// Configures cross-model comparison semantics for the Model Arena.
/// </summary>
public sealed class ModelArenaOptions
{
    /// <summary>
    /// Gets or sets the model id used as the point-forecast baseline.
    /// </summary>
    public string PointForecastBaselineModelId { get; set; } = ZeroReturnForecastModel.ModelId;

    /// <summary>
    /// Gets or sets the model id used as the probability-forecast baseline.
    /// </summary>
    public string ProbabilityBaselineModelId { get; set; } = HistoricalProbabilityBaselineForecastModel.ModelId;

    /// <summary>
    /// Gets or sets the minimum all-sample count required for ranking eligibility.
    /// </summary>
    public int MinimumAllSamples { get; set; }

    /// <summary>
    /// Gets or sets the minimum common-support count required for ranking.
    /// When omitted, the walk-forward minimum evaluation sample count is used.
    /// </summary>
    public int? MinimumCommonSupportSamples { get; set; }

    /// <summary>
    /// Gets or sets the minimum non-overlapping sample count required for ranking eligibility.
    /// </summary>
    public int MinimumNonOverlappingSamples { get; set; }

    /// <summary>
    /// Resolves the minimum common-support count.
    /// </summary>
    /// <param name="walkForwardOptions">The walk-forward options.</param>
    /// <returns>The resolved sample count.</returns>
    public int ResolveMinimumCommonSupportSamples(WalkForwardEvaluationOptions walkForwardOptions)
    {
        ArgumentNullException.ThrowIfNull(walkForwardOptions);
        return this.MinimumCommonSupportSamples ?? walkForwardOptions.MinimumEvaluationSamples;
    }

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.PointForecastBaselineModelId))
        {
            throw new ArgumentException("Point forecast baseline model id cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(this.ProbabilityBaselineModelId))
        {
            throw new ArgumentException("Probability baseline model id cannot be empty.");
        }

        if (this.MinimumAllSamples < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MinimumAllSamples), this.MinimumAllSamples, "Minimum all-sample count cannot be negative.");
        }

        if (this.MinimumCommonSupportSamples.HasValue && this.MinimumCommonSupportSamples.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.MinimumCommonSupportSamples),
                this.MinimumCommonSupportSamples.Value,
                "Minimum common-support count cannot be negative.");
        }

        if (this.MinimumNonOverlappingSamples < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.MinimumNonOverlappingSamples),
                this.MinimumNonOverlappingSamples,
                "Minimum non-overlapping count cannot be negative.");
        }
    }
}
