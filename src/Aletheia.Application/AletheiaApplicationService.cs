#pragma warning disable SA1204 // Workflow helpers are grouped near the public application use cases they support.

using System.Globalization;
using Aletheia.Analytics;
using Aletheia.Core;
using Aletheia.Data;
using Aletheia.Dynamics;
using Aletheia.Mathematics;
using Aletheia.Persistence;
using Aletheia.Simulation;
using Aletheia.Spectral;
using Aletheia.TimeSeries;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Coordinates reusable Aletheia use cases for CLI and desktop front ends.
/// </summary>
public sealed class AletheiaApplicationService
{
    private const int MinimumEconomicBacktestSignals = 10;

    private readonly AletheiaApplicationOptions options;
    private readonly FundDiscoveryService discoveryService;
    private readonly ReturnCalculator returnCalculator = new();
    private readonly ElapsedTimeAnnualizationEstimator irregularAnnualizationEstimator = new();
    private readonly RiskMetricsCalculator riskCalculator;
    private readonly RollingAnalyticsCalculator rollingAnalyticsCalculator;
    private readonly TimeDomainFeatureCalculator featureCalculator = new();
    private readonly DynamicStateFeaturePipeline statePipeline;
    private readonly PowerSpectrumAnalyzer spectralAnalyzer = new();
    private readonly HistoricalAnalogueFinder analogueFinder = new();
    private readonly HistoricalAnalogueOutcomeAnalyzer analogueOutcomeAnalyzer = new();
    private readonly DataQualityAnalyzer qualityAnalyzer = new();
    private readonly DatasetFingerprintCalculator fingerprintCalculator = new();
    private readonly EffectiveNavSeriesBuilder effectiveSeriesBuilder = new();
    private readonly FundResearchReportBuilder researchReportBuilder = new();
    private readonly MarketTimingAssessmentBuilder marketTimingAssessmentBuilder = new();
    private readonly MarketTimingAssessmentBuilder automaticMarketTimingAssessmentBuilder = new(CreateAutomaticMarketTimingOptions());

    /// <summary>
    /// Initializes a new instance of the <see cref="AletheiaApplicationService"/> class.
    /// </summary>
    /// <param name="options">Optional application settings.</param>
    public AletheiaApplicationService(AletheiaApplicationOptions? options = null)
    {
        this.options = options ?? new AletheiaApplicationOptions();
        this.riskCalculator = new RiskMetricsCalculator(
            irregularAnnualizationEstimator: this.irregularAnnualizationEstimator);
        this.rollingAnalyticsCalculator = new RollingAnalyticsCalculator(this.returnCalculator, this.riskCalculator);
        this.statePipeline = new DynamicStateFeaturePipeline(riskCalculator: this.riskCalculator);
        this.discoveryService = CreateDiscoveryService(this.options);
    }

    /// <summary>
    /// Searches configured fund catalogs.
    /// </summary>
    /// <param name="query">The user-entered query.</param>
    /// <param name="maximumResults">The maximum result count.</param>
    /// <param name="cancellationToken">A token used to cancel provider I/O.</param>
    /// <returns>Search results.</returns>
    public Task<IReadOnlyList<FundSearchResultSummary>> SearchFundsAsync(
        string query,
        int maximumResults = 50,
        CancellationToken cancellationToken = default)
    {
        return this.discoveryService.SearchAsync(query, maximumResults, cancellationToken);
    }

    /// <summary>
    /// Loads and analyzes the deterministic sample fund.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <param name="progress">Optional analysis-stage progress reporter.</param>
    /// <returns>The populated workspace.</returns>
    public async Task<FundWorkspace> LoadSampleWorkspaceAsync(
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading dataset");
        var provider = new SampleFundDataProvider();
        var result = await provider.GetHistoryWithProvenanceAsync(
            SampleFundDataProvider.GetSampleIdentifier(),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return this.AnalyzeFund(result, null, cancellationToken, progress);
    }

    /// <summary>
    /// Loads and analyzes a local CSV fund dataset.
    /// </summary>
    /// <param name="filePath">The CSV file path.</param>
    /// <param name="cancellationToken">A token used to cancel file I/O.</param>
    /// <param name="progress">Optional analysis-stage progress reporter.</param>
    /// <returns>The populated workspace.</returns>
    public async Task<FundWorkspace> LoadCsvWorkspaceAsync(
        string filePath,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("CSV path cannot be empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The supplied CSV file does not exist.", filePath);
        }

        progress?.Report("Loading dataset");
        var result = await new CsvFundDataProvider().GetHistoryFromFileWithProvenanceAsync(filePath, cancellationToken).ConfigureAwait(false);
        return this.AnalyzeFund(result, Path.GetFullPath(filePath), cancellationToken, progress);
    }

    /// <summary>
    /// Loads a provider-backed fund/share-class and analyzes it.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="fundIdentifier">The selected fund identifier.</param>
    /// <param name="from">The optional start date.</param>
    /// <param name="to">The optional end date.</param>
    /// <param name="cancellationToken">A token used to cancel provider I/O and analysis.</param>
    /// <param name="progress">Optional analysis-stage progress reporter.</param>
    /// <returns>The populated workspace.</returns>
    public async Task<FundWorkspace> LoadProviderWorkspaceAsync(
        string providerId,
        FundIdentifier fundIdentifier,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading dataset");
        var result = await this.discoveryService.LoadHistoryAsync(
            providerId,
            fundIdentifier,
            from,
            to,
            cancellationToken).ConfigureAwait(false);
        return this.AnalyzeFund(result, null, cancellationToken, progress);
    }

