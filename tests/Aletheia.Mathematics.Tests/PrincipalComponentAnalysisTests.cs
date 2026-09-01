using Aletheia.Mathematics;

namespace Aletheia.Mathematics.Tests;

public sealed class PrincipalComponentAnalysisTests
{
    [Fact]
    public void Fit_WithCorrelatedFeatures_ExplainsMostVarianceInFirstComponent()
    {
        var pca = new PrincipalComponentAnalysis();

        var result = pca.Fit(
        [
            [1d, 2d],
            [2d, 4d],
            [3d, 6d],
            [4d, 8d],
        ]);

        Assert.True(result.ExplainedVarianceRatio > 0.99d);
        Assert.Equal(4, result.Scores.Length);
    }
}
