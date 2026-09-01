using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores the realized outcome of a previously issued prediction.
/// </summary>
public sealed class PredictionEvaluation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionEvaluation"/> class.
    /// </summary>
    /// <param name="prediction">The evaluated prediction.</param>
    /// <param name="realizedReturn">The realized simple return.</param>
    /// <param name="absoluteError">The absolute error versus expected return.</param>
    /// <param name="wasInsideInterquartileRange">A value indicating whether the outcome landed between p25 and p75.</param>
    public PredictionEvaluation(
        PredictionRecord prediction,
        double realizedReturn,
        double absoluteError,
        bool wasInsideInterquartileRange)
    {
        this.Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        this.RealizedReturn = realizedReturn;
        this.AbsoluteError = absoluteError;
        this.WasInsideInterquartileRange = wasInsideInterquartileRange;
    }

    /// <summary>
    /// Gets the evaluated prediction.
    /// </summary>
    public PredictionRecord Prediction { get; }

    /// <summary>
    /// Gets the realized simple return.
    /// </summary>
    public double RealizedReturn { get; }

    /// <summary>
    /// Gets the absolute error versus expected return.
    /// </summary>
    public double AbsoluteError { get; }

    /// <summary>
    /// Gets a value indicating whether the outcome landed between p25 and p75.
    /// </summary>
    public bool WasInsideInterquartileRange { get; }
}
