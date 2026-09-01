namespace Aletheia.Validation;

/// <summary>
/// Couples one frozen prediction with the subsequently observed outcome.
/// </summary>
public sealed class ForecastEvaluationSample
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastEvaluationSample"/> class.
    /// </summary>
    /// <param name="prediction">The frozen prediction.</param>
    /// <param name="evaluation">The realized evaluation.</param>
    public ForecastEvaluationSample(PredictionLedgerRecord prediction, PredictionEvaluationRecord evaluation)
    {
        this.Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        this.Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        this.EventKey = EvaluationEventKey.FromPrediction(prediction);
    }

    /// <summary>
    /// Gets the frozen prediction.
    /// </summary>
    public PredictionLedgerRecord Prediction { get; }

    /// <summary>
    /// Gets the realized evaluation.
    /// </summary>
    public PredictionEvaluationRecord Evaluation { get; }

    /// <summary>
    /// Gets the model-independent event key used for common-support comparison.
    /// </summary>
    public EvaluationEventKey EventKey { get; }
}