    /// <summary>
    /// Runs the standard fund analysis for an already loaded history.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="sourcePath">The optional source path.</param>
    /// <param name="cancellationToken">A token used to cancel long-running analysis.</param>
    /// <param name="progress">Optional analysis-stage progress reporter.</param>
    /// <returns>The populated workspace.</returns>
    public FundWorkspace AnalyzeFund(
        FundHistory history,
        string? sourcePath = null,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        return this.AnalyzeFund(new FundHistoryResult(history, CreateFallbackProvenance(history, sourcePath)), sourcePath, cancellationToken, progress);
    }

    /// <summary>
    /// Runs the standard fund analysis for an already loaded history with provenance.
    /// </summary>
    /// <param name="result">The fund history and provenance.</param>
    /// <param name="sourcePath">The optional source path.</param>
    /// <param name="cancellationToken">A token used to cancel long-running analysis.</param>
    /// <param name="progress">Optional analysis-stage progress reporter.</param>
    /// <returns>The populated workspace.</returns>
    public FundWorkspace AnalyzeFund(
        FundHistoryResult result,
        string? sourcePath = null,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report("Preparing time series");
        var sourceHistory = result.History;
        var effectiveSeries = this.effectiveSeriesBuilder.Build(sourceHistory.NavSeries);
        var history = new FundHistory(sourceHistory.Fund, effectiveSeries.NavSeries);
        var dataset = this.CreateEvaluationDataset(history);
        var navSeries = history.NavSeries;
        progress?.Report("Computing risk metrics");
        var simpleReturns = this.returnCalculator.CalculateSimpleReturns(navSeries);
        var logReturns = this.returnCalculator.CalculateLogReturns(navSeries);
        var rollingWindow = this.ResolveRollingWindow(navSeries.Count);
        var irregularPeriodsPerYear = this.ResolveIrregularPeriodsPerYear(navSeries);
        var rollingReturn = navSeries.Count > rollingWindow
            ? this.rollingAnalyticsCalculator.RollingReturn(navSeries, rollingWindow)
            : EmptySeries(navSeries.ObservationFrequency);
        var rollingVolatility = simpleReturns.Count >= Math.Max(2, rollingWindow)
            ? this.rollingAnalyticsCalculator.RollingVolatility(simpleReturns, rollingWindow, irregularPeriodsPerYear)
            : EmptySeries(navSeries.ObservationFrequency);
        var drawdownPath = BuildDrawdownPath(navSeries);
        var quality = this.qualityAnalyzer.Evaluate(sourceHistory.NavSeries.Points);
        progress?.Report("Computing dynamic state");
        var currentState = this.statePipeline.Build(navSeries, navSeries.Count - 1);
        var stateHistory = new HistoricalAnalogueFeatureBuilder(this.statePipeline).Build(navSeries);
        var analogueSearch = this.analogueFinder.FindNearestWithDiagnostics(stateHistory, currentState, 50);
        progress?.Report("Computing spectral diagnostics");
        var logValues = logReturns.ToValueArray();
        var spectrum = this.spectralAnalyzer.Analyze(logValues);
        var spectralStability = new RollingSpectralStabilityAnalyzer(this.spectralAnalyzer).Analyze(
            logValues,
            Math.Min(256, Math.Max(32, Math.Max(4, logReturns.Count / 4))),
            Math.Max(1, Math.Min(64, Math.Max(1, logReturns.Count / 8))));
        var arModel = new AutoregressiveStateModel();
        var arFit = arModel.Fit(new DynamicModelInput(logReturns));
        var arForecast = arModel.Forecast(currentState, ForecastHorizon.Observations(21));
        var analysis = new FundAnalysisResult(
            CreateDatasetSummary(sourceHistory, effectiveSeries, dataset, sourcePath, result.Provenance, this.options),
            quality,
            new PerformanceSummary(
                this.returnCalculator.CalculateCagr(navSeries),
                this.returnCalculator.CalculateCumulativeReturn(navSeries),
                this.riskCalculator.CalculateAnnualizedVolatility(simpleReturns, irregularPeriodsPerYear),
                this.riskCalculator.CalculateMaximumDrawdown(navSeries),
                this.riskCalculator.CalculateCurrentDrawdown(navSeries),
                this.riskCalculator.CalculateSharpeRatio(simpleReturns, periodsPerYear: irregularPeriodsPerYear),
                this.riskCalculator.CalculateSortinoRatio(simpleReturns, periodsPerYear: irregularPeriodsPerYear),
                this.featureCalculator.CalculateAutocorrelation(logReturns, 1)),
            BuildDistributionSummary(simpleReturns.ToValueArray()),
            ToDatedValues(navSeries),
            BuildCumulativeReturnSeries(navSeries),
            ToDatedValues(simpleReturns),
            ToDatedValues(logReturns),
            ToDatedValues(rollingReturn),
            ToDatedValues(rollingVolatility),
            drawdownPath,
            currentState,
            stateHistory,
            BuildStateProjection(stateHistory, currentState),
            spectrum,
            spectralStability,
            arFit,
            arForecast,
            BuildAnalogueAnalysis(navSeries, analogueSearch, this.options),
            this.RunCurrentForecasts(history, dataset, cancellationToken, progress));
        progress?.Report("Preparing report");
        var initialReport = this.researchReportBuilder.Build(history, analysis);
        analysis = analysis with
        {
            ResearchReport = initialReport,
        };
        progress?.Report("Validating market timing");
        var timing = this.automaticMarketTimingAssessmentBuilder.Build(history, analysis);
        analysis = analysis with
        {
            MarketTiming = this.AttachEconomicBacktest(history, timing),
        };
        analysis = analysis with
        {
            ResearchReport = this.researchReportBuilder.Build(history, analysis),
        };

        progress?.Report("Analysis complete");
        return new FundWorkspace(history, dataset, analysis);
    }

