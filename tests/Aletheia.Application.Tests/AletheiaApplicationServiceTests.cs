using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Data;
using Aletheia.Simulation;
using Aletheia.Validation;

namespace Aletheia.Application.Tests;

public sealed class AletheiaApplicationServiceTests
{
    [Fact]
    public async Task LoadSampleWorkspaceAsync_ReturnsRealAnalysisAndForecasts()
    {
        var service = new AletheiaApplicationService(CreateOptions());

        var workspace = await service.LoadSampleWorkspaceAsync();

        Assert.Equal("Aletheia Deterministic Sample Fund", workspace.Analysis.Dataset.FundName);
        Assert.True(workspace.Analysis.Nav.Count > 1_000);
        Assert.True(workspace.Analysis.Drawdown.Count > 1_000);
        Assert.True(workspace.Analysis.RollingVolatility.Count > 0);
        Assert.NotNull(workspace.Analysis.Spectrum.DominantFrequency);
        Assert.NotEmpty(workspace.Analysis.StateProjection);
        Assert.Contains(workspace.Analysis.Forecasts.Runs, run => run.Distribution is not null);
        Assert.NotNull(workspace.Analysis.ResearchReport);
        Assert.InRange(workspace.Analysis.ResearchReport!.FundScore.Score, 1d, 10d);
        Assert.Equal(
            DecisionSignalLabels.ToDisplayLabel(
                workspace.Analysis.ResearchReport.DecisionSignal.Direction,
                workspace.Analysis.ResearchReport.DecisionSignal.Qualification),
            workspace.Analysis.ResearchReport.DecisionSignal.DisplayLabel);
        Assert.NotEqual(SignalQualification.Unavailable, workspace.Analysis.ResearchReport.DecisionSignal.Qualification);
        Assert.NotNull(workspace.Analysis.MarketTiming);
        Assert.NotEmpty(workspace.Analysis.MarketTiming!.Horizons);
        Assert.InRange(workspace.Analysis.MarketTiming.Decision.Strength, 0d, 1d);
        Assert.NotNull(workspace.Analysis.MarketTiming.EconomicBacktest);
        Assert.Equal(
            workspace.Analysis.MarketTiming.EconomicBacktest!.IsReliable,
            workspace.Analysis.MarketTiming.EconomicBacktest.Results.Count > 0);
        Assert.NotNull(workspace.Analysis.Dataset.Provenance);
        Assert.Equal("sample", workspace.Analysis.Dataset.Provenance!.ProviderId);
        Assert.Null(workspace.Arena);
    }

    [Fact]
    public async Task LoadCsvWorkspaceAsync_UsesExistingCsvInfrastructure()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var csvPath = Path.Combine(FindRepositoryRoot(), "examples", "sample-fund.csv");

        var workspace = await service.LoadCsvWorkspaceAsync(csvPath);

