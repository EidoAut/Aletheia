namespace Aletheia.Validation;

/// <summary>
/// Describes why a metric value is or is not available.
/// </summary>
public enum MetricStatus
{
    /// <summary>
    /// The metric was calculated from one or more supported samples.
    /// </summary>
    Available = 0,

    /// <summary>
    /// There were no evaluated samples in the requested support set.
    /// </summary>
    NoSamples = 1,

    /// <summary>
    /// The model does not support the forecast quantity required by the metric.
    /// </summary>
    NotSupported = 2,

    /// <summary>
    /// Samples exist, but too few are available for the configured comparison.
    /// </summary>
    InsufficientSamples = 3,
}
