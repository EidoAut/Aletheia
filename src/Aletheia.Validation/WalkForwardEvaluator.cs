using System.Globalization;
using System.Text;
using Aletheia.Core;
using Aletheia.Forecasting;

namespace Aletheia.Validation;

/// <summary>
/// Executes leakage-controlled walk-forward validation for forecast models.
/// </summary>
public sealed class WalkForwardEvaluator : IWalkForwardEvaluator
{
    private readonly WalkForwardSplitter splitter;
    private readonly ForecastHorizonResolver horizonResolver;
    private readonly ValidationMetricCalculator metricCalculator;
    private readonly NonOverlappingForecastSelector nonOverlappingSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalkForwardEvaluator"/> class.
    /// </summary>
    /// <param name="splitter">The split generator.</param>
    /// <param name="horizonResolver">The horizon resolver.</param>
    /// <param name="metricCalculator">The metric calculator.</param>
    /// <param name="nonOverlappingSelector">The non-overlapping selector.</param>
    public WalkForwardEvaluator(
        WalkForwardSplitter? splitter = null,
        ForecastHorizonResolver? horizonResolver = null,
        ValidationMetricCalculator? metricCalculator = null,
        NonOverlappingForecastSelector? nonOverlappingSelector = null)
    {
        this.splitter = splitter ?? new WalkForwardSplitter();
        this.horizonResolver = horizonResolver ?? new ForecastHorizonResolver();
        this.metricCalculator = metricCalculator ?? new ValidationMetricCalculator();
        this.nonOverlappingSelector = nonOverlappingSelector ?? new NonOverlappingForecastSelector();
    }

    /// <inheritdoc />
    public async Task<WalkForwardModelEvaluationResult> EvaluateAsync(
        IForecastModel model,
        ForecastEvaluationDataset dataset,
        WalkForwardEvaluationOptions options,
        IPredictionLedger? ledger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        options.EventSink.Record(new ValidationEvent(
            ValidationEventType.WalkForwardEvaluationStarted,
            model.Descriptor,
            null,
            "Walk-forward evaluation started."));

        if (ledger is not null)
        {
            await ledger.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        var samples = new List<ForecastEvaluationSample>();
        var failures = new List<ForecastFailureRecord>();
        var splits = this.splitter.CreateSplits(dataset.History.NavSeries, options);
        var runTimestamp = DateTimeOffset.UtcNow;

        foreach (var split in splits)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cutoffDate = split.PredictionCutoffDate ?? dataset.History.NavSeries[split.PredictionCutoffIndex].Date;
            var horizonResolution = this.horizonResolver.Resolve(
                options.ForecastHorizon,
                cutoffDate,
                dataset.History.NavSeries.ObservationFrequency);
            var trainingSeries = SliceByIndex(dataset.History.NavSeries, split.TrainStartIndex, split.TrainEndIndex);
            var trainingContext = new ForecastTrainingContext(dataset, trainingSeries, split, horizonResolution);
            var predictionContext = new ForecastPredictionContext(dataset, trainingSeries, split, horizonResolution);

            options.EventSink.Record(new ValidationEvent(
                ValidationEventType.ModelTrainingStarted,
                model.Descriptor,
                cutoffDate,
                "Model training started."));

            var trainingResult = model.Train(trainingContext, cancellationToken);
            if (!trainingResult.IsSuccess)
            {
                AddFailure(failures, model, split, trainingResult.Status, trainingResult.FailureReason);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.ForecastRejected,
                    model.Descriptor,
                    cutoffDate,
                    trainingResult.FailureReason ?? "Training failed."));
                continue;
            }

