using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Stores an immutable prediction plus walk-forward audit metadata.
/// </summary>
public sealed class PredictionLedgerRecord
{
    private readonly IReadOnlyDictionary<string, string> diagnosticMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionLedgerRecord"/> class.
    /// </summary>
    /// <param name="prediction">The immutable core prediction.</param>
    /// <param name="logicalKey">The stable logical key used for idempotency.</param>
    /// <param name="modelConfigurationFingerprint">The model configuration fingerprint.</param>
    /// <param name="origin">The prediction origin.</param>
    /// <param name="simulatedGeneratedAtUtc">The simulated generation timestamp for historical predictions.</param>
    /// <param name="trainingStartIndex">The inclusive training start index.</param>
    /// <param name="trainingEndIndex">The inclusive training end index.</param>
    /// <param name="trainingStartDate">The inclusive training start date.</param>
    /// <param name="trainingEndDate">The inclusive training end date.</param>
    /// <param name="predictionCutoffIndex">The prediction cutoff index.</param>
    /// <param name="targetIndex">The target index.</param>
    /// <param name="targetDate">The target date.</param>
    /// <param name="diagnosticMetadata">Diagnostic metadata captured with the prediction.</param>
    /// <param name="contentFingerprint">The deterministic scientific content fingerprint.</param>
    public PredictionLedgerRecord(
        PredictionRecord prediction,
        string logicalKey,
        string modelConfigurationFingerprint,
        PredictionOrigin origin,
        DateTimeOffset? simulatedGeneratedAtUtc,
        int trainingStartIndex,
        int trainingEndIndex,
        DateOnly trainingStartDate,
        DateOnly trainingEndDate,
        int predictionCutoffIndex,
        int? targetIndex,
        DateOnly? targetDate,
        IReadOnlyDictionary<string, string> diagnosticMetadata,
        string? contentFingerprint = null)
    {
        this.Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        this.LogicalKey = string.IsNullOrWhiteSpace(logicalKey)
            ? throw new ArgumentException("Logical key cannot be empty.", nameof(logicalKey))
            : logicalKey;
        this.ModelConfigurationFingerprint = string.IsNullOrWhiteSpace(modelConfigurationFingerprint)
            ? throw new ArgumentException("Model configuration fingerprint cannot be empty.", nameof(modelConfigurationFingerprint))
            : modelConfigurationFingerprint;
        this.Origin = origin;
        this.SimulatedGeneratedAtUtc = simulatedGeneratedAtUtc;
        this.TrainingStartIndex = trainingStartIndex;
        this.TrainingEndIndex = trainingEndIndex;
        this.TrainingStartDate = trainingStartDate;
        this.TrainingEndDate = trainingEndDate;
        this.PredictionCutoffIndex = predictionCutoffIndex;
        this.TargetIndex = targetIndex;
        this.TargetDate = targetDate;
        this.diagnosticMetadata = new Dictionary<string, string>(diagnosticMetadata ?? throw new ArgumentNullException(nameof(diagnosticMetadata)));
        this.ContentFingerprint = string.IsNullOrWhiteSpace(contentFingerprint)
            ? CalculateContentFingerprint(this)
            : contentFingerprint;
    }

    /// <summary>
    /// Gets the immutable core prediction.
    /// </summary>
    public PredictionRecord Prediction { get; }

    /// <summary>
    /// Gets the stable logical key used for idempotency.
    /// </summary>
    public string LogicalKey { get; }

    /// <summary>
    /// Gets the model configuration fingerprint.
    /// </summary>
    public string ModelConfigurationFingerprint { get; }

    /// <summary>
    /// Gets the prediction origin.
    /// </summary>
    public PredictionOrigin Origin { get; }

    /// <summary>
    /// Gets the simulated generation timestamp for historical predictions, when applicable.
    /// </summary>
    public DateTimeOffset? SimulatedGeneratedAtUtc { get; }

    /// <summary>
    /// Gets the inclusive training start index in the original dataset.
    /// </summary>
    public int TrainingStartIndex { get; }

