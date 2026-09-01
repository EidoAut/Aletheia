namespace Aletheia.Forecasting;

/// <summary>
/// Stores an evidence-weighted ensemble forecast.
/// </summary>
/// <param name="Distribution">The combined distribution.</param>
/// <param name="Components">The normalized model weights.</param>
/// <param name="ModelDisagreement">Weighted standard deviation of point forecasts.</param>
/// <param name="Reliability">A normalized reliability score in [0, 1].</param>
/// <param name="Diagnostic">A human-readable diagnostic.</param>
public sealed record ForecastEnsembleResult(
    ForecastDistribution? Distribution,
    IReadOnlyList<ForecastEnsembleComponent> Components,
    double ModelDisagreement,
    double Reliability,
    string Diagnostic);
