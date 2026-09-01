using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Creates chronological train/test splits for out-of-sample validation.
/// </summary>
public sealed class WalkForwardSplitter
{
    /// <summary>
    /// Creates expanding-window walk-forward splits.
    /// </summary>
    /// <param name="observationCount">The total number of observations.</param>
    /// <param name="initialTrainingSize">The first training-window size.</param>
    /// <param name="testSize">The test-window size.</param>
    /// <param name="stepSize">The number of observations to advance after each split.</param>
    /// <returns>Chronological walk-forward splits.</returns>
    public IReadOnlyList<WalkForwardSplit> CreateExpandingWindowSplits(
        int observationCount,
        int initialTrainingSize,
        int testSize,
        int stepSize)
    {
        if (observationCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(observationCount), observationCount, "Observation count must be positive.");
        }

        if (initialTrainingSize <= 0 || testSize <= 0 || stepSize <= 0)
        {
            throw new ArgumentException("Training, test, and step sizes must be positive.");
        }

        var splits = new List<WalkForwardSplit>();
        var trainEnd = initialTrainingSize - 1;

        while (trainEnd + testSize < observationCount)
        {
            var testStart = trainEnd + 1;
            var testEnd = testStart + testSize - 1;
            splits.Add(new WalkForwardSplit(0, trainEnd, testStart, testEnd, trainEnd, testEnd));
            trainEnd += stepSize;
        }

        return splits;
    }

    /// <summary>
    /// Creates walk-forward splits from dated NAV observations and typed validation options.
    /// </summary>
    /// <param name="navSeries">The full historical NAV series.</param>
    /// <param name="options">The walk-forward options.</param>
    /// <returns>Chronological walk-forward splits with explicit cutoff and target metadata.</returns>
    public IReadOnlyList<WalkForwardSplit> CreateSplits(
        NavSeries navSeries,
        WalkForwardEvaluationOptions options)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (navSeries.Count <= options.MinimumTrainingObservations)
        {
            return Array.Empty<WalkForwardSplit>();
        }

        var splits = new List<WalkForwardSplit>();
        var cutoffIndex = options.MinimumTrainingObservations - 1;
        var lastAcceptedTargetIndex = -1;
        while (cutoffIndex < navSeries.Count - 1)
        {
            var cutoffDate = navSeries[cutoffIndex].Date;
            if (options.EvaluationStartDate.HasValue && cutoffDate < options.EvaluationStartDate.Value)
            {
                cutoffIndex += options.StepSize;
                continue;
            }

            var targetIndex = ResolveTargetIndex(navSeries, cutoffIndex, options.ForecastHorizon);
            if (!targetIndex.HasValue)
            {
                break;
            }

            var targetDate = navSeries[targetIndex.Value].Date;
            if (options.EvaluationEndDate.HasValue && targetDate > options.EvaluationEndDate.Value)
            {
                break;
            }

            var trainStartIndex = ResolveTrainingStartIndex(cutoffIndex, options);
            if (trainStartIndex >= 0)
            {
                var targetWindowStart = cutoffIndex + 1;
                var overlapsPreviousAccepted = targetWindowStart <= lastAcceptedTargetIndex;
                if (!options.RequireNonOverlappingTargets || !overlapsPreviousAccepted)
                {
                    splits.Add(new WalkForwardSplit(
                        trainStartIndex,
                        cutoffIndex,
                        cutoffIndex + 1,
                        targetIndex.Value,
                        cutoffIndex,
                        targetIndex.Value,
                        cutoffDate,
                        targetDate));
                    lastAcceptedTargetIndex = targetIndex.Value;
                }
            }

            cutoffIndex += options.StepSize + options.EmbargoObservations;
        }

        return splits;
    }

    private static int ResolveTrainingStartIndex(int cutoffIndex, WalkForwardEvaluationOptions options)
    {
        if (options.WindowMode == TrainingWindowMode.Expanding)
        {
            return 0;
        }

        var windowLength = options.TrainingWindowLength.GetValueOrDefault();
        var start = cutoffIndex - windowLength + 1;
        return start < 0 ? -1 : start;
    }

    private static int? ResolveTargetIndex(
        NavSeries navSeries,
        int cutoffIndex,
        ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var targetIndex = cutoffIndex + horizon.Value;
            return targetIndex < navSeries.Count ? targetIndex : null;
        }

        var targetDate = navSeries[cutoffIndex].Date.AddDays(horizon.Value);
        for (var index = cutoffIndex + 1; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= targetDate)
            {
                return index;
            }
        }

        return null;
    }
}
