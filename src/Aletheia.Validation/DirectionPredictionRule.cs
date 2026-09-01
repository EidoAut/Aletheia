namespace Aletheia.Validation;

/// <summary>
/// Selects the forecast quantity used to classify predicted direction.
/// </summary>
public enum DirectionPredictionRule
{
    /// <summary>
    /// Use probability-positive thresholding when available, otherwise use the
    /// declared principal point forecast when available.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Classify direction from the sign of the principal point forecast.
    /// </summary>
    PointForecastSign = 1,

    /// <summary>
    /// Classify direction from the sign of the forecast median.
    /// </summary>
    MedianSign = 2,

    /// <summary>
    /// Classify direction from whether P(return &gt; 0) is strictly above 0.5.
    /// </summary>
    ProbabilityPositiveThreshold = 3,
}
