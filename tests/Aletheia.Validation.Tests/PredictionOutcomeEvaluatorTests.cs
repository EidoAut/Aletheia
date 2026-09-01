using Aletheia.Core;
using Aletheia.Validation;

namespace Aletheia.Validation.Tests;

public sealed class PredictionOutcomeEvaluatorTests
{
    [Fact]
    public void Evaluate_WhenRealizedReturnInsideInterquartileRange_FlagsCoverage()
    {
        var prediction = new PredictionRecord(
            Guid.NewGuid(),
            new FundIdentifier(FundIdentifierKind.Local, "sample"),
            DateTimeOffset.UtcNow,
            new DateOnly(2024, 1, 1),
            new ForecastHorizonResolution(
                ForecastHorizon.Observations(30),
                ObservationFrequency.BusinessDaily,
                30,
                new DateOnly(2024, 2, 12),
                "UnitTestPolicy",
                false),
            0.03d,
            0.04d,
            0.03d,
            0.60d,
            new Dictionary<int, double> { [25] = 0.01d, [75] = 0.08d },
            new ModelDescriptor("unit.model", "Unit Model", "1.0"),
            new Dictionary<string, string>(),
            "test",
            "v1.2",
            new string('1', 64),
            new DatasetIdentity("UnitTest", new string('0', 64), null),
            null,
            InvestmentSignal.NoReliableSignal,
            null,
            "unit-feature-config");
        var evaluator = new PredictionOutcomeEvaluator();

        var result = evaluator.Evaluate(prediction, 0.05d);

        Assert.True(result.WasInsideInterquartileRange);
        Assert.Equal(0.02d, result.AbsoluteError, 9);
        Assert.Equal(ObservationFrequency.BusinessDaily, prediction.ObservationFrequency);
        Assert.Equal(0.03d, prediction.PointForecastReturn);
    }
}
