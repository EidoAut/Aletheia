using Aletheia.Core;

namespace Aletheia.Data;

/// <summary>
/// Provides deterministic synthetic fund data for demos and repeatable tests.
/// </summary>
/// <remarks>
/// The generated path includes drift, cyclic structure, and noise. It is not a
/// market model; it is only a stable input that lets mathematical development
/// proceed without an external data subscription.
/// </remarks>
public sealed class SampleFundDataProvider : IProvenanceAwareFundDataProvider
{
    private const int Seed = 314159;
    private static readonly FundIdentifier Identifier = new(FundIdentifierKind.Local, "sample-fund");

    /// <summary>
    /// Gets the identifier of the deterministic sample fund.
    /// </summary>
    /// <returns>The sample fund identifier.</returns>
    public static FundIdentifier GetSampleIdentifier() => Identifier;

    /// <inheritdoc />
    public Task<Fund?> FindByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Fund?>(null);
    }

    /// <inheritdoc />
    public Task<FundHistory> GetHistoryAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.GetHistoryWithProvenance(fundIdentifier, from, to, cancellationToken).History);
    }

    /// <inheritdoc />
    public Task<FundHistoryResult> GetHistoryWithProvenanceAsync(
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this.GetHistoryWithProvenance(fundIdentifier, from, to, cancellationToken));
    }

    private FundHistoryResult GetHistoryWithProvenance(
        FundIdentifier fundIdentifier,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (fundIdentifier != Identifier)
        {
            throw new KeyNotFoundException($"The sample provider only contains '{Identifier}'.");
        }

        var fund = new Fund(Identifier, "Aletheia Deterministic Sample Fund", "Sample", "EUR");
        var points = this.GeneratePoints();
        var series = new NavSeries(points, ObservationFrequency.BusinessDaily).Slice(from, to);
        var history = new FundHistory(fund, series);
        var fingerprint = new DatasetFingerprintCalculator().CalculateSha256(series);
        var provenance = new FundDataProvenance(
            "sample",
            "Deterministic Sample",
            DateTimeOffset.UtcNow,
            Identifier,
            null,
            null,
            "Deterministic seeded generator",
            series.ObservationFrequency,
            from,
            to,
            series.Count == 0 ? null : series.StartDate,
            series.Count == 0 ? null : series.EndDate,
            series.Count,
            series.Count,
            fingerprint,
            false,
            null);
        return new FundHistoryResult(history, provenance);
    }

    private IReadOnlyList<NavPoint> GeneratePoints()
    {
        var random = new Random(Seed);
        var points = new List<NavPoint>();
        var date = new DateOnly(2014, 1, 2);
        var nav = 100d;
        var businessObservations = 2_800;

        for (var observation = 0; observation < businessObservations; observation++)
        {
            while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }

            var cycle = 0.0018d * Math.Sin((2d * Math.PI * observation) / 180d);
            var slowerCycle = 0.0012d * Math.Sin((2d * Math.PI * observation) / 510d);
            var shock = 0.0065d * this.NextGaussian(random);
            var logReturn = 0.00022d + cycle + slowerCycle + shock;
            nav *= Math.Exp(logReturn);

            points.Add(new NavPoint(date, decimal.Round((decimal)nav, 6)));
            date = date.AddDays(1);
        }

        return points;
    }

    private double NextGaussian(Random random)
    {
        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();

        return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
    }
}
