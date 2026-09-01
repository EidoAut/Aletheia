using Aletheia.Core;

namespace Aletheia.Forecasting;

/// <summary>
/// Describes one forecast candidate supplied to an evidence-weighted ensemble.
/// </summary>
/// <param name="ModelId">The stable model id.</param>
/// <param name="Distribution">The model forecast distribution.</param>
/// <param name="ValidatedLoss">The out-of-sample validation loss; lower is better.</param>
/// <param name="CalibrationPenalty">The optional non-negative calibration penalty.</param>
/// <param name="Eligible">A value indicating whether validation evidence is sufficient.</param>
/// <param name="ValidationHorizon">The horizon whose OOS metrics produced the loss and calibration penalty.</param>
/// <param name="EffectiveOosSampleCount">The effective same-horizon OOS sample count.</param>
public sealed record ForecastEnsembleMember(
    string ModelId,
    ForecastDistribution Distribution,
    double ValidatedLoss,
    double CalibrationPenalty,
    bool Eligible,
    ForecastHorizon? ValidationHorizon = null,
    int EffectiveOosSampleCount = 0);
