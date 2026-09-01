using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class SampleFundDataProviderTests
{
    [Fact]
    public async Task GetHistoryAsync_ReturnsDeterministicHistory()
    {
        var provider = new SampleFundDataProvider();

        var first = await provider.GetHistoryAsync(SampleFundDataProvider.GetSampleIdentifier());
        var second = await provider.GetHistoryAsync(SampleFundDataProvider.GetSampleIdentifier());

        Assert.Equal(first.NavSeries.Count, second.NavSeries.Count);
        Assert.Equal(first.NavSeries[0].Value, second.NavSeries[0].Value);
        Assert.Equal(first.NavSeries[first.NavSeries.Count - 1].Value, second.NavSeries[second.NavSeries.Count - 1].Value);
    }
}
