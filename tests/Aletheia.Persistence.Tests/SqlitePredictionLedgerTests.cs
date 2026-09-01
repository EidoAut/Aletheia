using Aletheia.Core;
using Aletheia.Persistence;
using Aletheia.Validation;

namespace Aletheia.Persistence.Tests;

public sealed class SqlitePredictionLedgerTests
{
    [Fact]
    public async Task StorePredictionAsync_RoundTripsPredictionAndInitializesSchema()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var prediction = CreatePrediction(expectedReturn: 0.04d);

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(prediction);
            var loaded = await ledger.GetPredictionAsync(prediction.Prediction.PredictionId);

            Assert.NotNull(loaded);
            Assert.Equal(prediction.Prediction.PredictionId, loaded.Prediction.PredictionId);
            Assert.Equal(0.04d, loaded.Prediction.ExpectedReturn, 12);
            Assert.Equal(PredictionOrigin.HistoricalWalkForward, loaded.Origin);
            Assert.Equal(prediction.ModelConfigurationFingerprint, loaded.ModelConfigurationFingerprint);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task StorePredictionAsync_WhenLogicalKeyAlreadyExistsWithSameContent_IsIdempotent()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var prediction = CreatePrediction(expectedReturn: 0.04d);

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(prediction);
            await ledger.StorePredictionAsync(prediction);
            var loaded = await ledger.GetPredictionByLogicalKeyAsync(prediction.LogicalKey);

            Assert.NotNull(loaded);
            Assert.Equal(prediction.ContentFingerprint, loaded.ContentFingerprint);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task StorePredictionAsync_WhenLogicalKeyAlreadyExistsWithDifferentContent_ThrowsIntegrityError()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var prediction = CreatePrediction(expectedReturn: 0.04d);
            var attemptedOverwrite = CreatePrediction(expectedReturn: 0.99d, prediction.LogicalKey, prediction.Prediction.PredictionId);

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(prediction);
            await Assert.ThrowsAsync<PredictionLedgerIntegrityException>(() => ledger.StorePredictionAsync(attemptedOverwrite));
            var loaded = await ledger.GetPredictionByLogicalKeyAsync(prediction.LogicalKey);

            Assert.NotNull(loaded);
            Assert.Equal(0.04d, loaded.Prediction.ExpectedReturn, 12);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task StoreEvaluationAsync_LinksEvaluationWithoutChangingPrediction()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var prediction = CreatePrediction(expectedReturn: 0.04d);
            var evaluation = PredictionEvaluationRecord.Create(
                prediction,
                0.03d,
                new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
                0d);

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(prediction);
            await ledger.StoreEvaluationAsync(evaluation);
            await ledger.StoreEvaluationAsync(evaluation);
            var loadedPrediction = await ledger.GetPredictionAsync(prediction.Prediction.PredictionId);
            var evaluations = await ledger.GetEvaluationsAsync(prediction.Prediction.PredictionId);

            Assert.NotNull(loadedPrediction);
            Assert.Equal(0.04d, loadedPrediction.Prediction.ExpectedReturn, 12);
            Assert.Single(evaluations);
            Assert.Equal(0.03d, evaluations[0].ActualReturn, 12);
            Assert.Equal(0.01d, evaluations[0].AbsoluteError, 12);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task StoreEvaluationAsync_WhenSamePredictionHasDifferentEvaluationContent_ThrowsIntegrityError()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var prediction = CreatePrediction(expectedReturn: 0.04d);
            var evaluation = PredictionEvaluationRecord.Create(
                prediction,
                0.03d,
                new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
                0d);
            var conflictingEvaluation = PredictionEvaluationRecord.Create(
                prediction,
                -0.02d,
                new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
                0d);

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(prediction);
            await ledger.StoreEvaluationAsync(evaluation);

            await Assert.ThrowsAsync<PredictionLedgerIntegrityException>(() => ledger.StoreEvaluationAsync(conflictingEvaluation));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task StorePredictionAsync_WhenConfigurationChanges_UsesDistinctLogicalPrediction()
    {
        var path = CreateTemporaryDatabasePath();
        try
        {
            var ledger = new SqlitePredictionLedger(path);
            var first = CreatePrediction(expectedReturn: 0.04d, logicalKey: "unit|config-a", modelConfigurationFingerprint: "configuration-a");
            var second = CreatePrediction(expectedReturn: 0.04d, logicalKey: "unit|config-b", modelConfigurationFingerprint: "configuration-b");

            await ledger.InitializeAsync();
            await ledger.StorePredictionAsync(first);
            await ledger.StorePredictionAsync(second);
            var predictions = await ledger.ListPredictionsAsync(10);

            Assert.Equal(2, predictions.Count);
            Assert.Contains(predictions, prediction => prediction.LogicalKey == "unit|config-a");
            Assert.Contains(predictions, prediction => prediction.LogicalKey == "unit|config-b");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static PredictionLedgerRecord CreatePrediction(
        double expectedReturn,
        string? logicalKey = null,
        Guid? predictionId = null,
        string modelConfigurationFingerprint = "configuration")
    {
        logicalKey ??= $"unit|{expectedReturn}";
        var horizon = new ForecastHorizonResolution(
            ForecastHorizon.Observations(5),
            ObservationFrequency.Daily,
            5,
            new DateOnly(2024, 1, 6),
            "Unit",
            false);
        var id = predictionId ?? DeterministicPredictionIdentity.CreateGuid(logicalKey);
        var corePrediction = new PredictionRecord(
            id,
            new FundIdentifier(FundIdentifierKind.Local, "unit"),
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateOnly(2024, 1, 1),
            horizon,
            expectedReturn,
            expectedReturn,
            expectedReturn,
            0.60d,
            new Dictionary<int, double>
            {
                [10] = -0.01d,
                [25] = 0d,
                [50] = expectedReturn,
                [75] = 0.05d,
                [90] = 0.08d,
            },
            new ModelDescriptor("unit.model", "Unit Model", "1.0"),
            new Dictionary<string, string> { ["Parameter"] = "Value" },
            "test",
            "v1.2",
            new string('1', 64),
            new DatasetIdentity("Unit", new string('0', 64), null),
            null,
            InvestmentSignal.NoReliableSignal,
            null,
            "feature-config");

        return new PredictionLedgerRecord(
            corePrediction,
            logicalKey,
            modelConfigurationFingerprint,
            PredictionOrigin.HistoricalWalkForward,
            null,
            0,
            100,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 1, 1),
            100,
            105,
            new DateOnly(2024, 1, 6),
            new Dictionary<string, string> { ["Diagnostic"] = "Value" });
    }

    private static string CreateTemporaryDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"aletheia-{Guid.NewGuid():N}.db");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