    /// <summary>
    /// Runs a periodic-investment Monte Carlo baseline for the active fund.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="request">The requested capital, contribution, horizon, and path settings.</param>
    /// <param name="cancellationToken">A token used to cancel simulation.</param>
    /// <returns>The presentation-ready simulation summary.</returns>
    public InvestmentSimulationSummary RunInvestmentSimulation(
        FundWorkspace workspace,
        InvestmentSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(request);
        if (request.HorizonYears is < 1 or > 50)
        {
            throw new InvalidOperationException("Simulation horizon must be between 1 and 50 years.");
        }

        var options = new InvestmentPlanOptions
        {
            InitialInvestment = request.InitialInvestment,
            MonthlyContribution = request.MonthlyContribution,
            HorizonMonths = checked(request.HorizonYears * 12),
            PathCount = request.PathCount,
            Seed = request.Seed,
        };
        var result = new InvestmentPlanSimulator(options).Simulate(
            workspace.Analysis.LogReturns.Select(value => value.Value).ToArray(),
            workspace.History.NavSeries.ObservationFrequency,
            workspace.History.NavSeries.EndDate,
            this.ResolveIrregularPeriodsPerYear(workspace.History.NavSeries),
            cancellationToken);
        return new InvestmentSimulationSummary(
            workspace.Analysis.Dataset,
            request,
            result.StartDate,
            result.TargetDate,
            result.TotalContributed,
            result.MeanTerminalValue,
            result.MedianTerminalValue,
            result.P10TerminalValue,
            result.P25TerminalValue,
            result.P75TerminalValue,
            result.P90TerminalValue,
            result.ProbabilityTerminalBelowContributions,
            result.ObservationPeriodsPerMonth,
            result.HistoricalMeanLogReturnPerObservation,
            result.HistoricalStandardDeviationPerObservation,
            result.MonthlyMeanLogReturn,
            result.MonthlyStandardDeviation,
            result.Trajectory.Select(point => new InvestmentValueProjectionPoint(
                point.MonthOffset,
                point.Date,
                point.TotalContributed,
                point.MeanValue,
                point.P10Value,
                point.P25Value,
                point.MedianValue,
                point.P75Value,
                point.P90Value)).ToArray(),
            result.Methodology);
    }