    /// <summary>
    /// Gets the inclusive training end index in the original dataset.
    /// </summary>
    public int TrainingEndIndex { get; }

    /// <summary>
    /// Gets the inclusive training start date.
    /// </summary>
    public DateOnly TrainingStartDate { get; }

    /// <summary>
    /// Gets the inclusive training end date.
    /// </summary>
    public DateOnly TrainingEndDate { get; }

    /// <summary>
    /// Gets the cutoff index available to the prediction.
    /// </summary>
    public int PredictionCutoffIndex { get; }

    /// <summary>
    /// Gets the target index in the original dataset.
    /// </summary>
    public int? TargetIndex { get; }

    /// <summary>
    /// Gets the target date.
    /// </summary>
    public DateOnly? TargetDate { get; }

    /// <summary>
    /// Gets diagnostic metadata captured with the prediction.
    /// </summary>
    public IReadOnlyDictionary<string, string> DiagnosticMetadata => this.diagnosticMetadata;

    /// <summary>
    /// Gets the deterministic scientific content fingerprint.
    /// </summary>
    public string ContentFingerprint { get; }

    private static string CalculateContentFingerprint(PredictionLedgerRecord record)
    {
        var prediction = record.Prediction;
        var builder = new StringBuilder()
            .Append("ModelId=").Append(prediction.Model.Id).Append('\n')
            .Append("ModelVersion=").Append(prediction.Model.Version).Append('\n')
            .Append("ModelConfigurationFingerprint=").Append(record.ModelConfigurationFingerprint).Append('\n')
            .Append("FeatureConfigurationId=").Append(prediction.FeatureConfigurationId).Append('\n')
            .Append("DatasetProvider=").Append(prediction.DatasetIdentity.DataProvider).Append('\n')
            .Append("DatasetFingerprint=").Append(prediction.DatasetIdentity.DatasetFingerprintSha256).Append('\n')
            .Append("StateSchemaVersion=").Append(prediction.StateSchemaVersion).Append('\n')
            .Append("StateSchemaFingerprint=").Append(prediction.StateSchemaFingerprint).Append('\n')
            .Append("Fund=").Append(prediction.FundIdentifier).Append('\n')
            .Append("CutoffIndex=").Append(record.PredictionCutoffIndex.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("CutoffDate=").Append(prediction.DataCutoffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('\n')
            .Append("TargetIndex=").Append(record.TargetIndex?.ToString(CultureInfo.InvariantCulture) ?? "n/a").Append('\n')
            .Append("TargetDate=").Append(record.TargetDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "n/a").Append('\n')
            .Append("HorizonValue=").Append(prediction.RequestedHorizon.Value.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("HorizonUnit=").Append(prediction.RequestedHorizon.Unit).Append('\n')
            .Append("EffectiveObservationCount=").Append(prediction.EffectiveObservationCount.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("Capabilities=").Append(prediction.ForecastCapabilities).Append('\n')
            .Append("PointForecastStatistic=").Append(prediction.PointForecastStatistic).Append('\n')
            .Append("PointForecastReturn=").Append(prediction.PointForecastReturn.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("ExpectedReturn=").Append(prediction.ExpectedReturn.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("MedianReturn=").Append(prediction.MedianReturn.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("ProbabilityPositive=").Append(prediction.ProbabilityPositive.ToString("G17", CultureInfo.InvariantCulture)).Append('\n')
            .Append("RandomSeed=").Append(prediction.RandomSeed?.ToString(CultureInfo.InvariantCulture) ?? "n/a").Append('\n');

        foreach (var pair in prediction.ReturnPercentiles.OrderBy(item => item.Key))
        {
            builder
                .Append("Quantile.")
                .Append(pair.Key.ToString(CultureInfo.InvariantCulture))
                .Append('=')
                .Append(pair.Value.ToString("G17", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        foreach (var pair in prediction.ModelParameters.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.Append("ModelParameter.").Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
        }

        foreach (var pair in record.DiagnosticMetadata.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.Append("Diagnostic.").Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}
