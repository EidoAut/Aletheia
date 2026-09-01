using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Identifies one semantically comparable forecast event across models.
/// </summary>
/// <param name="FundIdentifier">The fund identifier.</param>
/// <param name="DatasetFingerprint">The dataset fingerprint used for evaluation.</param>
/// <param name="PredictionCutoffIndex">The last available observation index.</param>
/// <param name="PredictionCutoffDate">The last available observation date.</param>
/// <param name="TargetIndex">The realized target index.</param>
/// <param name="TargetDate">The realized target date.</param>
/// <param name="HorizonValue">The requested forecast horizon value.</param>
/// <param name="HorizonUnit">The requested forecast horizon unit.</param>
/// <param name="EffectiveObservationCount">The resolved observation count.</param>
public sealed record EvaluationEventKey(
    FundIdentifier FundIdentifier,
    string DatasetFingerprint,
    int PredictionCutoffIndex,
    DateOnly PredictionCutoffDate,
    int? TargetIndex,
    DateOnly? TargetDate,
    int HorizonValue,
    ForecastHorizonUnit HorizonUnit,
    int EffectiveObservationCount)
{
    /// <summary>
    /// Creates a comparison key from one ledger prediction.
    /// </summary>
    /// <param name="prediction">The prediction ledger record.</param>
    /// <returns>The comparable evaluation event key.</returns>
    public static EvaluationEventKey FromPrediction(PredictionLedgerRecord prediction)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        return new EvaluationEventKey(
            prediction.Prediction.FundIdentifier,
            prediction.Prediction.DatasetIdentity.DatasetFingerprintSha256,
            prediction.PredictionCutoffIndex,
            prediction.Prediction.DataCutoffDate,
            prediction.TargetIndex,
            prediction.TargetDate,
            prediction.Prediction.RequestedHorizon.Value,
            prediction.Prediction.RequestedHorizon.Unit,
            prediction.Prediction.EffectiveObservationCount);
    }
}