    /// <summary>
    /// Runs Model Arena through the shared application layer.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    /// <returns>The completed arena result.</returns>
    public async Task<ModelArenaResult> RunModelArenaAsync(
        FundWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return await this.RunModelArenaAsync(workspace, ForecastHorizon.CalendarDays(90), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs Model Arena for each standard user-facing forecast horizon.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    /// <returns>Completed arena results indexed by horizon.</returns>
    public async Task<IReadOnlyList<ModelArenaResult>> RunModelArenasAsync(
        FundWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        return await this.RunModelArenasAsync(
            workspace,
            ForecastHorizon.CalendarDays(90),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs Model Arena for standard horizons plus a user-preferred horizon.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="preferredHorizon">The user-selected horizon to include in validation.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    /// <returns>Completed arena results indexed by horizon.</returns>
    public async Task<IReadOnlyList<ModelArenaResult>> RunModelArenasAsync(
        FundWorkspace workspace,
        ForecastHorizon preferredHorizon,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var results = new List<ModelArenaResult>();
        foreach (var horizon in StandardForecastHorizons().Append(preferredHorizon).Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await this.RunModelArenaAsync(workspace, horizon, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <summary>
    /// Runs Model Arena for one explicit horizon.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="horizon">The forecast horizon to validate.</param>
    /// <param name="cancellationToken">A token used to cancel validation.</param>
    /// <returns>The completed arena result.</returns>
    public async Task<ModelArenaResult> RunModelArenaAsync(
        FundWorkspace workspace,
        ForecastHorizon horizon,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var ledger = new SqlitePredictionLedger(this.GetLedgerPath());
        var evaluator = new WalkForwardEvaluator(
            horizonResolver: this.CreateHorizonResolver(workspace.History.NavSeries));
        var arena = new ModelArena(evaluator);
        return await arena.EvaluateAsync(
            CreateDefaultForecastModels(),
            workspace.EvaluationDataset,
            CreateDefaultWalkForwardOptions(workspace.History, horizon),
            ledger,
            cancellationToken,
            CreateDefaultArenaOptions()).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a research report, optionally incorporating Model Arena evidence attached to the workspace.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <returns>The research report.</returns>
    public FundResearchReport BuildResearchReport(FundWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return this.researchReportBuilder.Build(
            workspace.History,
            workspace.Analysis,
            workspace.Arenas,
            workspace.Analysis.MarketTiming);
    }

    /// <summary>
    /// Builds a market-timing assessment, optionally incorporating evidence attached to the workspace.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <returns>The market-timing assessment.</returns>
    public MarketTimingAssessment BuildMarketTimingAssessment(FundWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var analysis = workspace.Arena is null
            ? workspace.Analysis
            : workspace.Analysis with { ResearchReport = this.BuildResearchReport(workspace) };
        var timing = this.marketTimingAssessmentBuilder.Build(workspace.History, analysis);
        return this.AttachEconomicBacktest(workspace.History, timing);
    }

    /// <summary>
    /// Runs the integrated OOS economic timing backtest for a workspace.
    /// </summary>
    /// <param name="workspace">The active workspace.</param>
    /// <param name="options">Optional execution and cost settings.</param>
    /// <returns>The economic timing backtest assessment.</returns>
    public TimingEconomicBacktestAssessment RunTimingEconomicBacktest(
        FundWorkspace workspace,
        TimingBacktestOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var timing = workspace.Analysis.MarketTiming ?? this.marketTimingAssessmentBuilder.Build(workspace.History, workspace.Analysis);
        return this.RunTimingEconomicBacktest(workspace.History, timing, options);
    }

    /// <summary>
    /// Reads recent prediction-ledger records.
    /// </summary>
    /// <param name="limit">The maximum number of predictions.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>Ledger summary rows.</returns>
    public async Task<IReadOnlyList<PredictionLedgerSummary>> GetPredictionListAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var ledger = new SqlitePredictionLedger(this.GetLedgerPath());
        await ledger.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var predictions = await ledger.ListPredictionsAsync(limit, cancellationToken).ConfigureAwait(false);
        return predictions.Select(ToPredictionSummary).ToArray();
    }

    /// <summary>
    /// Reads one prediction and its immutable evaluations.
    /// </summary>
    /// <param name="predictionId">The prediction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>The prediction details, or <see langword="null"/> when absent.</returns>
    public async Task<PredictionDetailsResult?> GetPredictionDetailsAsync(
        Guid predictionId,
        CancellationToken cancellationToken = default)
    {
        var ledger = new SqlitePredictionLedger(this.GetLedgerPath());
        await ledger.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var prediction = await ledger.GetPredictionAsync(predictionId, cancellationToken).ConfigureAwait(false);
        if (prediction is null)
        {
            return null;
        }

        var evaluations = await ledger.GetEvaluationsAsync(predictionId, cancellationToken).ConfigureAwait(false);
        return new PredictionDetailsResult(prediction, evaluations);
    }

    /// <summary>
    /// Creates the default Model Arena forecast models.
    /// </summary>
    /// <returns>The default model set.</returns>
    public IReadOnlyList<IForecastModel> CreateDefaultForecastModels()
    {
        return
        [
            new ZeroReturnForecastModel(),
            new HistoricalProbabilityBaselineForecastModel(minimumSamples: 10),
            new HistoricalMeanForecastModel(),
            new AutoregressiveForecastModel(statePipeline: this.statePipeline),
            new StateSpaceForecastModel(),
            new HistoricalAnalogueForecastModel(
                maximumAnalogues: 25,
                minimumAnalogues: 5,
                statePipeline: this.statePipeline),
        ];
    }

    /// <summary>
    /// Creates the default Model Arena options.
    /// </summary>
    /// <returns>The default arena options.</returns>
    public ModelArenaOptions CreateDefaultArenaOptions()
    {
        return new ModelArenaOptions
        {
            PointForecastBaselineModelId = ZeroReturnForecastModel.ModelId,
            ProbabilityBaselineModelId = HistoricalProbabilityBaselineForecastModel.ModelId,
            MinimumAllSamples = 10,
            MinimumCommonSupportSamples = 10,
            MinimumNonOverlappingSamples = 5,
        };
    }

    /// <summary>
    /// Creates the default walk-forward validation options for a history.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <returns>The default walk-forward options.</returns>
    public WalkForwardEvaluationOptions CreateDefaultWalkForwardOptions(FundHistory history)
    {
        return this.CreateDefaultWalkForwardOptions(history, ForecastHorizon.CalendarDays(90));
    }

    /// <summary>
    /// Creates the default walk-forward validation options for a history and horizon.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="horizon">The horizon under validation.</param>
    /// <returns>The default walk-forward options.</returns>
    public WalkForwardEvaluationOptions CreateDefaultWalkForwardOptions(FundHistory history, ForecastHorizon horizon)
    {
        ArgumentNullException.ThrowIfNull(history);
        return new WalkForwardEvaluationOptions
        {
            MinimumTrainingObservations = Math.Min(500, Math.Max(60, history.NavSeries.Count / 3)),
            ForecastHorizon = horizon,
            StepSize = Math.Max(1, Math.Min(63, Math.Max(1, history.NavSeries.Count / 40))),
            MinimumEvaluationSamples = 10,
            Calibration = new CalibrationOptions { BinCount = 10 },
        };
    }

    private MarketTimingAssessment AttachEconomicBacktest(FundHistory history, MarketTimingAssessment timing)
    {
        if (timing.EconomicBacktest is not null)
        {
            return timing;
        }

        return timing with { EconomicBacktest = this.RunTimingEconomicBacktest(history, timing, null) };
    }

    private TimingEconomicBacktestAssessment RunTimingEconomicBacktest(
        FundHistory history,
        MarketTimingAssessment timing,
        TimingBacktestOptions? options)
    {
        var selected = SelectEconomicBacktestArena(timing);
        if (selected is null)
        {
            return CreateNoReliableEconomicBacktest("No timing horizon with historical OOS predictions is available.", options);
        }

        if (!selected.Ensemble.IsActive)
        {
            return CreateNoReliableEconomicBacktest(
                $"Validated ensemble inactive for {selected.Definition.Horizon}: {selected.Ensemble.FallbackReason}",
                options,
                selected.Definition.Horizon);
        }

        var effectiveOptions = options ?? new TimingBacktestOptions();
        var trace = selected.HistoricalPredictions
            .Where(prediction => prediction.Evidence != EvidenceStrength.Insufficient && prediction.Reliability > 0d)
            .OrderBy(prediction => prediction.Date)
            .Select(prediction => BuildSignalTrace(history.NavSeries, prediction, effectiveOptions.ExecutionDelayObservations))
            .Where(item => item is not null)
            .Cast<TimingEconomicSignalTrace>()
            .GroupBy(item => item.DecisionDate)
            .Select(group => group.Last())
            .OrderBy(item => item.DecisionDate)
            .ToArray();
        if (trace.Length < MinimumEconomicBacktestSignals)
        {
            return CreateNoReliableEconomicBacktest(
                $"Only {trace.Length.ToString(CultureInfo.InvariantCulture)} usable historical OOS timing decision(s); minimum {MinimumEconomicBacktestSignals.ToString(CultureInfo.InvariantCulture)}.",
                effectiveOptions,
                selected.Definition.Horizon,
                trace);
        }

        var signals = trace
            .Select(item => new TimingBacktestSignal(
                item.DecisionDate,
                item.TargetExposure,
                "Aletheia historical OOS timing",
                item.CalculationDate,
                item.DecisionDate))
            .ToArray();
        var results = new TimingDecisionBacktester().Run(history.NavSeries, signals, effectiveOptions);
        return new TimingEconomicBacktestAssessment(
            "OOS ECONOMIC BACKTEST",
            true,
            "Historical OOS timing predictions were converted to target exposure and executed with the configured delay. A good Brier score or ReliabilityIndex does not guarantee economic profitability.",
            selected.Definition.Horizon,
            trace.Length,
            trace[0].DecisionDate,
            trace[^1].DecisionDate,
            effectiveOptions.ExecutionDelayObservations,
            effectiveOptions.TransactionCostRate,
            effectiveOptions.SlippageRate,
            trace,
            results);
    }

    private static MarketTimingArenaResult? SelectEconomicBacktestArena(MarketTimingAssessment timing)
    {
        var preferred = timing.Decision.PrimaryHorizon is null
            ? null
            : timing.ModelArenaResults.FirstOrDefault(item => item.Definition.Horizon.Equals(timing.Decision.PrimaryHorizon));
        if (preferred is not null)
        {
            return preferred;
        }

        return timing.ModelArenaResults
            .OrderByDescending(item => item.Ensemble.IsActive)
            .ThenByDescending(item => item.HistoricalPredictions.Count)
            .ThenByDescending(item => item.Ensemble.Reliability)
            .FirstOrDefault(item => item.HistoricalPredictions.Count > 0);
    }

    private static TimingEconomicSignalTrace? BuildSignalTrace(
        NavSeries navSeries,
        HistoricalTimingPrediction prediction,
        int executionDelayObservations)
    {
        var signalIndex = FindIndexOnOrAfter(navSeries, prediction.Date);
        if (signalIndex < 0)
        {
            return null;
        }

        var executionIndex = signalIndex + executionDelayObservations;
        if (executionIndex <= signalIndex || executionIndex >= navSeries.Count)
        {
            return null;
        }

        return new TimingEconomicSignalTrace(
            prediction.Date,
            prediction.Date,
            navSeries[executionIndex].Date,
            prediction.Zone.ToString(),
            prediction.Reliability,
            ResolveTargetExposure(prediction.Zone));
    }

    private static double ResolveTargetExposure(MarketTimingZone zone)
    {
        return zone switch
        {
            MarketTimingZone.StrongAccumulation or MarketTimingZone.Accumulation => 1d,
            MarketTimingZone.WatchPositive => 0.75d,
            MarketTimingZone.Neutral => 0.5d,
            MarketTimingZone.WatchNegative => 0.25d,
            MarketTimingZone.Reduction or MarketTimingZone.StrongReduction => 0d,
            _ => 0d,
        };
    }

    private static TimingEconomicBacktestAssessment CreateNoReliableEconomicBacktest(
        string diagnostic,
        TimingBacktestOptions? options,
        ForecastHorizon? horizon = null,
        IReadOnlyList<TimingEconomicSignalTrace>? trace = null)
    {
        var effectiveOptions = options ?? new TimingBacktestOptions();
        return new TimingEconomicBacktestAssessment(
            "NO RELIABLE ECONOMIC BACKTEST",
            false,
            diagnostic,
            horizon,
            trace?.Count ?? 0,
            trace?.FirstOrDefault()?.DecisionDate,
            trace?.LastOrDefault()?.DecisionDate,
            effectiveOptions.ExecutionDelayObservations,
            effectiveOptions.TransactionCostRate,
            effectiveOptions.SlippageRate,
            trace ?? Array.Empty<TimingEconomicSignalTrace>(),
            Array.Empty<TimingBacktestResult>());
    }

    private static MarketTimingEngineOptions CreateAutomaticMarketTimingOptions()
    {
        return new MarketTimingEngineOptions
        {
            EnableStateModelFeatures = false,
            MaximumWalkForwardEvaluations = 2,
            ClassifierOptions = new MarketEventClassifierOptions
            {
                Iterations = 8,
            },
        };
    }

    private static DatasetSummary CreateDatasetSummary(
        FundHistory sourceHistory,
        EffectiveNavSeriesResult effectiveSeries,
        ForecastEvaluationDataset dataset,
        string? sourcePath,
        FundDataProvenance? provenance,
        AletheiaApplicationOptions options)
    {
        var history = dataset.History;
        var freshness = BuildDataFreshness(effectiveSeries.LastEffectiveObservationDate, options);
        return new DatasetSummary(
            history.Fund.Name,
            history.Fund.Identifier,
            history.Fund.ProviderName,
            history.Fund.Currency,
            history.NavSeries.StartDate,
            history.NavSeries.EndDate,
            history.NavSeries.Count,
            history.NavSeries.ObservationFrequency,
            dataset.DatasetIdentity.DatasetFingerprintSha256,
            sourcePath,
            provenance is null ? null : ToProvenanceSummary(provenance),
            effectiveSeries.SourceObservationCount,
            effectiveSeries.SyntheticObservationCount,
            sourceHistory.NavSeries.Count == 0 ? null : effectiveSeries.SourceStartDate,
            sourceHistory.NavSeries.Count == 0 ? null : effectiveSeries.SourceEndDate,
            effectiveSeries.LastEffectiveObservationDate,
            effectiveSeries.Policy,
            freshness);
    }

    private static DataFreshnessAssessment BuildDataFreshness(
        DateOnly lastEffectiveObservationDate,
        AletheiaApplicationOptions options)
    {
        var generatedAt = options.ReportGeneratedAtUtc ?? DateTimeOffset.UtcNow;
        var generatedDate = DateOnly.FromDateTime(generatedAt.DateTime);
        var dataAgeDays = Math.Max(0, generatedDate.DayNumber - lastEffectiveObservationDate.DayNumber);
        var status = dataAgeDays <= options.FreshDataMaxAgeDays
            ? DataFreshnessStatus.Fresh
            : dataAgeDays <= options.ActionableDataMaxAgeDays
                ? DataFreshnessStatus.Aging
                : DataFreshnessStatus.Stale;
        var diagnostic = status switch
        {
            DataFreshnessStatus.Fresh => $"Latest effective observation is {dataAgeDays.ToString(CultureInfo.InvariantCulture)} calendar day(s) old.",
            DataFreshnessStatus.Aging => $"Latest effective observation is {dataAgeDays.ToString(CultureInfo.InvariantCulture)} calendar day(s) old; current wording should be qualified.",
            _ => $"Latest effective observation is {dataAgeDays.ToString(CultureInfo.InvariantCulture)} calendar day(s) old; unqualified current decisions are blocked.",
        };
        return new DataFreshnessAssessment(
            generatedAt,
            lastEffectiveObservationDate,
            dataAgeDays,
            status,
            options.FreshDataMaxAgeDays,
            options.ActionableDataMaxAgeDays,
            diagnostic);
    }

    private static DatasetProvenanceSummary ToProvenanceSummary(FundDataProvenance provenance)
    {
        return new DatasetProvenanceSummary(
            provenance.ProviderId,
            provenance.ProviderDisplayName,
            provenance.RetrievalTimestampUtc,
            provenance.ExternalFundIdentifier,
            provenance.Isin,
            provenance.SourceUri,
            provenance.SourceReference,
            provenance.ObservationFrequency,
            provenance.RequestedStartDate,
            provenance.RequestedEndDate,
            provenance.ReturnedStartDate,
            provenance.ReturnedEndDate,
            provenance.OriginalObservationCount,
            provenance.NormalizedObservationCount,
            provenance.DatasetFingerprintSha256,
            provenance.IsFromCache,
            provenance.CacheKey);
    }

    private static FundDiscoveryService CreateDiscoveryService(AletheiaApplicationOptions options)
    {
        if (options.CatalogProviders is not null && options.HistoryProviders is not null)
        {
            return new FundDiscoveryService(options.CatalogProviders, options.HistoryProviders);
        }

        var cnmv = new CnmvIicProvider();
        var catalogProviders = options.CatalogProviders ?? [cnmv];
        var historyProviders = options.HistoryProviders ?? new Dictionary<string, IProvenanceAwareFundDataProvider>
        {
            [cnmv.ProviderId] = cnmv,
        };
        return new FundDiscoveryService(catalogProviders, historyProviders);
    }

    private static FundDataProvenance CreateFallbackProvenance(FundHistory history, string? sourcePath)
    {
        var fingerprint = new DatasetFingerprintCalculator().CalculateSha256(history.NavSeries);
        return new FundDataProvenance(
            history.Fund.ProviderName ?? "unknown",
            history.Fund.ProviderName ?? "Unknown",
            DateTimeOffset.UtcNow,
            history.Fund.Identifier,
            history.Fund.Identifier.Kind == FundIdentifierKind.Isin ? history.Fund.Identifier.Value : null,
            sourcePath is null ? null : new Uri(Path.GetFullPath(sourcePath)),
            sourcePath,
            history.NavSeries.ObservationFrequency,
            null,
            null,
            history.NavSeries.Count == 0 ? null : history.NavSeries.StartDate,
            history.NavSeries.Count == 0 ? null : history.NavSeries.EndDate,
            history.NavSeries.Count,
            history.NavSeries.Count,
            fingerprint,
            false,
            null);
    }

    private static DistributionSummary BuildDistributionSummary(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return new DistributionSummary(0d, 0d, 0d, 0d, 0d, Array.Empty<HistogramBin>());
        }

        return new DistributionSummary(
            DescriptiveStatistics.Mean(values),
            DescriptiveStatistics.Median(values),
            values.Count < 2 ? 0d : DescriptiveStatistics.SampleStandardDeviation(values),
            values.Min(),
            values.Max(),
            BuildHistogram(values, 24));
    }

    private static IReadOnlyList<HistogramBin> BuildHistogram(IReadOnlyList<double> values, int binCount)
    {
        if (values.Count == 0)
        {
            return Array.Empty<HistogramBin>();
        }

        var minimum = values.Min();
        var maximum = values.Max();
        if (minimum == maximum)
        {
            return [new HistogramBin(minimum, maximum, values.Count)];
        }

        var width = (maximum - minimum) / binCount;
        var counts = new int[binCount];
        foreach (var value in values)
        {
            var index = Math.Min(binCount - 1, (int)((value - minimum) / width));
            counts[index]++;
        }

        return Enumerable.Range(0, binCount)
            .Select(index => new HistogramBin(minimum + (index * width), minimum + ((index + 1) * width), counts[index]))
            .ToArray();
    }

    private static IReadOnlyList<DatedValue> ToDatedValues(NavSeries navSeries)
    {
        return navSeries.Points
            .Select(point => new DatedValue(point.Date, (double)point.Value))
            .ToArray();
    }

    private static IReadOnlyList<DatedValue> ToDatedValues(TimeSeries<double> series)
    {
        return series.Points
            .Select(point => new DatedValue(point.Date, point.Value))
            .ToArray();
    }

    private static IReadOnlyList<DatedValue> BuildCumulativeReturnSeries(NavSeries navSeries)
    {
        if (navSeries.Count == 0)
        {
            return Array.Empty<DatedValue>();
        }

        var first = navSeries[0].Value;
        return navSeries.Points
            .Select(point => new DatedValue(point.Date, ((double)point.Value / (double)first) - 1d))
            .ToArray();
    }

    private static IReadOnlyList<DatedValue> BuildDrawdownPath(NavSeries navSeries)
    {
        if (navSeries.Count == 0)
        {
            return Array.Empty<DatedValue>();
        }

        var peak = navSeries[0].Value;
        var result = new List<DatedValue>(navSeries.Count);
        foreach (var point in navSeries.Points)
        {
            if (point.Value > peak)
            {
                peak = point.Value;
            }

            result.Add(new DatedValue(point.Date, ((double)point.Value / (double)peak) - 1d));
        }

        return result;
    }

    private static IReadOnlyList<StateProjectionPoint> BuildStateProjection(
        IReadOnlyList<StateObservation> history,
        DynamicState currentState)
    {
        var points = history.Select(observation => new StateProjectionPoint(
            observation.Date,
            GetDimension(observation.Dimensions, StandardStateDimensions.Momentum),
            GetDimension(observation.Dimensions, StandardStateDimensions.Volatility),
            GetDimension(observation.Dimensions, StandardStateDimensions.LogNavVelocityPerObservation),
            GetDimension(observation.Dimensions, StandardStateDimensions.LogNavAccelerationPerObservationSquared),
            false)).ToList();
        points.Add(new StateProjectionPoint(
            currentState.Date,
            GetDimension(currentState.Dimensions, StandardStateDimensions.Momentum),
            GetDimension(currentState.Dimensions, StandardStateDimensions.Volatility),
            GetDimension(currentState.Dimensions, StandardStateDimensions.LogNavVelocityPerObservation),
            GetDimension(currentState.Dimensions, StandardStateDimensions.LogNavAccelerationPerObservationSquared),
            true));
        return points;
    }

    private static AnalogueAnalysisResult BuildAnalogueAnalysis(
        NavSeries navSeries,
        HistoricalAnalogueSearchResult search,
        AletheiaApplicationOptions options)
    {
        var outcomeAnalyzer = new HistoricalAnalogueOutcomeAnalyzer();
        var paths = search.Matches
            .Take(options.MaximumAnaloguePaths)
            .Select(match => BuildAnaloguePath(navSeries, match, options.AnaloguePathHorizonObservations))
            .Where(path => path is not null)
            .Cast<AnaloguePath>()
            .ToArray();
        return new AnalogueAnalysisResult(
            search,
            search.Matches.Select(match => new AnalogueMatchSummary(
                match.Observation.Date,
                match.Distance,
                CalculateFutureReturn(navSeries, match.Observation.Date, ForecastHorizon.CalendarDays(30)),
                CalculateFutureReturn(navSeries, match.Observation.Date, ForecastHorizon.CalendarDays(90)),
                CalculateFutureReturn(navSeries, match.Observation.Date, ForecastHorizon.CalendarDays(180)))).ToArray(),
            paths,
            BuildAggregatePath(paths),
            outcomeAnalyzer.Analyze(navSeries, search.Matches, ForecastHorizon.CalendarDays(30)),
            outcomeAnalyzer.Analyze(navSeries, search.Matches, ForecastHorizon.CalendarDays(90)),
            outcomeAnalyzer.Analyze(navSeries, search.Matches, ForecastHorizon.CalendarDays(180)));
    }

    private static AnaloguePath? BuildAnaloguePath(
        NavSeries navSeries,
        HistoricalAnalogueResult match,
        int horizonObservations)
    {
        var startIndex = FindIndexOnOrAfter(navSeries, match.Observation.Date);
        if (startIndex < 0 || startIndex >= navSeries.Count)
        {
            return null;
        }

        var start = navSeries[startIndex].Value;
        if (start <= 0m)
        {
            return null;
        }

        var lastIndex = Math.Min(navSeries.Count - 1, startIndex + horizonObservations);
        var points = new List<AnaloguePathPoint>(lastIndex - startIndex + 1);
        for (var index = startIndex; index <= lastIndex; index++)
        {
            points.Add(new AnaloguePathPoint(index - startIndex, ((double)navSeries[index].Value / (double)start) - 1d));
        }

        return new AnaloguePath(match.Observation.Date, match.Distance, points);
    }

    private static IReadOnlyList<AnalogueAggregatePoint> BuildAggregatePath(IReadOnlyList<AnaloguePath> paths)
    {
        if (paths.Count == 0)
        {
            return Array.Empty<AnalogueAggregatePoint>();
        }

        var maximumOffset = paths.Max(path => path.Points.Count == 0 ? 0 : path.Points.Max(point => point.ObservationOffset));
        var aggregate = new List<AnalogueAggregatePoint>();
        for (var offset = 0; offset <= maximumOffset; offset++)
        {
            var values = paths
                .SelectMany(path => path.Points.Where(point => point.ObservationOffset == offset).Select(point => point.Return))
                .ToArray();
            if (values.Length < 3)
            {
                continue;
            }

            aggregate.Add(new AnalogueAggregatePoint(
                offset,
                values.Length,
                DescriptiveStatistics.Percentile(values, 25d),
                DescriptiveStatistics.Median(values),
                DescriptiveStatistics.Percentile(values, 75d)));
        }

        return aggregate;
    }

    private static double? CalculateFutureReturn(
        NavSeries navSeries,
        DateOnly date,
        ForecastHorizon horizon)
    {
        var startIndex = FindIndexOnOrAfter(navSeries, date);
        if (startIndex < 0)
        {
            return null;
        }

        var endIndex = ResolveEndIndex(navSeries, startIndex, horizon);
        if (endIndex < 0 || endIndex <= startIndex)
        {
            return null;
        }

        return ((double)navSeries[endIndex].Value / (double)navSeries[startIndex].Value) - 1d;
    }

    private static int ResolveEndIndex(NavSeries navSeries, int startIndex, ForecastHorizon horizon)
    {
        if (horizon.Unit == ForecastHorizonUnit.Observations)
        {
            var endIndex = startIndex + horizon.Value;
            return endIndex < navSeries.Count ? endIndex : -1;
        }

        return FindIndexOnOrAfter(navSeries, navSeries[startIndex].Date.AddDays(horizon.Value));
    }

    private static int FindIndexOnOrAfter(NavSeries navSeries, DateOnly date)
    {
        for (var index = 0; index < navSeries.Count; index++)
        {
            if (navSeries[index].Date >= date)
            {
                return index;
            }
        }

        return -1;
    }

    private static double GetDimension(IReadOnlyDictionary<StateDimension, double> dimensions, StateDimension dimension)
    {
        return dimensions.TryGetValue(dimension, out var value) ? value : 0d;
    }

    private static TimeSeries<double> EmptySeries(ObservationFrequency frequency)
    {
        return new TimeSeries<double>(Array.Empty<TimeSeriesPoint<double>>(), frequency);
    }

    private static PredictionLedgerSummary ToPredictionSummary(PredictionLedgerRecord record)
    {
        var prediction = record.Prediction;
        return new PredictionLedgerSummary(
            prediction.PredictionId,
            record.Origin,
            prediction.FundIdentifier,
            prediction.GeneratedAtUtc,
            prediction.DataCutoffDate,
            prediction.Model.Name,
            prediction.RequestedHorizon,
            prediction.Supports(ForecastCapabilities.PointForecast) ? prediction.PointForecastReturn : null,
            prediction.Supports(ForecastCapabilities.ExpectedReturn) ? prediction.ExpectedReturn : null,
            prediction.Supports(ForecastCapabilities.ProbabilityPositive) ? prediction.ProbabilityPositive : null,
            record.TargetDate,
            prediction.DatasetIdentity.DatasetFingerprintSha256);
    }

    private ForecastCollectionResult RunCurrentForecasts(
        FundHistory history,
        ForecastEvaluationDataset dataset,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        progress?.Report("Running current forecasts");
        var runs = new List<ForecastModelRun>();
        var horizonResolver = this.CreateHorizonResolver(history.NavSeries);
        foreach (var model in CreateDefaultForecastModels())
        {
            foreach (var horizon in StandardForecastHorizons())
            {
                cancellationToken.ThrowIfCancellationRequested();
                runs.Add(this.RunCurrentForecast(
                    history,
                    dataset,
                    model,
                    horizon,
                    horizonResolver,
                    cancellationToken));
            }
        }

        return new ForecastCollectionResult(runs);
    }

    private static IReadOnlyList<ForecastHorizon> StandardForecastHorizons()
    {
        return
        [
            ForecastHorizon.CalendarDays(30),
            ForecastHorizon.CalendarDays(90),
            ForecastHorizon.CalendarDays(180),
            ForecastHorizon.CalendarDays(365),
        ];
    }

    private ForecastModelRun RunCurrentForecast(
        FundHistory history,
        ForecastEvaluationDataset dataset,
        IForecastModel model,
        ForecastHorizon horizon,
        ForecastHorizonResolver horizonResolver,
        CancellationToken cancellationToken)
    {
        var navSeries = history.NavSeries;
        var cutoffIndex = navSeries.Count - 1;
        var resolution = horizonResolver.Resolve(horizon, navSeries.EndDate, navSeries.ObservationFrequency);
        var split = new WalkForwardSplit(
            0,
            cutoffIndex,
            cutoffIndex,
            cutoffIndex,
            cutoffIndex,
            null,
            navSeries.EndDate,
            resolution.TargetDate);
        var trainingContext = new ForecastTrainingContext(dataset, navSeries, split, resolution);
        var training = model.Train(trainingContext, cancellationToken);
        if (!training.IsSuccess)
        {
            return new ForecastModelRun(
                model.Descriptor,
                model.Capabilities,
                model.PointForecastStatistic,
                model.ConfigurationFingerprint,
                horizon,
                training.Status,
                training.FailureReason,
                null);
        }

        var predictionContext = new ForecastPredictionContext(dataset, navSeries, split, resolution);
        var prediction = model.Predict(training, predictionContext, cancellationToken);
        return new ForecastModelRun(
            model.Descriptor,
            model.Capabilities,
            model.PointForecastStatistic,
            model.ConfigurationFingerprint,
            horizon,
            prediction.Status,
            prediction.FailureReason,
            prediction.Distribution);
    }

    private ForecastEvaluationDataset CreateEvaluationDataset(FundHistory history)
    {
        var fingerprint = this.fingerprintCalculator.CalculateSha256(history.NavSeries);
        return new ForecastEvaluationDataset(
            history,
            new DatasetIdentity(history.Fund.ProviderName ?? "Local", fingerprint, null),
            AletheiaRelease.ScientificVersion);
    }

    private ForecastHorizonResolver CreateHorizonResolver(NavSeries navSeries)
    {
        return new ForecastHorizonResolver(
            irregularPeriodsPerYear: this.ResolveIrregularPeriodsPerYear(navSeries));
    }

    private double? ResolveIrregularPeriodsPerYear(NavSeries navSeries)
    {
        if (navSeries.ObservationFrequency != ObservationFrequency.Irregular || navSeries.Count < 2)
        {
            return null;
        }

        return this.irregularAnnualizationEstimator.EstimatePeriodsPerYear(navSeries.ToDates());
    }

    private int ResolveRollingWindow(int observationCount)
    {
        if (observationCount < 4)
        {
            return 2;
        }

        return Math.Min(this.options.RollingWindowObservations, Math.Max(2, observationCount / 4));
    }

    private string GetLedgerPath()
    {
        if (!string.IsNullOrWhiteSpace(this.options.LedgerPath))
        {
            return this.options.LedgerPath;
        }

        var configured = Environment.GetEnvironmentVariable("ALETHEIA_LEDGER_PATH");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.CurrentDirectory, "data", "aletheia.db")
            : configured;
    }
}
