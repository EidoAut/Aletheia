namespace Aletheia.Core;

/// <summary>
/// Provides common dimensions used by the first dynamic-state estimator.
/// </summary>
public static class StandardStateDimensions
{
    /// <summary>
    /// A recent simple-return estimate.
    /// </summary>
    public static readonly StateDimension SimpleReturn = new("SimpleReturn");

    /// <summary>
    /// A recent logarithmic-return estimate.
    /// </summary>
    public static readonly StateDimension LogReturn = new("LogReturn");

    /// <summary>
    /// A first-order trend estimate.
    /// </summary>
    public static readonly StateDimension Trend = new("Trend");

    /// <summary>
    /// A momentum estimate over a finite lookback window.
    /// </summary>
    public static readonly StateDimension Momentum = new("Momentum");

    /// <summary>
    /// A realized volatility estimate.
    /// </summary>
    public static readonly StateDimension Volatility = new("Volatility");

    /// <summary>
    /// The current drawdown from the running high-water mark.
    /// </summary>
    public static readonly StateDimension Drawdown = new("Drawdown");

    /// <summary>
    /// A first numerical derivative of the smoothed NAV signal.
    /// </summary>
    public static readonly StateDimension LogNavVelocityPerObservation = new("LogNavVelocityPerObservation");

    /// <summary>
    /// A second numerical derivative of the smoothed NAV signal.
    /// </summary>
    public static readonly StateDimension LogNavAccelerationPerObservationSquared = new("LogNavAccelerationPerObservationSquared");
}
