using Aletheia.Core;
using Aletheia.Dynamics;

namespace Aletheia.Dynamics.Tests;

public sealed class HistoricalAnalogueFinderTests
{
    [Fact]
    public void FindNearest_ExcludesFutureObservations()
    {
        var dimension = StandardStateDimensions.Momentum;
        var schema = CreateSchema(dimension);
        var current = new DynamicState(
            new DateOnly(2024, 1, 3),
            new Dictionary<StateDimension, double> { [dimension] = 1d },
            1d,
            schema);
        var history = new[]
        {
            new StateObservation(new DateOnly(2024, 1, 1), new Dictionary<StateDimension, double> { [dimension] = 1.1d }, schema),
            new StateObservation(new DateOnly(2024, 1, 4), new Dictionary<StateDimension, double> { [dimension] = 1.0d }, schema),
        };

        var finder = new HistoricalAnalogueFinder();

        var results = finder.FindNearest(history, current, 10);

        Assert.Single(results);
        Assert.Equal(new DateOnly(2024, 1, 1), results[0].Observation.Date);
    }

    [Fact]
    public void FindNearestWithDiagnostics_RejectsMismatchedSchemaFingerprint()
    {
        var dimension = StandardStateDimensions.Momentum;
        var querySchema = CreateSchema(dimension, ("MomentumLookback", "30"));
        var otherSchema = CreateSchema(dimension, ("MomentumLookback", "90"));
        var current = new DynamicState(
            new DateOnly(2024, 1, 3),
            new Dictionary<StateDimension, double> { [dimension] = 1d },
            1d,
            querySchema);
        var history = new[]
        {
            new StateObservation(new DateOnly(2024, 1, 1), new Dictionary<StateDimension, double> { [dimension] = 1.1d }, otherSchema),
            new StateObservation(new DateOnly(2024, 1, 2), new Dictionary<StateDimension, double> { [dimension] = 1.2d }, querySchema),
        };

        var result = new HistoricalAnalogueFinder().FindNearestWithDiagnostics(history, current, 10);

        Assert.Single(result.Matches);
        Assert.Equal(2, result.CandidateCount);
        Assert.Equal(1, result.SchemaCompatibleCount);
        Assert.Equal(1, result.RejectedSchemaIncompatibleCount);
        Assert.Equal([dimension], result.DimensionsUsed);
    }

    private static StateSchemaDescriptor CreateSchema(
        StateDimension dimension,
        params (string Key, string Value)[] configuration)
    {
        return new StateSchemaDescriptor(
            "UnitStateSchema",
            "v1",
            [dimension],
            configuration.ToDictionary(pair => pair.Key, pair => pair.Value),
            "Unit-test schema.");
    }
}
