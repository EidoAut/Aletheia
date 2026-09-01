namespace Aletheia.Forecasting;

/// <summary>
/// Stores one normalized ensemble component.
/// </summary>
/// <param name="ModelId">The stable model id.</param>
/// <param name="Weight">The normalized ensemble weight.</param>
/// <param name="ValidatedLoss">The validation loss used for weighting.</param>
/// <param name="CalibrationPenalty">The calibration penalty used for weighting.</param>
public sealed record ForecastEnsembleComponent(
    string ModelId,
    double Weight,
    double ValidatedLoss,
    double CalibrationPenalty);