            var predictionResult = model.Predict(trainingResult, predictionContext, cancellationToken);
            if (!predictionResult.IsSuccess || predictionResult.Distribution is null)
            {
                AddFailure(failures, model, split, predictionResult.Status, predictionResult.FailureReason);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.ForecastRejected,
                    model.Descriptor,
                    cutoffDate,
                    predictionResult.FailureReason ?? "Prediction failed."));
                continue;
            }

            var validationFailure = ForecastOutputValidator.Validate(predictionResult.Distribution);
            if (validationFailure is not null)
            {
                AddFailure(failures, model, split, ForecastStatus.InvalidOutput, validationFailure);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.ForecastRejected,
                    model.Descriptor,
                    cutoffDate,
                    validationFailure));
                continue;
            }

            var unsupportedCapabilities = predictionResult.Distribution.Capabilities & ~model.Capabilities;
            if (unsupportedCapabilities != ForecastCapabilities.None)
            {
                var reason = $"Forecast declared capabilities not advertised by model: {unsupportedCapabilities}.";
                AddFailure(failures, model, split, ForecastStatus.InvalidOutput, reason);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.ForecastRejected,
                    model.Descriptor,
                    cutoffDate,
                    reason));
                continue;
            }

            var prediction = CreatePredictionRecord(
                model,
                dataset,
                split,
                predictionResult.Distribution,
                runTimestamp,
                trainingResult.Diagnostics,
                predictionResult.Diagnostics);
            var actualReturn = CalculateSimpleReturn(
                dataset.History.NavSeries,
                split.PredictionCutoffIndex,
                split.TargetIndex.GetValueOrDefault());
            var evaluation = PredictionEvaluationRecord.Create(
                prediction,
                actualReturn,
                runTimestamp,
                options.FlatReturnTolerance,
                options.DirectionRule);

            if (ledger is not null)
            {
                await ledger.StorePredictionAsync(prediction, cancellationToken).ConfigureAwait(false);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.PredictionStored,
                    model.Descriptor,
                    cutoffDate,
                    "Prediction stored."));
                await ledger.StoreEvaluationAsync(evaluation, cancellationToken).ConfigureAwait(false);
                options.EventSink.Record(new ValidationEvent(
                    ValidationEventType.EvaluationStored,
                    model.Descriptor,
                    cutoffDate,
                    "Evaluation stored."));
            }

            options.EventSink.Record(new ValidationEvent(
                ValidationEventType.ForecastGenerated,
                model.Descriptor,
                cutoffDate,
                "Forecast generated and evaluated."));
            samples.Add(new ForecastEvaluationSample(prediction, evaluation));
        }

        var nonOverlapping = this.nonOverlappingSelector.Select(samples);
        var result = new WalkForwardModelEvaluationResult(
            model.Descriptor,
            model.ConfigurationFingerprint,
            samples,
            nonOverlapping,
            failures,
            this.metricCalculator.Calculate(samples, options.Calibration),
            this.metricCalculator.Calculate(nonOverlapping, options.Calibration),
            samples.Count == 0 ? null : samples.Min(sample => sample.Prediction.Prediction.DataCutoffDate),
            samples.Count == 0 ? null : samples.Max(sample => sample.Prediction.TargetDate),
            new ModelCoverageDiagnostics(model.Descriptor, splits.Count, samples.Count, failures));

        options.EventSink.Record(new ValidationEvent(
            ValidationEventType.ModelEvaluationCompleted,
            model.Descriptor,
            null,
            "Model evaluation completed."));

        return result;
    }

    private static void AddFailure(
        ICollection<ForecastFailureRecord> failures,
        IForecastModel model,
        WalkForwardSplit split,
        ForecastStatus status,
        string? reason)
    {
        failures.Add(new ForecastFailureRecord(
            model.Descriptor,
            split.PredictionCutoffDate ?? DateOnly.MinValue,
            split.PredictionCutoffIndex,
            status,
            string.IsNullOrWhiteSpace(reason) ? "No failure reason supplied." : reason));
    }

    private static NavSeries SliceByIndex(NavSeries navSeries, int startIndex, int endIndex)
    {
        return new NavSeries(
            navSeries.Points.Skip(startIndex).Take(endIndex - startIndex + 1),
            navSeries.ObservationFrequency);
    }

    private static double CalculateSimpleReturn(NavSeries navSeries, int startIndex, int endIndex)
    {
        var start = navSeries[startIndex].Value;
        var end = navSeries[endIndex].Value;
        if (start <= 0m || end <= 0m)
        {
            throw new ArgumentException("Evaluation returns require strictly positive NAV values.", nameof(navSeries));
        }

        return ((double)end / (double)start) - 1d;
    }

    private static PredictionLedgerRecord CreatePredictionRecord(
        IForecastModel model,
        ForecastEvaluationDataset dataset,
        WalkForwardSplit split,
        ForecastDistribution distribution,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> trainingDiagnostics,
        IReadOnlyDictionary<string, string> predictionDiagnostics)
    {
        var diagnostics = MergeDiagnostics(trainingDiagnostics, predictionDiagnostics);
        var logicalKey = CreateLogicalKey(model, dataset, split, distribution.HorizonResolution);
        var predictionId = DeterministicPredictionIdentity.CreateGuid(logicalKey);
        var stateSchemaVersion = diagnostics.GetValueOrDefault("StateSchemaVersion", "n/a");
        var stateSchemaFingerprint = diagnostics.GetValueOrDefault("StateSchemaFingerprint", "n/a");
        var prediction = new PredictionRecord(
            predictionId,
            dataset.History.Fund.Identifier,
            generatedAtUtc,
            split.PredictionCutoffDate ?? dataset.History.NavSeries[split.PredictionCutoffIndex].Date,
            distribution.HorizonResolution,
            distribution.PointForecastReturn,
            distribution.ExpectedReturn,
            distribution.MedianReturn,
            distribution.ProbabilityPositive,
            distribution.Percentiles,
            model.Descriptor,
            model.Configuration,
            dataset.AletheiaVersion,
            stateSchemaVersion,
            stateSchemaFingerprint,
            dataset.DatasetIdentity,
            null,
            InvestmentSignal.NoReliableSignal,
            null,
            model.ConfigurationFingerprint,
            distribution.Capabilities,
            distribution.PointForecastStatistic);

        return new PredictionLedgerRecord(
            prediction,
            logicalKey,
            model.ConfigurationFingerprint,
            PredictionOrigin.HistoricalWalkForward,
            null,
            split.TrainStartIndex,
            split.TrainEndIndex,
            dataset.History.NavSeries[split.TrainStartIndex].Date,
            dataset.History.NavSeries[split.TrainEndIndex].Date,
            split.PredictionCutoffIndex,
            split.TargetIndex,
            split.TargetDate,
            diagnostics);
    }

    private static IReadOnlyDictionary<string, string> MergeDiagnostics(
        IReadOnlyDictionary<string, string> trainingDiagnostics,
        IReadOnlyDictionary<string, string> predictionDiagnostics)
    {
        var diagnostics = new Dictionary<string, string>();
        foreach (var pair in trainingDiagnostics)
        {
            diagnostics[$"Training.{pair.Key}"] = pair.Value;
        }

        foreach (var pair in predictionDiagnostics)
        {
            diagnostics[$"Prediction.{pair.Key}"] = pair.Value;
        }

        return diagnostics;
    }

    private static string CreateLogicalKey(
        IForecastModel model,
        ForecastEvaluationDataset dataset,
        WalkForwardSplit split,
        ForecastHorizonResolution resolution)
    {
        var builder = new StringBuilder()
            .Append("Fund=").Append(dataset.History.Fund.Identifier).Append('\n')
            .Append("Origin=").Append(PredictionOrigin.HistoricalWalkForward).Append('\n')
            .Append("DatasetFingerprint=").Append(dataset.DatasetIdentity.DatasetFingerprintSha256).Append('\n')
            .Append("CutoffIndex=").Append(split.PredictionCutoffIndex.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("CutoffDate=").Append((split.PredictionCutoffDate ?? DateOnly.MinValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('\n')
            .Append("TrainingStartIndex=").Append(split.TrainStartIndex.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("TrainingEndIndex=").Append(split.TrainEndIndex.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("TargetIndex=").Append(split.TargetIndex.GetValueOrDefault(-1).ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("HorizonValue=").Append(resolution.RequestedHorizon.Value.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append("HorizonUnit=").Append(resolution.RequestedHorizon.Unit).Append('\n')
            .Append("ModelId=").Append(model.Descriptor.Id).Append('\n')
            .Append("ModelVersion=").Append(model.Descriptor.Version).Append('\n')
            .Append("ModelConfigurationFingerprint=").Append(model.ConfigurationFingerprint).Append('\n');

        return builder.ToString();
    }
}
