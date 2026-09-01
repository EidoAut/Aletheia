using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores realized evaluation metrics for one immutable prediction.
/// </summary>
public sealed class PredictionEvaluationRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionEvaluationRecord"/> class.
    /// </summary>
    /// <param name="predictionEvaluationId">The stable evaluation identifier.</param>
    /// <param name="predictionId">The evaluated prediction identifier.</param>
    /// <param name="evaluatedAtUtc">The evaluation timestamp.</param>
    /// <param name="actualReturn">The realized simple return.</param>
    /// <param name="actualDirection">The realized direction.</param>
    /// <param name="predictedDirection">The forecast direction.</param>
    /// <param name="absoluteError">Absolute error in decimal return units.</param>
    /// <param name="squaredError">Squared error in squared decimal return units.</param>
    /// <param name="directionCorrect">Whether predicted and actual directions match.</param>
    /// <param name="probabilityOutcome">The binary positive-return outcome.</param>
    /// <param name="brierContribution">This prediction's Brier-score contribution.</param>
    /// <param name="directionRule">The concrete direction rule used.</param>
    /// <param name="evaluationContentFingerprint">The deterministic scientific evaluation-content fingerprint.</param>
    public PredictionEvaluationRecord(
        Guid predictionEvaluationId,
        Guid predictionId,
        DateTimeOffset evaluatedAtUtc,
        double actualReturn,
        ForecastDirection actualDirection,
        ForecastDirection predictedDirection,
        double absoluteError,
        double squaredError,
        bool directionCorrect,
        int probabilityOutcome,
        double brierContribution,
        DirectionPredictionRule directionRule = DirectionPredictionRule.Automatic,
        string? evaluationContentFingerprint = null)
    {
        this.PredictionEvaluationId = predictionEvaluationId;
        this.PredictionId = predictionId;
        this.EvaluatedAtUtc = evaluatedAtUtc;
        this.ActualReturn = actualReturn;
        this.ActualDirection = actualDirection;
        this.PredictedDirection = predictedDirection;
        this.AbsoluteError = absoluteError;
        this.SquaredError = squaredError;
        this.DirectionCorrect = directionCorrect;
        this.ProbabilityOutcome = probabilityOutcome;
        this.BrierContribution = brierContribution;
        this.DirectionRule = directionRule;
        this.EvaluationContentFingerprint = string.IsNullOrWhiteSpace(evaluationContentFingerprint)
            ? CalculateContentFingerprint(
                predictionEvaluationId,
                predictionId,
                actualReturn,
                actualDirection,
                predictedDirection,
                absoluteError,
                squaredError,
                directionCorrect,
                probabilityOutcome,
                brierContribution,
                directionRule)
            : evaluationContentFingerprint;
    }

    /// <summary>
    /// Gets the stable evaluation identifier.
    /// </summary>
    public Guid PredictionEvaluationId { get; }

    /// <summary>
    /// Gets the evaluated prediction identifier.
    /// </summary>
    public Guid PredictionId { get; }

    /// <summary>
    /// Gets the UTC timestamp when evaluation was generated.
    /// </summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>
    /// Gets the realized simple return.
    /// </summary>
    public double ActualReturn { get; }

    /// <summary>
    /// Gets the realized direction.
    /// </summary>
    public ForecastDirection ActualDirection { get; }

    /// <summary>
    /// Gets the forecast direction.
    /// </summary>
    public ForecastDirection PredictedDirection { get; }

    /// <summary>
    /// Gets absolute error in decimal return units.
    /// </summary>
    public double AbsoluteError { get; }

    /// <summary>
    /// Gets squared error in squared decimal return units.
    /// </summary>
    public double SquaredError { get; }

    /// <summary>
    /// Gets a value indicating whether predicted and actual directions match.
    /// </summary>
    public bool DirectionCorrect { get; }

    /// <summary>
    /// Gets the binary positive-return outcome used by Brier score.
    /// </summary>
    public int ProbabilityOutcome { get; }

    /// <summary>
    /// Gets this prediction's Brier-score contribution.
    /// </summary>
    public double BrierContribution { get; }

    /// <summary>
    /// Gets the concrete direction rule used for this evaluation.
    /// </summary>
    public DirectionPredictionRule DirectionRule { get; }

    /// <summary>
    /// Gets the deterministic scientific evaluation-content fingerprint.
    /// </summary>
    public string EvaluationContentFingerprint { get; }

    /// <summary>
    /// Creates an evaluation from a prediction record and realized return.
    /// </summary>
    /// <param name="prediction">The prediction ledger record.</param>
    /// <param name="actualReturn">The realized simple return.</param>
    /// <param name="evaluatedAtUtc">The evaluation timestamp.</param>
    /// <param name="flatReturnTolerance">The return tolerance used to classify flat directions.</param>
    /// <param name="directionRule">The configured forecast direction rule.</param>
    /// <returns>The evaluation record.</returns>
    public static PredictionEvaluationRecord Create(
        PredictionLedgerRecord prediction,
        double actualReturn,
        DateTimeOffset evaluatedAtUtc,
        double flatReturnTolerance,
        DirectionPredictionRule directionRule = DirectionPredictionRule.Automatic)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        if (double.IsNaN(actualReturn) || double.IsInfinity(actualReturn))
        {
            throw new ArgumentException("Actual return must be finite.", nameof(actualReturn));
        }

        var supportsPoint = prediction.Prediction.Supports(ForecastCapabilities.PointForecast);
        var forecast = supportsPoint ? prediction.Prediction.PointForecastReturn : 0d;
        var error = supportsPoint ? actualReturn - forecast : 0d;
        var actualDirection = DirectionClassifier.Classify(actualReturn, flatReturnTolerance);
        var concreteDirectionRule = DirectionPredictionPolicy.ResolveRule(prediction.Prediction, directionRule);
        var predictedDirection = DirectionPredictionPolicy.Classify(prediction, concreteDirectionRule, flatReturnTolerance);
        var outcome = actualReturn > 0d ? 1 : 0;
        var probabilityError = prediction.Prediction.Supports(ForecastCapabilities.ProbabilityPositive)
            ? prediction.Prediction.ProbabilityPositive - outcome
            : 0d;
        var logicalEvaluationKey = $"{prediction.LogicalKey}|evaluation";

        return new PredictionEvaluationRecord(
            DeterministicPredictionIdentity.CreateGuid(logicalEvaluationKey),
            prediction.Prediction.PredictionId,
            evaluatedAtUtc,
            actualReturn,
            actualDirection,
            predictedDirection,
            Math.Abs(error),
            error * error,
            actualDirection == predictedDirection,
            outcome,
            probabilityError * probabilityError,
            concreteDirectionRule);
    }

    private static string CalculateContentFingerprint(
        Guid predictionEvaluationId,
        Guid predictionId,
        double actualReturn,
        ForecastDirection actualDirection,
        ForecastDirection predictedDirection,
        double absoluteError,
        double squaredError,
        bool directionCorrect,
        int probabilityOutcome,
        double brierContribution,
        DirectionPredictionRule directionRule)
    {
        var builder = new StringBuilder()
            .Append("PredictionEvaluationId=").Append(predictionEvaluationId).Append('\n')
            .Append("PredictionId=").Append(predictionId).Append('\n')
            .Append("ActualReturn=").Append(actualReturn.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("ActualDirection=").Append(actualDirection).Append('\n')
            .Append("PredictedDirection=").Append(predictedDirection).Append('\n')
            .Append("AbsoluteError=").Append(absoluteError.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("SquaredError=").Append(squaredError.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("DirectionCorrect=").Append(directionCorrect ? "true" : "false").Append('\n')
            .Append("ProbabilityOutcome=").Append(probabilityOutcome.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("BrierContribution=").Append(brierContribution.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("DirectionRule=").Append(directionRule).Append('\n');

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}
