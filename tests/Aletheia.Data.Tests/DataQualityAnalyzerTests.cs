using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class DataQualityAnalyzerTests
{
    [Fact]
    public void Evaluate_WithDuplicateAndNonPositiveValues_ReportsDiagnostics()
    {
        var analyzer = new DataQualityAnalyzer(new DataQualityOptions { MinimumObservationCount = 2 });

        var report = analyzer.Evaluate(
        [
            new NavPoint(new DateOnly(2024, 1, 1), 100m),
            new NavPoint(new DateOnly(2024, 1, 1), 101m),
            new NavPoint(new DateOnly(2024, 1, 2), 0m),
        ]);

        Assert.Equal(1, report.DuplicateObservationCount);
        Assert.Equal(1, report.NonPositiveValueCount);
        Assert.True(report.QualityScore < 100);
    }
}
