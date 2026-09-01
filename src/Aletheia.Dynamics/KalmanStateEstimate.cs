namespace Aletheia.Dynamics;

/// <summary>
/// Stores one Kalman filter state estimate.
/// </summary>
/// <param name="Index">The observation index.</param>
/// <param name="Observation">The observed value.</param>
/// <param name="Level">The filtered latent level.</param>
/// <param name="Trend">The filtered latent trend.</param>
/// <param name="LevelVariance">The filtered level variance.</param>
/// <param name="TrendVariance">The filtered trend variance.</param>
/// <param name="Innovation">The one-step forecast innovation.</param>
/// <param name="InnovationVariance">The one-step innovation variance.</param>
/// <param name="LevelTrendCovariance">The filtered covariance between latent level and trend.</param>
public sealed record KalmanStateEstimate(
    int Index,
    double Observation,
    double Level,
    double Trend,
    double LevelVariance,
    double TrendVariance,
    double Innovation,
    double InnovationVariance,
    double LevelTrendCovariance = 0d);
