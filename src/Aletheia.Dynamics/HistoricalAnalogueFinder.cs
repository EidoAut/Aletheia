using Aletheia.Core;
using Aletheia.Mathematics;

namespace Aletheia.Dynamics;

/// <summary>
/// Finds historical states that are mathematically close to the current state.
/// </summary>
public sealed class HistoricalAnalogueFinder
{
    /// <summary>
    /// Finds the nearest historical states by standardized Euclidean distance.
    /// </summary>
    /// <param name="history">The historical state observations.</param>
    /// <param name="currentState">The current state.</param>
    /// <param name="maximumMatches">The maximum number of matches to return.</param>
    /// <returns>The nearest historical analogue states.</returns>
    public IReadOnlyList<HistoricalAnalogueResult> FindNearest(
        IReadOnlyList<StateObservation> history,
        DynamicState currentState,
        int maximumMatches)
    {
        return this.FindNearestWithDiagnostics(history, currentState, maximumMatches).Matches;
    }

    /// <summary>
    /// Finds the nearest historical states and exposes compatibility diagnostics.
    /// </summary>
    /// <param name="history">The historical state observations.</param>
    /// <param name="currentState">The current state.</param>
    /// <param name="maximumMatches">The maximum number of matches to return.</param>
    /// <returns>The analogue search result.</returns>
    public HistoricalAnalogueSearchResult FindNearestWithDiagnostics(
        IReadOnlyList<StateObservation> history,
        DynamicState currentState,
        int maximumMatches)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(currentState);

        if (maximumMatches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMatches), maximumMatches, "Maximum matches must be positive.");
        }

        if (currentState.Schema is null)
        {
            throw new IncompatibleDynamicStateException("Analogue search requires a state schema fingerprint on the query state.");
        }

        var candidateHistory = history
            .Where(observation => observation.Date < currentState.Date)
            .ToArray();
        var candidateCount = candidateHistory.Length;

        var schemaCompatible = candidateHistory
            .Where(observation => observation.Schema is not null && observation.Schema.IsCompatibleWith(currentState.Schema))
            .ToArray();
        var rejectedSchemaIncompatibleCount = candidateCount - schemaCompatible.Length;
        var dimensions = currentState.Schema.DimensionOrder;
        var dimensionCompatible = schemaCompatible
            .Where(observation => HasAllDimensions(observation, dimensions))
            .ToArray();
        var rejectedMissingDimensionCount = schemaCompatible.Length - dimensionCompatible.Length;

        if (dimensionCompatible.Length == 0 || dimensions.Count == 0)
        {
            return new HistoricalAnalogueSearchResult(
                Array.Empty<HistoricalAnalogueResult>(),
                candidateCount,
                schemaCompatible.Length,
                rejectedSchemaIncompatibleCount,
                rejectedMissingDimensionCount,
                dimensions);
        }

        var scales = EstimateScales(dimensionCompatible, dimensions);
        var results = new List<HistoricalAnalogueResult>(dimensionCompatible.Length);

        foreach (var observation in dimensionCompatible)
        {
            var distance = CalculateDistance(observation, currentState, dimensions, scales);
            results.Add(new HistoricalAnalogueResult(observation, distance));
        }

        var matches = results
            .OrderBy(result => result.Distance)
            .Take(maximumMatches)
            .ToArray();

        return new HistoricalAnalogueSearchResult(
            matches,
            candidateCount,
            schemaCompatible.Length,
            rejectedSchemaIncompatibleCount,
            rejectedMissingDimensionCount,
            dimensions);
    }

    private static IReadOnlyDictionary<StateDimension, double> EstimateScales(
        IReadOnlyList<StateObservation> history,
        IReadOnlyList<StateDimension> dimensions)
    {
        var scales = new Dictionary<StateDimension, double>();
        foreach (var dimension in dimensions)
        {
            var values = history.Select(observation => observation.Dimensions[dimension]).ToArray();
            var standardDeviation = values.Length < 2
                ? 1d
                : DescriptiveStatistics.SampleStandardDeviation(values);
            scales[dimension] = standardDeviation == 0d ? 1d : standardDeviation;
        }

        return scales;
    }

    private static double CalculateDistance(
        StateObservation observation,
        DynamicState currentState,
        IReadOnlyList<StateDimension> dimensions,
        IReadOnlyDictionary<StateDimension, double> scales)
    {
        var sum = 0d;
        foreach (var dimension in dimensions)
        {
            var difference = (observation.Dimensions[dimension] - currentState.Dimensions[dimension]) / scales[dimension];
            sum += difference * difference;
        }

        return Math.Sqrt(sum);
    }

    private static bool HasAllDimensions(StateObservation observation, IReadOnlyList<StateDimension> dimensions)
    {
        return dimensions.All(dimension => observation.Dimensions.ContainsKey(dimension));
    }
}
