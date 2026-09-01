using Aletheia.Core;

#pragma warning disable SA1402 // Scientific protocol DTOs are intentionally colocated.
#pragma warning disable SA1649 // File name follows the primary protocol type.

namespace Aletheia.Validation;

/// <summary>
/// Configures nested walk-forward selection.
/// </summary>
public sealed record NestedWalkForwardOptions
{
    /// <summary>
    /// Gets the candidate horizons selected inside each outer training prefix.
    /// </summary>
    public IReadOnlyList<ForecastHorizon> CandidateHorizons { get; init; } =
    [
        ForecastHorizon.Observations(5),
        ForecastHorizon.Observations(10),
        ForecastHorizon.Observations(20),
        ForecastHorizon.Observations(60),
        ForecastHorizon.Observations(120),
    ];

    /// <summary>
    /// Gets the minimum observations before the first outer prediction.
    /// </summary>
    public int MinimumOuterTrainingObservations { get; init; } = 180;

    /// <summary>
    /// Gets the minimum observations for inner walk-forward loops.
    /// </summary>
    public int MinimumInnerTrainingObservations { get; init; } = 80;

    /// <summary>
    /// Gets the outer cutoff step size.
    /// </summary>
    public int OuterStepSize { get; init; } = 20;

    /// <summary>
    /// Gets the inner cutoff step size.
    /// </summary>
    public int InnerStepSize { get; init; } = 5;

    /// <summary>
    /// Gets observations embargoed between adjacent validation targets.
    /// </summary>
    public int EmbargoObservations { get; init; } = 0;

    /// <summary>
    /// Validates options.
    /// </summary>
    public void Validate()
    {
        if (this.CandidateHorizons.Count == 0)
        {
            throw new ArgumentException("At least one candidate horizon is required.");
        }

        if (this.MinimumOuterTrainingObservations <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MinimumOuterTrainingObservations), this.MinimumOuterTrainingObservations, "Outer training observations must exceed one.");
        }

        if (this.MinimumInnerTrainingObservations <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(this.MinimumInnerTrainingObservations), this.MinimumInnerTrainingObservations, "Inner training observations must exceed one.");
        }

        if (this.OuterStepSize <= 0 || this.InnerStepSize <= 0)
        {
            throw new ArgumentException("Nested walk-forward step sizes must be positive.");
        }

        if (this.EmbargoObservations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.EmbargoObservations), this.EmbargoObservations, "Embargo cannot be negative.");
        }
    }
}

/// <summary>
/// Context supplied to an inner selection scorer.
/// </summary>
/// <param name="TrainingPrefix">The NAV prefix available to the outer cutoff.</param>
/// <param name="CandidateHorizon">The candidate horizon being scored.</param>
/// <param name="OuterPredictionCutoffIndex">The outer prediction cutoff index in the original series.</param>
/// <param name="InnerOptions">The inner walk-forward options.</param>
public sealed record NestedWalkForwardSelectionContext(
    NavSeries TrainingPrefix,
    ForecastHorizon CandidateHorizon,
    int OuterPredictionCutoffIndex,
    WalkForwardEvaluationOptions InnerOptions);

/// <summary>
/// Stores the horizon selected for one outer cutoff.
/// </summary>
/// <param name="OuterPredictionCutoffIndex">The outer prediction cutoff.</param>
/// <param name="OuterTargetIndex">The target index for the selected outer horizon.</param>
/// <param name="SelectedHorizon">The selected horizon.</param>
/// <param name="SelectedScore">The inner-loop selection score.</param>
/// <param name="InnerSelectionEndIndex">The final index visible to inner selection.</param>
/// <param name="CandidateScores">All candidate scores.</param>
public sealed record NestedWalkForwardSelection(
    int OuterPredictionCutoffIndex,
    int OuterTargetIndex,
    ForecastHorizon SelectedHorizon,
    double SelectedScore,
    int InnerSelectionEndIndex,
    IReadOnlyDictionary<ForecastHorizon, double> CandidateScores);

/// <summary>
/// Builds nested walk-forward selections without exposing outer-test outcomes to the inner loop.
/// </summary>
public sealed class NestedWalkForwardValidator
{
    /// <summary>
    /// Selects one horizon per outer cutoff using only each outer training prefix.
    /// </summary>
    /// <param name="navSeries">The complete historical series used only to locate outer targets.</param>
    /// <param name="options">Nested validation options.</param>
    /// <param name="scoreSelector">A deterministic scorer evaluated on the outer training prefix.</param>
    /// <returns>Outer cutoff selections.</returns>
    public IReadOnlyList<NestedWalkForwardSelection> Select(
        NavSeries navSeries,
        NestedWalkForwardOptions options,
        Func<NestedWalkForwardSelectionContext, double> scoreSelector)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scoreSelector);
        options.Validate();

        var selections = new List<NestedWalkForwardSelection>();
        var outerCutoff = options.MinimumOuterTrainingObservations - 1;
        while (outerCutoff < navSeries.Count - 1)
        {
            var trainingPrefix = new NavSeries(
                navSeries.Points.Take(outerCutoff + 1),
                navSeries.ObservationFrequency);
            var scores = new Dictionary<ForecastHorizon, double>();
            foreach (var horizon in options.CandidateHorizons)
            {
                var innerOptions = new WalkForwardEvaluationOptions
                {
                    MinimumTrainingObservations = Math.Min(options.MinimumInnerTrainingObservations, Math.Max(2, trainingPrefix.Count - 1)),
                    ForecastHorizon = horizon,
                    StepSize = options.InnerStepSize,
                    EmbargoObservations = options.EmbargoObservations,
                };
                var score = scoreSelector(new NestedWalkForwardSelectionContext(
                    trainingPrefix,
                    horizon,
                    outerCutoff,
                    innerOptions));
                scores[horizon] = double.IsFinite(score) ? score : double.NegativeInfinity;
            }

            var selected = scores
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key.Unit)
                .ThenBy(pair => pair.Key.Value)
                .First();
            var targetIndex = ResolveTargetIndex(navSeries, outerCutoff, selected.Key);
            if (targetIndex.HasValue)
            {
                selections.Add(new NestedWalkForwardSelection(
                    outerCutoff,
                    targetIndex.Value,
                    selected.Key,
                    selected.Value,
                    outerCutoff,
                    scores));
            }

            outerCutoff += options.OuterStepSize + options.EmbargoObservations;
        }

        return selections;
    }

    private static int? ResolveTargetIndex(NavSeries navSeries, int cutoffIndex, ForecastHorizon horizon)
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
