using System.Globalization;
using Aletheia.Core;
using Aletheia.Data;

namespace Aletheia.Data.Tests;

public sealed class CsvFundDataReaderTests
{
    [Fact]
    public async Task ReadAsync_WithHeader_LoadsNavSeries()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, "Date,NAV\n2024-01-01,100\n2024-01-02,101\n");

        try
        {
            var reader = new CsvFundDataReader();

            var history = await reader.ReadAsync(path);

            Assert.Equal(2, history.NavSeries.Count);
            Assert.Equal(101m, history.NavSeries[1].Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DatasetFingerprint_IsCultureInvariantAndDeterministic()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var calculator = new DatasetFingerprintCalculator();
        var series = new NavSeries(
        [
            new NavPoint(new DateOnly(2024, 1, 2), 101.25m),
            new NavPoint(new DateOnly(2024, 1, 1), 100.50m),
        ],
        ObservationFrequency.BusinessDaily);

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var french = calculator.CalculateSha256(series);
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var english = calculator.CalculateSha256(series);

            Assert.Equal(french, english);
            Assert.Equal(64, french.Length);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
