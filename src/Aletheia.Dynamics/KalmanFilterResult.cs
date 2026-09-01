namespace Aletheia.Dynamics;

/// <summary>
/// Stores Kalman filter output for a local linear trend model.
/// </summary>
/// <param name="Estimates">The filtered state estimates.</param>
/// <param name="LogLikelihood">The Gaussian log likelihood.</param>
/// <param name="ObservationVariance">The observation noise variance.</param>
/// <param name="LevelVariance">The level process variance.</param>
/// <param name="TrendVariance">The trend process variance.</param>
public sealed record KalmanFilterResult(
    IReadOnlyList<KalmanStateEstimate> Estimates,
    double LogLikelihood,
    double ObservationVariance,
    double LevelVariance,
    double TrendVariance)
{
    /// <summary>
    /// Gets the final filtered estimate.
    /// </summary>
    public KalmanStateEstimate? LastEstimate => this.Estimates.Count == 0 ? null : this.Estimates[^1];
}
