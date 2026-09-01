namespace Aletheia.Data;

/// <summary>
/// Summarizes data-quality diagnostics for a NAV series.
/// </summary>
public sealed class DataQualityReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataQualityReport"/> class.
    /// </summary>
    /// <param name="observationCount">The raw observation count.</param>
    /// <param name="duplicateObservationCount">The duplicate observation count.</param>
    /// <param name="nonPositiveValueCount">The non-positive NAV count.</param>
    /// <param name="largeGapCount">The large adjacent-gap count.</param>
    /// <param name="missingBusinessDayCount">The missing business-day count.</param>
    /// <param name="suspiciousJumpCount">The suspicious jump count.</param>
    /// <param name="staleObservationCount">The stale repeated-value observation count.</param>
    /// <param name="coverageRatio">The observed-to-expected business-day coverage ratio.</param>
    /// <param name="qualityScore">The bounded quality score.</param>
    /// <param name="hasSufficientHistory">A value indicating whether the history is long enough.</param>
    public DataQualityReport(
        int observationCount,
        int duplicateObservationCount,
        int nonPositiveValueCount,
        int largeGapCount,
        int missingBusinessDayCount,
        int suspiciousJumpCount,
        int staleObservationCount,
        double coverageRatio,
        int qualityScore,
        bool hasSufficientHistory)
    {
        this.ObservationCount = observationCount;
        this.DuplicateObservationCount = duplicateObservationCount;
        this.NonPositiveValueCount = nonPositiveValueCount;
        this.LargeGapCount = largeGapCount;
        this.MissingBusinessDayCount = missingBusinessDayCount;
        this.SuspiciousJumpCount = suspiciousJumpCount;
        this.StaleObservationCount = staleObservationCount;
        this.CoverageRatio = coverageRatio;
        this.QualityScore = qualityScore;
        this.HasSufficientHistory = hasSufficientHistory;
    }

    /// <summary>
    /// Gets the raw observation count.
    /// </summary>
    public int ObservationCount { get; }

    /// <summary>
    /// Gets the number of duplicated observations beyond the first occurrence of each date.
    /// </summary>
    public int DuplicateObservationCount { get; }

    /// <summary>
    /// Gets the number of zero or negative NAV values.
    /// </summary>
    public int NonPositiveValueCount { get; }

    /// <summary>
    /// Gets the number of adjacent gaps above the configured threshold.
    /// </summary>
    public int LargeGapCount { get; }

    /// <summary>
    /// Gets the number of missing weekdays between the first and last observation.
    /// </summary>
    public int MissingBusinessDayCount { get; }

    /// <summary>
    /// Gets the number of absolute log-return jumps above the configured threshold.
    /// </summary>
    public int SuspiciousJumpCount { get; }

    /// <summary>
    /// Gets the number of observations that belong to stale repeated-value streaks.
    /// </summary>
    public int StaleObservationCount { get; }

    /// <summary>
    /// Gets observed weekdays divided by expected weekdays in the covered date range.
    /// </summary>
    public double CoverageRatio { get; }

    /// <summary>
    /// Gets a bounded quality score from 0 to 100.
    /// </summary>
    public int QualityScore { get; }

    /// <summary>
    /// Gets a value indicating whether the series has enough history for initial analytics.
    /// </summary>
    public bool HasSufficientHistory { get; }
}
