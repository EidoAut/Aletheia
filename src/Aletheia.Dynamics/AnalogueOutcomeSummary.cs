namespace Aletheia.Dynamics;

/// <summary>
/// Summarizes future returns after historical analogue states.
/// </summary>
public sealed class AnalogueOutcomeSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalogueOutcomeSummary"/> class.
    /// </summary>
    /// <param name="matchCount">The number of analogue outcomes.</param>
    /// <param name="probabilityPositive">The fraction of positive outcomes.</param>
    /// <param name="meanReturn">The mean subsequent return.</param>
    /// <param name="medianReturn">The median subsequent return.</param>
    /// <param name="percentile25">The 25th percentile subsequent return.</param>
    /// <param name="percentile75">The 75th percentile subsequent return.</param>
    /// <param name="worstReturn">The worst subsequent return.</param>
    /// <param name="bestReturn">The best subsequent return.</param>
    public AnalogueOutcomeSummary(
        int matchCount,
        double probabilityPositive,
        double meanReturn,
        double medianReturn,
        double percentile25,
        double percentile75,
        double worstReturn,
        double bestReturn)
    {
        this.MatchCount = matchCount;
        this.ProbabilityPositive = probabilityPositive;
        this.MeanReturn = meanReturn;
        this.MedianReturn = medianReturn;
        this.Percentile25 = percentile25;
        this.Percentile75 = percentile75;
        this.WorstReturn = worstReturn;
        this.BestReturn = bestReturn;
    }

    /// <summary>
    /// Gets the number of analogue states with observable outcomes.
    /// </summary>
    public int MatchCount { get; }

    /// <summary>
    /// Gets the fraction of outcomes with positive returns.
    /// </summary>
    public double ProbabilityPositive { get; }

    /// <summary>
    /// Gets the mean subsequent return.
    /// </summary>
    public double MeanReturn { get; }

    /// <summary>
    /// Gets the median subsequent return.
    /// </summary>
    public double MedianReturn { get; }

    /// <summary>
    /// Gets the 25th percentile subsequent return.
    /// </summary>
    public double Percentile25 { get; }

    /// <summary>
    /// Gets the 75th percentile subsequent return.
    /// </summary>
    public double Percentile75 { get; }

    /// <summary>
    /// Gets the worst subsequent return.
    /// </summary>
    public double WorstReturn { get; }

    /// <summary>
    /// Gets the best subsequent return.
    /// </summary>
    public double BestReturn { get; }
}
