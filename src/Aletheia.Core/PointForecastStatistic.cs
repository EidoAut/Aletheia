namespace Aletheia.Core;

/// <summary>
/// Identifies the statistic represented by a forecast's principal point value.
/// </summary>
public enum PointForecastStatistic
{
    /// <summary>
    /// No point forecast is supported.
    /// </summary>
    None = 0,

    /// <summary>
    /// The point is a model-defined point estimate that is not necessarily the
    /// mean or median of an explicit distribution.
    /// </summary>
    ExplicitModelPoint = 1,

    /// <summary>
    /// The point is the forecast mean.
    /// </summary>
    Mean = 2,

    /// <summary>
    /// The point is the forecast median.
    /// </summary>
    Median = 3,
}
