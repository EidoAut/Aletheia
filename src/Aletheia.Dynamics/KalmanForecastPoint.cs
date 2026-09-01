namespace Aletheia.Dynamics;

/// <summary>
/// Stores one state-space forecast point.
/// </summary>
/// <param name="Step">The one-based forecast step.</param>
/// <param name="ExpectedValue">The forecast expectation in input units.</param>
/// <param name="Variance">The forecast variance in input units squared.</param>
/// <param name="Lower95">The approximate lower 95% prediction bound.</param>
/// <param name="Upper95">The approximate upper 95% prediction bound.</param>
public sealed record KalmanForecastPoint(
    int Step,
    double ExpectedValue,
    double Variance,
    double Lower95,
    double Upper95);