        Assert.Equal("sample-fund", workspace.Analysis.Dataset.FundName);
        Assert.Equal(10, workspace.Analysis.Dataset.ObservationCount);
        Assert.NotEqual(new string('0', 64), workspace.Analysis.Dataset.DatasetFingerprint);
        Assert.NotNull(workspace.Analysis.Dataset.Provenance);
        Assert.Equal("local-csv", workspace.Analysis.Dataset.Provenance!.ProviderId);
        Assert.Equal(workspace.Analysis.CumulativeReturn[^1].Value, workspace.Analysis.Performance.CumulativeReturn, 12);
    }

    [Fact]
    public async Task FundDiscovery_SearchesConfiguredProviderAndLoadsHistoryWithProvenance()
    {
        var provider = new StubFundProvider();
        var service = new AletheiaApplicationService(CreateOptions(provider));

        var results = await service.SearchFundsAsync("alfa", 10);
        var workspace = await service.LoadProviderWorkspaceAsync(
            provider.ProviderId,
            new FundIdentifier(FundIdentifierKind.Isin, StubFundProvider.IsinValue),
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 3, 31));

        Assert.Single(results);
        Assert.Equal(StubFundProvider.IsinValue, results[0].Isin);
        Assert.Equal("Stub Asset Management", results[0].ManagementCompany);
        Assert.Equal("Stub Official Provider", workspace.Analysis.Dataset.Provenance!.ProviderDisplayName);
        Assert.Equal(ObservationFrequency.BusinessDaily, workspace.History.NavSeries.ObservationFrequency);
        Assert.DoesNotContain(workspace.History.NavSeries.Points, point => point.Date == new DateOnly(2024, 1, 6));
        Assert.DoesNotContain(workspace.History.NavSeries.Points, point => point.Date == new DateOnly(2024, 1, 15));
    }

    [Fact]
    public async Task RunInvestmentSimulation_UsesLoadedDatasetAndPeriodicContributions()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var workspace = await service.LoadSampleWorkspaceAsync();
        var request = new InvestmentSimulationRequest(1_800d, 100d, 5, 500, 42);

        var result = service.RunInvestmentSimulation(workspace, request);

        Assert.Equal(workspace.Analysis.Dataset.DatasetFingerprint, result.Dataset.DatasetFingerprint);
        Assert.Equal(61, result.Trajectory.Count);
        Assert.Equal(7_800d, result.TotalContributed, 10);
        Assert.Equal(workspace.History.NavSeries.EndDate.AddYears(5), result.TargetDate);
        Assert.InRange(result.ProbabilityTerminalBelowContributions, 0d, 1d);
        Assert.True(double.IsFinite(result.MonthlyMeanLogReturn));
        Assert.True(double.IsFinite(result.MonthlyStandardDeviation));
        Assert.Contains("Gaussian", result.Methodology, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunInvestmentSimulation_WithInvalidHorizon_Throws()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var workspace = await service.LoadSampleWorkspaceAsync();
        var request = new InvestmentSimulationRequest(1_800d, 100d, 0, 500, 42);

        var exception = Assert.Throws<InvalidOperationException>(() => service.RunInvestmentSimulation(workspace, request));

        Assert.Contains("between 1 and 50 years", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunModelArenaAsync_ExposesSeparatedCommonSupportAndPredictionLedger()
    {
        var options = CreateOptions();
        var service = new AletheiaApplicationService(options);
        var workspace = await service.LoadSampleWorkspaceAsync();

        var arena = await service.RunModelArenaAsync(workspace);
        var predictions = await service.GetPredictionListAsync(10);
        var details = await service.GetPredictionDetailsAsync(predictions[0].PredictionId);

        Assert.True(arena.PointCommonSupportEventCount > 0);
        Assert.True(arena.ProbabilityCommonSupportEventCount > 0);
        Assert.True(arena.PointCommonSupportEventCount >= arena.ProbabilityCommonSupportEventCount);
        Assert.Contains(arena.Models, model => model.Capabilities == ForecastCapabilities.ProbabilityPositive);
        Assert.NotEmpty(predictions);
        Assert.NotNull(details);
        Assert.NotEmpty(details!.Evaluations);
    }

    [Fact]
    public async Task BuildResearchReport_WithSingleNinetyDayArena_DoesNotTreatItAsTwelveMonthEvidence()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var workspace = await service.LoadSampleWorkspaceAsync();
        var arena = await service.RunModelArenaAsync(workspace);

        var report = service.BuildResearchReport(workspace.WithArena(arena));

        Assert.InRange(report.FundScore.Score, 1d, 10d);
        Assert.NotEmpty(report.StressScenarios);
        Assert.NotNull(report.RegimeModel);
        Assert.NotNull(report.Ensemble?.Distribution);
        Assert.Equal(ForecastHorizon.CalendarDays(90), report.Ensemble.Distribution.RequestedHorizon);
        Assert.NotSame(report.Ensemble.Distribution, report.TwelveMonthForecast);
        Assert.NotEmpty(report.Provenance);
    }

    [Fact]
    public void AnalyzeFund_WithIrregularObservations_UsesElapsedTimeConventionAcrossWorkspace()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var history = CreateIrregularHistory();

        var workspace = service.AnalyzeFund(history);
        var simulation = service.RunInvestmentSimulation(
            workspace,
            new InvestmentSimulationRequest(1_800d, 100d, 1, 100, 42));

        Assert.Equal(ObservationFrequency.Irregular, workspace.History.NavSeries.ObservationFrequency);
        Assert.True(double.IsFinite(workspace.Analysis.Performance.AnnualizedVolatility));
        Assert.NotEmpty(workspace.Analysis.RollingVolatility);
        Assert.NotEmpty(workspace.Analysis.Forecasts.Runs);
        Assert.True(simulation.ObservationPeriodsPerMonth > 0d);
        Assert.Contains("irregular cadence", simulation.Methodology, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnalyzeFund_WithCalendarCarryForwardRows_UsesEffectiveObservations()
    {
        var history = CreateCalendarCarryForwardHistory(new DateOnly(2024, 1, 1), 45);
        var generatedAt = ToUtcTimestamp(history.NavSeries.EndDate.AddDays(5));
        var service = new AletheiaApplicationService(CreateOptions(generatedAt));

        var workspace = service.AnalyzeFund(history);

        Assert.Equal(45, workspace.Analysis.Dataset.SourceObservationCount);
        Assert.True(workspace.Analysis.Dataset.SyntheticObservationCount > 0);
        Assert.Equal(workspace.History.NavSeries.Count, workspace.Analysis.Dataset.EffectiveObservationCount);
        Assert.Equal(ObservationFrequency.BusinessDaily, workspace.History.NavSeries.ObservationFrequency);
        Assert.DoesNotContain(workspace.History.NavSeries.Points, point =>
            point.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        Assert.Contains(
            workspace.Analysis.ResearchReport!.Warnings,
            warning => warning.Contains("carry-forward", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeFund_WithStaleEffectiveDataset_BlocksCurrentActionability()
    {
        var history = CreateBusinessDailyHistory(new DateOnly(2025, 9, 1), 180);
        var generatedAt = ToUtcTimestamp(history.NavSeries.EndDate.AddDays(90));
        var service = new AletheiaApplicationService(CreateOptions(generatedAt));

        var workspace = service.AnalyzeFund(history);
        var report = workspace.Analysis.ResearchReport!;

        Assert.Equal(DataFreshnessStatus.Stale, report.DataFreshness.Status);
        Assert.Equal("CurrentDecisionUnavailable", report.Actionability.Status);
        Assert.Equal(SignalActionabilityLevel.Unavailable, report.Actionability.Level);
        Assert.Equal(ConfidenceLevel.Low, report.Actionability.Confidence);
        Assert.Contains(
            report.DecisionSignal.CounterEvidence,
            item => item.Contains("unqualified current decisions are blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeFund_WithFreshEffectiveDataset_DoesNotBlockCurrentActionabilityAsStale()
    {
        var history = CreateBusinessDailyHistory(new DateOnly(2025, 9, 1), 180);
        var generatedAt = ToUtcTimestamp(history.NavSeries.EndDate.AddDays(10));
        var service = new AletheiaApplicationService(CreateOptions(generatedAt));

        var workspace = service.AnalyzeFund(history);
        var report = workspace.Analysis.ResearchReport!;

        Assert.Equal(DataFreshnessStatus.Fresh, report.DataFreshness.Status);
        Assert.NotEqual("CurrentDecisionUnavailable", report.Actionability.Status);
    }

    [Theory]
    [InlineData(DirectionalSignal.Buy, SignalQualification.Confirmed, "BUY")]
    [InlineData(DirectionalSignal.Buy, SignalQualification.Tentative, "BUY?")]
    [InlineData(DirectionalSignal.Hold, SignalQualification.Confirmed, "HOLD")]
    [InlineData(DirectionalSignal.Hold, SignalQualification.Tentative, "HOLD?")]
    [InlineData(DirectionalSignal.Sell, SignalQualification.Confirmed, "SELL")]
    [InlineData(DirectionalSignal.Sell, SignalQualification.Tentative, "SELL?")]
    [InlineData(DirectionalSignal.None, SignalQualification.Unavailable, "NO CALL")]
    [InlineData(DirectionalSignal.Buy, SignalQualification.Unavailable, "NO CALL")]
    public void DecisionSignalLabels_MapDirectionAndQualificationToInvestorLabel(
        DirectionalSignal direction,
        SignalQualification qualification,
        string expected)
    {
        Assert.Equal(expected, DecisionSignalLabels.ToDisplayLabel(direction, qualification));
    }

    [Theory]
    [InlineData("QualifiedActionable", SignalActionabilityLevel.Actionable)]
    [InlineData("QualifiedTentativeSignal", SignalActionabilityLevel.Caution)]
    [InlineData("StrategicOnlyTimingUnavailable", SignalActionabilityLevel.Caution)]
    [InlineData("NoDefensibleCurrentSignal", SignalActionabilityLevel.Unavailable)]
    [InlineData("CurrentDecisionUnavailable", SignalActionabilityLevel.Unavailable)]
    public void ActionabilityAssessment_ExposesStructuredLevel(string status, SignalActionabilityLevel expected)
    {
        var assessment = new ActionabilityAssessment(
            status,
            ConfidenceLevel.Low,
            new DateOnly(2026, 1, 15),
            Array.Empty<string>());

        Assert.Equal(expected, assessment.Level);
    }

    [Fact]
    public void RunTimingEconomicBacktest_WithInsufficientOosSignalsReturnsExplicitNoReliableStatus()
    {
        var service = new AletheiaApplicationService(CreateOptions());
        var workspace = service.AnalyzeFund(CreateIrregularHistory());
        var options = new TimingBacktestOptions(
            TransactionCostRate: 0.002d,
            SlippageRate: 0.001d,
            ExecutionDelayObservations: 2);

        var result = service.RunTimingEconomicBacktest(workspace, options);

        Assert.Equal("NO RELIABLE ECONOMIC BACKTEST", result.Status);
        Assert.False(result.IsReliable);
        Assert.Empty(result.Results);
        Assert.Equal(2, result.ExecutionDelayObservations);
        Assert.Equal(0.002d, result.TransactionCostRate, 12);
        Assert.Equal(0.001d, result.SlippageRate, 12);
        Assert.NotNull(workspace.Analysis.MarketTiming!.EconomicBacktest);
        Assert.Equal("NO RELIABLE ECONOMIC BACKTEST", workspace.Analysis.MarketTiming.EconomicBacktest!.Status);
    }

    [Fact]
    public void QuantitativeFormatter_HandlesCapabilitiesAndFingerprintsDeterministically()
    {
        var value = QuantitativeFormatter.FormatCapabilityReturn(
            ForecastCapabilities.ProbabilityPositive,
            ForecastCapabilities.PointForecast,
            0.10d);

        Assert.Equal("N/A", value);
        Assert.Equal("abcd...", QuantitativeFormatter.FormatFingerprint("abcdef", 4));
        Assert.Equal("2026-08-17", QuantitativeFormatter.FormatDate(new DateOnly(2026, 8, 17)));
        Assert.Equal("EUR 1,800.00", QuantitativeFormatter.FormatCurrency(1_800d, "eur"));
    }

    private static AletheiaApplicationOptions CreateOptions()
    {
        return new AletheiaApplicationOptions
        {
            LedgerPath = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", $"{Guid.NewGuid():N}.db"),
        };
    }

    private static AletheiaApplicationOptions CreateOptions(DateTimeOffset generatedAt)
    {
        return new AletheiaApplicationOptions
        {
            LedgerPath = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", $"{Guid.NewGuid():N}.db"),
            ReportGeneratedAtUtc = generatedAt,
        };
    }

    private static AletheiaApplicationOptions CreateOptions(StubFundProvider provider)
    {
        return new AletheiaApplicationOptions
        {
            CatalogProviders = [provider],
            HistoryProviders = new Dictionary<string, IProvenanceAwareFundDataProvider>
            {
                [provider.ProviderId] = provider,
            },
            LedgerPath = Path.Combine(Path.GetTempPath(), "Aletheia.Tests", $"{Guid.NewGuid():N}.db"),
        };
    }

    private static FundHistory CreateIrregularHistory()
    {
        var gaps = new[] { 1, 3, 8, 2, 5, 11, 4 };
        var points = new List<NavPoint>();
        var date = new DateOnly(2020, 1, 2);
        for (var index = 0; index < 180; index++)
        {
            var cycle = (index % 13) - 6;
            points.Add(new NavPoint(date, 100m + (index * 0.18m) + (cycle * 0.04m)));
            date = date.AddDays(gaps[index % gaps.Length]);
        }

        var fund = new Fund(
            new FundIdentifier(FundIdentifierKind.Local, "irregular-test"),
            "Irregular Test Fund",
            "Test Provider",
            "EUR");
        return new FundHistory(fund, new NavSeries(points, ObservationFrequency.Irregular));
    }

    private static FundHistory CreateCalendarCarryForwardHistory(DateOnly startDate, int calendarDays)
    {
        var points = new List<NavPoint>();
        var value = 100m;
        for (var offset = 0; offset < calendarDays; offset++)
        {
            var date = startDate.AddDays(offset);
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                value += 0.05m + ((offset % 5) * 0.002m);
            }

            points.Add(new NavPoint(date, value));
        }

        var fund = new Fund(
            new FundIdentifier(FundIdentifierKind.Local, "calendar-carry-forward"),
            "Calendar Carry Forward Fund",
            "Unit",
            "EUR");
        return new FundHistory(fund, new NavSeries(points, ObservationFrequencyDetector.Detect(points)));
    }

    private static FundHistory CreateBusinessDailyHistory(DateOnly startDate, int businessDays)
    {
        var points = new List<NavPoint>();
        var date = startDate;
        while (points.Count < businessDays)
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                var index = points.Count;
                var cycle = (index % 17) - 8;
                points.Add(new NavPoint(date, 100m + (index * 0.03m) + (cycle * 0.002m)));
            }

            date = date.AddDays(1);
        }

        var fund = new Fund(
            new FundIdentifier(FundIdentifierKind.Local, "business-daily"),
            "Business Daily Fund",
            "Unit",
            "EUR");
        return new FundHistory(fund, new NavSeries(points, ObservationFrequencyDetector.Detect(points)));
    }

    private static DateTimeOffset ToUtcTimestamp(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Aletheia.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find repository root.");
    }

    private sealed class StubFundProvider : IFundCatalogProvider, IProvenanceAwareFundDataProvider
    {
        public const string IsinValue = "ES1234567890";

        public string ProviderId => "stub-official";

        public string DisplayName => "Stub Official Provider";

        public FundCatalogCapabilities Capabilities { get; } = new(
            SupportsFreeTextSearch: true,
            SupportsIsinSearch: true,
            SupportsPartialIsinSearch: true,
            SupportsManagerSearch: true,
            ProvidesHistoricalData: true,
            HistoricalResolution: "Reported observations only.");

        public Task<IReadOnlyList<FundSearchResult>> SearchAsync(
            FundSearchQuery query,
            int maximumResults = 50,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<FundSearchResult> results =
            [
                new FundSearchResult(
                    this.ProviderId,
                    this.DisplayName,
                    new FundIdentifier(FundIdentifierKind.Isin, IsinValue),
                    "Fondo Alfa Global",
                    IsinValue,
                    "Stub Asset Management",
                    "EUR",
                    "FI",
                    "ES",
                    true,
                    new DateOnly(2024, 1, 1),
                    new DateOnly(2024, 3, 31),
                    ObservationFrequency.Irregular,
                    "Stub official source",
                    "Stub registry 202403"),
            ];
            return Task.FromResult(results);
        }

        public Task<FundHistory> GetHistoryAsync(
            FundIdentifier fundIdentifier,
            DateOnly? from = null,
            DateOnly? to = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.CreateHistory(from, to));
        }

        public Task<Fund?> FindByIsinAsync(string isin, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fund = string.Equals(isin, IsinValue, StringComparison.OrdinalIgnoreCase)
                ? new Fund(new FundIdentifier(FundIdentifierKind.Isin, IsinValue), "Fondo Alfa Global", this.DisplayName, "EUR")
                : null;
            return Task.FromResult(fund);
        }

        public Task<FundHistoryResult> GetHistoryWithProvenanceAsync(
            FundIdentifier fundIdentifier,
            DateOnly? from = null,
            DateOnly? to = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var history = this.CreateHistory(from, to);
            var fingerprint = new DatasetFingerprintCalculator().CalculateSha256(history.NavSeries);
            var provenance = new FundDataProvenance(
                this.ProviderId,
                this.DisplayName,
                new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero),
                fundIdentifier,
                IsinValue,
                new Uri("https://example.test/stub"),
                "Stub source reference",
                history.NavSeries.ObservationFrequency,
                from,
                to,
                history.NavSeries.StartDate,
                history.NavSeries.EndDate,
                history.NavSeries.Count,
                history.NavSeries.Count,
                fingerprint,
                false,
                null);
            return Task.FromResult(new FundHistoryResult(history, provenance));
        }

        private FundHistory CreateHistory(DateOnly? from, DateOnly? to)
        {
            var start = new DateOnly(2024, 1, 1);
            var points = Enumerable.Range(0, 95)
                .Select(offset => new NavPoint(start.AddDays(offset), 100m + offset))
                .Where(point => point.Date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                .Where(point => point.Date != new DateOnly(2024, 1, 15))
                .Where(point => (!from.HasValue || point.Date >= from.Value) && (!to.HasValue || point.Date <= to.Value))
                .ToArray();
            var series = new NavSeries(points, ObservationFrequencyDetector.Detect(points));
            var fund = new Fund(new FundIdentifier(FundIdentifierKind.Isin, IsinValue), "Fondo Alfa Global", this.DisplayName, "EUR");
            return new FundHistory(fund, series);
        }
    }
}
