using System.Globalization;
using Aletheia.Analytics;
using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Data;
using Aletheia.Dynamics;
using Aletheia.Forecasting;
using Aletheia.Simulation;
using Aletheia.Spectral;
using Aletheia.Validation;

namespace Aletheia.Cli;

/// <summary>
/// Provides the command-line entry point for Aletheia analysis.
/// </summary>
internal static class Program
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// Runs the Aletheia command-line analysis pipeline.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var application = new AletheiaApplicationService();
            if (args.Length > 0 && string.Equals(args[0], "funds", StringComparison.OrdinalIgnoreCase))
            {
                await RunFundsCommandAsync(application, args.Skip(1).ToArray(), CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "arena", StringComparison.OrdinalIgnoreCase))
            {
                var arenaWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "arena",
                    CancellationToken.None).ConfigureAwait(false);
                await RunArenaAsync(application, arenaWorkspace, CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "score", StringComparison.OrdinalIgnoreCase))
            {
                var scoreWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "score",
                    CancellationToken.None).ConfigureAwait(false);
                WriteFundScore(scoreWorkspace.Analysis.ResearchReport ?? application.BuildResearchReport(scoreWorkspace));
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "forecast", StringComparison.OrdinalIgnoreCase))
            {
                var forecastWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "forecast",
                    CancellationToken.None).ConfigureAwait(false);
                WriteCurrentForecasts(forecastWorkspace.Analysis.Forecasts);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "timing", StringComparison.OrdinalIgnoreCase))
            {
                var timingWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "timing",
                    CancellationToken.None).ConfigureAwait(false);
                WriteMarketTiming(application.BuildMarketTimingAssessment(timingWorkspace));
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "backtest", StringComparison.OrdinalIgnoreCase))
            {
                await RunTimingBacktestCommandAsync(
                    application,
                    args.Skip(1).ToArray(),
                    CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "simulate", StringComparison.OrdinalIgnoreCase))
            {
                await RunSimulationCommandAsync(
                    application,
                    args.Skip(1).ToArray(),
                    CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "stress", StringComparison.OrdinalIgnoreCase))
            {
                var stressWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "stress",
                    CancellationToken.None).ConfigureAwait(false);
                WriteStressScenarios(stressWorkspace.Analysis.ResearchReport ?? application.BuildResearchReport(stressWorkspace));
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "report", StringComparison.OrdinalIgnoreCase))
            {
                var reportWorkspace = await LoadSourceWorkspaceAsync(
                    application,
                    args.Skip(1).ToArray(),
                    "report",
                    CancellationToken.None).ConfigureAwait(false);
                var arenas = await application.RunModelArenasAsync(reportWorkspace, CancellationToken.None).ConfigureAwait(false);
                var workspaceWithArena = reportWorkspace.WithArenas(arenas);
                var preliminaryReport = application.BuildResearchReport(workspaceWithArena);
                var preliminaryWorkspace = workspaceWithArena with
                {
                    Analysis = workspaceWithArena.Analysis with { ResearchReport = preliminaryReport },
                };
                var timing = application.BuildMarketTimingAssessment(preliminaryWorkspace);
                var finalWorkspace = preliminaryWorkspace with
                {
                    Analysis = preliminaryWorkspace.Analysis with { MarketTiming = timing },
                };
                var report = application.BuildResearchReport(finalWorkspace);
                WriteResearchReport(report);
                WriteMarketTiming(timing);
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "predictions", StringComparison.OrdinalIgnoreCase))
            {
                await RunPredictionLedgerCommandAsync(application, args.Skip(1).ToArray(), CancellationToken.None).ConfigureAwait(false);
                return 0;
            }

            var workspace = await LoadWorkspaceAsync(application, args, CancellationToken.None).ConfigureAwait(false);
            WriteApplicationAnalysis(workspace.Analysis);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Aletheia failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task RunFundsCommandAsync(
        AletheiaApplicationService application,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (args.Length >= 2 && string.Equals(args[0], "search", StringComparison.OrdinalIgnoreCase))
        {
            var query = string.Equals(args[1], "--isin", StringComparison.OrdinalIgnoreCase)
                ? string.Join(' ', args.Skip(2))
                : string.Join(' ', args.Skip(1));
            var results = await application.SearchFundsAsync(query, 25, cancellationToken).ConfigureAwait(false);
            WriteFundSearchResults(results);
            return;
        }

        throw new ArgumentException("Usage: aletheia funds search <name-or-isin> OR aletheia funds search --isin <isin>");
    }

    private static async Task<FundWorkspace> LoadWorkspaceAsync(
        AletheiaApplicationService application,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (args.Length == 0 || (args.Length is 1 && string.Equals(args[0], "sample", StringComparison.OrdinalIgnoreCase)))
        {
            return await application.LoadSampleWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        }

        if (args.Length is 2 && string.Equals(args[0], "analyze", StringComparison.OrdinalIgnoreCase))
        {
            return await application.LoadCsvWorkspaceAsync(args[1], cancellationToken).ConfigureAwait(false);
        }

        if (args.Length >= 5 && string.Equals(args[0], "analyze", StringComparison.OrdinalIgnoreCase))
        {
            var providerId = ReadRequiredOption(args, "--provider");
            var fundIdentifier = ParseFundIdentifier(ReadRequiredOption(args, "--fund"));
            var from = ParseOptionalDate(args, "--from");
            var to = ParseOptionalDate(args, "--to");
            return await application.LoadProviderWorkspaceAsync(providerId, fundIdentifier, from, to, cancellationToken).ConfigureAwait(false);
        }

        throw new ArgumentException("Usage: aletheia sample OR aletheia analyze <sample-fund.csv> OR aletheia analyze --provider <provider> --fund <identifier> [--from yyyy-MM-dd] [--to yyyy-MM-dd]");
    }

    private static async Task<FundWorkspace> LoadSourceWorkspaceAsync(
        AletheiaApplicationService application,
        string[] args,
        string commandName,
        CancellationToken cancellationToken)
    {
        if (args.Length is 0 || (args.Length is 1 && string.Equals(args[0], "sample", StringComparison.OrdinalIgnoreCase)))
        {
            return await application.LoadSampleWorkspaceAsync(cancellationToken).ConfigureAwait(false);
        }

        if (args.Length is 1)
        {
            return await application.LoadCsvWorkspaceAsync(args[0], cancellationToken).ConfigureAwait(false);
        }

        if (args.Length >= 4)
        {
            var providerId = ReadRequiredOption(args, "--provider");
            var fundIdentifier = ParseFundIdentifier(ReadRequiredOption(args, "--fund"));
            var from = ParseOptionalDate(args, "--from");
            var to = ParseOptionalDate(args, "--to");
            return await application.LoadProviderWorkspaceAsync(providerId, fundIdentifier, from, to, cancellationToken).ConfigureAwait(false);
        }

        throw new ArgumentException(
            $"Usage: aletheia {commandName} sample OR aletheia {commandName} <sample-fund.csv> OR " +
            $"aletheia {commandName} --provider <provider> --fund <identifier> [--from yyyy-MM-dd] [--to yyyy-MM-dd]");
    }

    private static async Task RunSimulationCommandAsync(
        AletheiaApplicationService application,
        string[] args,
        CancellationToken cancellationToken)
    {
        var request = new InvestmentSimulationRequest(
            ReadOptionalDouble(args, "--initial", 1_800d),
            ReadOptionalDouble(args, "--monthly", 100d),
            ReadOptionalInt32(args, "--years", 10),
            ReadOptionalInt32(args, "--paths", 5_000),
            ReadOptionalInt32(args, "--seed", 161803));
        var sourceArguments = RemoveSimulationOptions(args);
        var workspace = await LoadSourceWorkspaceAsync(
            application,
            sourceArguments,
            "simulate",
            cancellationToken).ConfigureAwait(false);
        var result = application.RunInvestmentSimulation(workspace, request, cancellationToken);
        WriteInvestmentSimulation(result);
    }

    private static async Task RunTimingBacktestCommandAsync(
        AletheiaApplicationService application,
        string[] args,
        CancellationToken cancellationToken)
    {
        var periods = ReadOptionalNullableDouble(args, "--periods-per-year");
        var options = new TimingBacktestOptions(
            ReadOptionalDouble(args, "--cost", 0.001d),
            ReadOptionalDouble(args, "--slippage", 0.0005d),
            ReadOptionalInt32(args, "--delay", 1),
            ReadOptionalDouble(args, "--max-exposure", 1d),
            periods,
            !HasFlag(args, "--no-initial-cost"));
        var sourceArguments = RemoveBacktestOptions(args);
        var workspace = await LoadSourceWorkspaceAsync(
            application,
            sourceArguments,
            "backtest",
            cancellationToken).ConfigureAwait(false);
        var result = application.RunTimingEconomicBacktest(workspace, options);
        WriteTimingEconomicBacktest(result);
    }

    private static async Task RunArenaAsync(
        AletheiaApplicationService application,
        FundWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var result = await application.RunModelArenaAsync(workspace, cancellationToken).ConfigureAwait(false);
        WriteArena(result, GetLedgerPath());
    }

    private static async Task RunPredictionLedgerCommandAsync(
        AletheiaApplicationService application,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (args.Length is 1 && string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
        {
            var predictions = await application.GetPredictionListAsync(20, cancellationToken).ConfigureAwait(false);
            Console.WriteLine("PREDICTION LEDGER");
            Console.WriteLine("------------------------------------------------");
            foreach (var prediction in predictions)
            {
                Console.WriteLine($"{prediction.PredictionId}  {prediction.ModelName}  {prediction.CutoffDate:yyyy-MM-dd}  {prediction.Horizon}");
            }

            return;
        }

        if (args.Length is 2 &&
            string.Equals(args[0], "show", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(args[1], out var predictionId))
        {
            var details = await application.GetPredictionDetailsAsync(predictionId, cancellationToken).ConfigureAwait(false);
            if (details is null)
            {
                Console.WriteLine("Prediction not found.");
                return;
            }

            var prediction = details.Prediction;
            Console.WriteLine("PREDICTION");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine($"Id:                           {prediction.Prediction.PredictionId}");
            Console.WriteLine($"Origin:                       {prediction.Origin}");
            Console.WriteLine($"Model:                        {prediction.Prediction.Model.Name} {prediction.Prediction.Model.Version}");
            Console.WriteLine($"Cutoff:                       {prediction.Prediction.DataCutoffDate:yyyy-MM-dd} (index {prediction.PredictionCutoffIndex.ToString(InvariantCulture)})");
            Console.WriteLine($"Target:                       {prediction.TargetDate:yyyy-MM-dd} (index {prediction.TargetIndex?.ToString(InvariantCulture) ?? "n/a"})");
            Console.WriteLine($"Content fingerprint:          {FormatFingerprint(prediction.ContentFingerprint)}");
            Console.WriteLine($"Capabilities:                 {FormatCapabilities(prediction.Prediction.ForecastCapabilities)}");
            Console.WriteLine($"Point statistic:              {prediction.Prediction.PointForecastStatistic}");
            Console.WriteLine($"Point forecast:               {FormatCapabilityPercent(prediction.Prediction, ForecastCapabilities.PointForecast, prediction.Prediction.PointForecastReturn)}");
            Console.WriteLine($"Expected return:              {FormatCapabilityPercent(prediction.Prediction, ForecastCapabilities.ExpectedReturn, prediction.Prediction.ExpectedReturn)}");
            Console.WriteLine($"Probability positive:         {FormatCapabilityPercent(prediction.Prediction, ForecastCapabilities.ProbabilityPositive, prediction.Prediction.ProbabilityPositive)}");
            foreach (var evaluation in details.Evaluations)
            {
                Console.WriteLine();
                Console.WriteLine("EVALUATION");
                Console.WriteLine($"Content fingerprint:          {FormatFingerprint(evaluation.EvaluationContentFingerprint)}");
                Console.WriteLine($"Direction rule:               {evaluation.DirectionRule}");
                Console.WriteLine($"Actual return:                {FormatPercent(evaluation.ActualReturn)}");
                Console.WriteLine($"Absolute error:               {FormatCapabilityPercent(prediction.Prediction, ForecastCapabilities.PointForecast, evaluation.AbsoluteError)}");
                Console.WriteLine($"Direction correct:            {(evaluation.DirectionCorrect ? "YES" : "NO")}");
                Console.WriteLine($"Brier contribution:           {FormatCapabilityNumber(prediction.Prediction, ForecastCapabilities.ProbabilityPositive, evaluation.BrierContribution)}");
            }

            return;
        }

        throw new ArgumentException("Usage: aletheia predictions list OR aletheia predictions show <prediction-id>");
    }

    private static void WriteInvestmentSimulation(InvestmentSimulationSummary result)
    {
        var currency = result.Dataset.Currency;
        Console.WriteLine("PERIODIC-INVESTMENT SCENARIO");
        Console.WriteLine("================================================");
        Console.WriteLine();
        Console.WriteLine($"Fund:                         {result.Dataset.FundName}");
        Console.WriteLine($"Period:                       {result.StartDate:yyyy-MM-dd} to {result.TargetDate:yyyy-MM-dd}");
        Console.WriteLine($"Initial capital:              {QuantitativeFormatter.FormatCurrency(result.Request.InitialInvestment, currency)}");
        Console.WriteLine($"Monthly contribution:         {QuantitativeFormatter.FormatCurrency(result.Request.MonthlyContribution, currency)}");
        Console.WriteLine($"Horizon:                      {result.Request.HorizonYears.ToString(InvariantCulture)} years");
        Console.WriteLine($"Paths / seed:                 {result.Request.PathCount.ToString("N0", InvariantCulture)} / {result.Request.Seed.ToString(InvariantCulture)}");
        Console.WriteLine($"Observation periods / month:  {result.ObservationPeriodsPerMonth.ToString("0.###", InvariantCulture)}");
        Console.WriteLine($"Historical mean / obs:         {QuantitativeFormatter.FormatReturn(result.HistoricalMeanLogReturnPerObservation)}");
        Console.WriteLine($"Historical std. / obs:         {QuantitativeFormatter.FormatReturn(result.HistoricalStandardDeviationPerObservation)}");
        Console.WriteLine($"Monthly mean log return:       {QuantitativeFormatter.FormatReturn(result.MonthlyMeanLogReturn)}");
        Console.WriteLine($"Monthly log-return std. dev.:  {QuantitativeFormatter.FormatReturn(result.MonthlyStandardDeviation)}");
        Console.WriteLine($"Total contributed:            {QuantitativeFormatter.FormatCurrency(result.TotalContributed, currency)}");
        Console.WriteLine($"P10 terminal value:           {QuantitativeFormatter.FormatCurrency(result.P10TerminalValue, currency)}");
        Console.WriteLine($"P25 terminal value:           {QuantitativeFormatter.FormatCurrency(result.P25TerminalValue, currency)}");
        Console.WriteLine($"Median terminal value:        {QuantitativeFormatter.FormatCurrency(result.MedianTerminalValue, currency)}");
        Console.WriteLine($"Mean terminal value:          {QuantitativeFormatter.FormatCurrency(result.MeanTerminalValue, currency)}");
        Console.WriteLine($"P75 terminal value:           {QuantitativeFormatter.FormatCurrency(result.P75TerminalValue, currency)}");
        Console.WriteLine($"P90 terminal value:           {QuantitativeFormatter.FormatCurrency(result.P90TerminalValue, currency)}");
        Console.WriteLine($"P(value below contributions): {QuantitativeFormatter.FormatPercentShort(result.ProbabilityTerminalBelowContributions)}");
        Console.WriteLine();
        Console.WriteLine("METHODOLOGY");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine(result.Methodology);
        Console.WriteLine();
        Console.WriteLine("INVESTMENT SIGNAL");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine("NO CALL");
        Console.WriteLine("Scenario distribution only; not a validated forecast or recommendation.");
    }

    private static void WriteApplicationAnalysis(FundAnalysisResult analysis)
    {
        WriteHeader(analysis.Dataset);
        if (analysis.ResearchReport is not null)
        {
            WriteReportOverview(analysis.ResearchReport);
        }

        WriteDataQuality(analysis.DataQuality);
        WritePerformance(
            analysis.Performance.Cagr,
            analysis.Performance.AnnualizedVolatility,
            analysis.Performance.MaximumDrawdown,
            analysis.Performance.SharpeRatio,
            analysis.Performance.SortinoRatio,
            analysis.Performance.Lag1Autocorrelation);
        WriteDynamics(analysis.CurrentState);
        WriteArDiagnostics(analysis.ArFit, analysis.ArForecast);
        WriteSpectralAnalysis(analysis.Spectrum, analysis.SpectralStability);
        WriteHistoricalAnalogues(
            analysis.Analogues.Search,
            analysis.Analogues.Outcome30CalendarDays,
            analysis.Analogues.Outcome90CalendarDays);
        WriteCurrentForecasts(analysis.Forecasts);
        if (analysis.ResearchReport is null)
        {
            WriteSignal();
        }
        else
        {
            WriteDecisionSignal(analysis.ResearchReport.DecisionSignal);
        }
    }

    private static void WriteResearchReport(FundResearchReport report)
    {
        WriteHeader(report.Dataset);
        WriteReportOverview(report);
        WriteFundScore(report);
        WriteStressScenarios(report);
        WriteCurrentForecasts(report.Forecasts);
        WriteEnsembleAudit(report);
        WriteDecisionSignal(report.DecisionSignal);
        Console.WriteLine("PROVENANCE");
        Console.WriteLine("------------------------------------------------");
        foreach (var pair in report.Provenance)
        {
            Console.WriteLine($"{pair.Key}: {pair.Value}");
        }

        Console.WriteLine();
    }

    private static void WriteReportOverview(FundResearchReport report)
    {
        Console.WriteLine("RESEARCH OVERVIEW");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Fund quality:                 {report.FundScore.Score:0.0} / 10 ({report.FundScore.Confidence})");
        Console.WriteLine($"Attractiveness as-of:         {report.Actionability.EffectiveDate:yyyy-MM-dd}");
        Console.WriteLine($"Strategic attractiveness:     {report.CurrentAttractiveness.Score:0.0} / 10 - {report.CurrentAttractiveness.Category} ({report.CurrentAttractiveness.Confidence})");
        Console.WriteLine($"Strategic decision signal:    {report.DecisionSignal.DisplayLabel} ({report.DecisionSignal.Qualification}, {report.DecisionSignal.Confidence})");
        Console.WriteLine($"Actionability:                {report.Actionability.Status} ({report.Actionability.Level}, {report.Actionability.Confidence})");
        Console.WriteLine($"Data freshness:               {report.DataFreshness.Status} ({report.DataFreshness.DataAgeDays.ToString(InvariantCulture)} days)");
        Console.WriteLine($"Latest effective regime:      {report.CurrentRegimeLabel ?? "n/a"}");
        Console.WriteLine($"Regime probability:           {FormatPercentShort(report.CurrentRegimeProbability)}");
        Console.WriteLine($"Maximum drawdown:             {FormatPercent(report.Performance.MaximumDrawdown.MaximumDrawdown)}");
        if (report.TwelveMonthForecast is not null)
        {
            Console.WriteLine($"12M expected return:          {FormatNullablePercent(report.TwelveMonthForecast.ExpectedReturnOrNull)}");
            Console.WriteLine($"12M P(positive):              {FormatNullablePercent(report.TwelveMonthForecast.ProbabilityPositiveOrNull)}");
        }
        else
        {
            Console.WriteLine("12M forecast:                 n/a");
        }

        Console.WriteLine($"Ensemble ReliabilityIndex:    {FormatPercentShort(report.Ensemble?.Reliability)}");
        if (report.Warnings.Count > 0)
        {
            Console.WriteLine($"Warnings:                     {report.Warnings.Count.ToString(InvariantCulture)}");
        }

        Console.WriteLine();
    }

    private static void WriteFundScore(FundResearchReport report)
    {
        Console.WriteLine("FUND SCORE");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Overall:                      {report.FundScore.Score:0.0} / 10");
        Console.WriteLine($"Confidence:                   {report.FundScore.Confidence}");
        foreach (var component in report.FundScore.Components)
        {
            Console.WriteLine($"{Truncate(component.Name, 30),-30} {component.Score,5:0.0}   weight {component.Weight,5:0.00}");
        }

        Console.WriteLine();
        Console.WriteLine("WHY");
        foreach (var reason in report.FundScore.Reasons)
        {
            Console.WriteLine($"* {reason}");
        }

        foreach (var warning in report.FundScore.Warnings.Take(5))
        {
            Console.WriteLine($"- {warning}");
        }

        Console.WriteLine();
    }

    private static void WriteStressScenarios(FundResearchReport report)
    {
        Console.WriteLine("STRESS SCENARIOS");
        Console.WriteLine("------------------------------------------------");
        if (report.StressScenarios.Count == 0)
        {
            Console.WriteLine("No stress scenarios available.");
            Console.WriteLine();
            return;
        }

        foreach (var scenario in report.StressScenarios)
        {
            Console.WriteLine($"{scenario.Name}");
            Console.WriteLine($"Peak loss:                    {FormatPercent(scenario.PeakLoss)}");
            Console.WriteLine($"Terminal return:              {FormatPercent(scenario.TerminalReturn)}");
            Console.WriteLine($"Window:                       {FormatDate(scenario.StartDate)} -> {FormatDate(scenario.EndDate)} ({scenario.WindowLengthObservations?.ToString(InvariantCulture) ?? "n/a"} obs)");
            Console.WriteLine($"Selection criterion:          {scenario.SelectionCriterion ?? "n/a"}");
            Console.WriteLine($"Diagnostic:                   {scenario.Diagnostic}");
        }

        Console.WriteLine();
    }

    private static void WriteDecisionSignal(DecisionSignal signal)
    {
        Console.WriteLine("SIGNAL");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"{signal.DisplayLabel}");
        Console.WriteLine($"Direction:                    {signal.Direction}");
        Console.WriteLine($"Qualification:                {signal.Qualification}");
        Console.WriteLine($"Directional strength:         {FormatPercent(signal.DirectionalStrength)}");
        Console.WriteLine($"Validation strength:          {FormatPercent(signal.ValidationStrength)}");
        Console.WriteLine($"Legacy action:                {signal.Action}");
        Console.WriteLine($"Confidence:                   {signal.Confidence}");
        foreach (var item in signal.Reasons)
        {
            Console.WriteLine($"= {item}");
        }

        foreach (var item in signal.Evidence)
        {
            Console.WriteLine($"* {item}");
        }

        foreach (var item in signal.CounterEvidence)
        {
            Console.WriteLine($"- {item}");
        }

        foreach (var warning in signal.Warnings.Take(3))
        {
            Console.WriteLine($"! {warning}");
        }

        Console.WriteLine();
    }

    private static void WriteEnsembleAudit(FundResearchReport report)
    {
        Console.WriteLine("ENSEMBLE AUDIT");
        Console.WriteLine("------------------------------------------------");
        if (report.EnsembleAudit.Count == 0)
        {
            Console.WriteLine("No ensemble audit entries available.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("Model                         Status           Rank   Weight   Validation   Reason");
        foreach (var entry in report.EnsembleAudit)
        {
            Console.WriteLine(
                $"{Truncate(entry.Model.Name, 28),-28} {Truncate(entry.ValidationStatus, 15),-15} {entry.ArenaRank?.ToString(InvariantCulture) ?? "n/a",4} {FormatPercentShort(entry.EnsembleWeight),8} {FormatPercentShort(entry.ValidationScore),12}   {Truncate(entry.ExclusionReason, 48)}");
        }

        Console.WriteLine();
    }

    private static void WriteFundSearchResults(IReadOnlyList<FundSearchResultSummary> results)
    {
        Console.WriteLine("FUND SEARCH");
        Console.WriteLine("------------------------------------------------------------");
        if (results.Count == 0)
        {
            Console.WriteLine("No funds found.");
            return;
        }

        Console.WriteLine("Provider    ISIN          History   Latest       Fund");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var result in results)
        {
            Console.WriteLine(
                $"{Truncate(result.ProviderId, 10),-10}  {result.Isin ?? "n/a",-12}  {FormatYesNo(result.HasHistoricalData),7}   {FormatDate(result.LatestAvailableObservation),10}   {Truncate(result.FundName, 48)}");
            if (!string.IsNullOrWhiteSpace(result.ManagementCompany))
            {
                Console.WriteLine($"            Manager: {result.ManagementCompany}");
            }

            Console.WriteLine($"            Source:  {result.SourceAuthority}");
        }
    }

    private static void WriteMarketTiming(MarketTimingAssessment timing)
    {
        Console.WriteLine("MARKET TIMING");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Training cutoff:              {timing.TrainingCutoff:yyyy-MM-dd}");
        Console.WriteLine($"Current zone:                 {timing.CurrentTimingZone}");
        Console.WriteLine($"Timing signal:                {timing.Decision.DisplayLabel} ({timing.Decision.Qualification}, {timing.Decision.Confidence})");
        Console.WriteLine($"Timing direction:             {timing.Decision.Direction}");
        Console.WriteLine($"Primary horizon:              {timing.Decision.PrimaryHorizon?.ToString() ?? "n/a"}");
        Console.WriteLine($"Signal strength:              {FormatPercentShort(timing.Decision.Strength)}");
        Console.WriteLine($"Validation strength:          {FormatPercentShort(timing.Decision.ValidationStrength)}");
        var primary = timing.Decision.PrimaryHorizon is null
            ? timing.Horizons.FirstOrDefault()
            : timing.Horizons.FirstOrDefault(item => item.Horizon.Equals(timing.Decision.PrimaryHorizon)) ?? timing.Horizons.FirstOrDefault();
        Console.WriteLine($"Forecast expected return:     {FormatPercentShort(primary?.ForecastExpectedReturn)}");
        Console.WriteLine($"Expected barrier payoff:      {FormatPercent(timing.Decision.ExpectedPayoff)}");
        var primaryArena = FindPrimaryTimingArena(timing);
        if (primaryArena is not null)
        {
            Console.WriteLine($"OOD status:                   {primaryArena.OutOfDistribution.Level}");
            Console.WriteLine($"OOD robust distance:          {FormatNumber(primaryArena.OutOfDistribution.RobustDistance)} / {FormatNumber(primaryArena.OutOfDistribution.Threshold)}");
        }

        Console.WriteLine($"Primary horizon reason:       {timing.PrimaryHorizonSelectionReason}");
        Console.WriteLine(timing.Narrative.Summary);
        Console.WriteLine(timing.Narrative.DirectionExplanation);
        Console.WriteLine(timing.Narrative.TimingExplanation);
        foreach (var reason in timing.Decision.Reasons.Take(4))
        {
            Console.WriteLine($"= {reason}");
        }

        Console.WriteLine();
        Console.WriteLine("HORIZONS");
        Console.WriteLine("Horizon                  P Up     P Down   P Neutral  RelIndex     Evidence");
        foreach (var horizon in timing.Horizons)
        {
            Console.WriteLine(
                $"{horizon.Horizon,-22} {FormatPercentShort(horizon.ProbabilityUp),8} {FormatPercentShort(horizon.ProbabilityDown),8} {FormatPercentShort(horizon.ProbabilityNeutral),10} {FormatPercentShort(horizon.ReliabilityIndex),11}  {horizon.EvidenceStrength}");
        }

        Console.WriteLine("ReliabilityIndex is a validation-quality index, not a probability of a correct call.");

        Console.WriteLine();
        Console.WriteLine("WHY");
        foreach (var item in timing.Evidence.Take(6))
        {
            Console.WriteLine($"* {item}");
        }

        foreach (var item in timing.CounterEvidence.Take(6))
        {
            Console.WriteLine($"- {item}");
        }

        if (timing.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("WARNINGS");
            foreach (var warning in timing.Warnings.Take(6))
            {
                Console.WriteLine($"! {warning}");
            }
        }

        Console.WriteLine();
    }

    private static void WriteTimingEconomicBacktest(TimingEconomicBacktestAssessment backtest)
    {
        Console.WriteLine("TIMING ECONOMIC BACKTEST");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Status:                       {backtest.Status}");
        Console.WriteLine($"Horizon:                      {backtest.Horizon?.ToString() ?? "n/a"}");
        Console.WriteLine($"Signals:                      {backtest.SignalCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Signal period:                {FormatDate(backtest.FirstSignalDate)} -> {FormatDate(backtest.LastSignalDate)}");
        Console.WriteLine($"Execution delay:              {backtest.ExecutionDelayObservations.ToString(InvariantCulture)} observation(s)");
        Console.WriteLine($"Cost / slippage:              {FormatPercentShort(backtest.TransactionCostRate)} / {FormatPercentShort(backtest.SlippageRate)}");
        Console.WriteLine($"Diagnostic:                   {backtest.Diagnostic}");
        Console.WriteLine("A good Brier score does not guarantee economic profitability.");
        Console.WriteLine();
        if (!backtest.IsReliable)
        {
            Console.WriteLine("NO RELIABLE ECONOMIC BACKTEST");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("RESULTS");
        Console.WriteLine("Strategy              CumRet    AnnRet      Vol   Sharpe  Sortino    MaxDD   Calmar Turnover  InMkt Trades");
        foreach (var result in backtest.Results)
        {
            Console.WriteLine(
                $"{Truncate(result.StrategyName, 20),-20} {FormatPercentShort(result.CumulativeReturn),7} {FormatPercentShort(result.AnnualizedReturn),9} {FormatPercentShort(result.AnnualizedVolatility),8} {FormatNumber(result.Sharpe),7} {FormatNumber(result.Sortino),8} {FormatPercentShort(result.MaximumDrawdown),8} {FormatNumber(result.Calmar),8} {FormatNumber(result.Turnover),8} {FormatPercentShort(result.TimeInMarket),6} {result.TradeCount,6}");
        }

        Console.WriteLine();
        Console.WriteLine("TEMPORAL SEMANTICS");
        Console.WriteLine("Signal date is the OOS calculation/decision date; execution occurs after the configured NAV-observation delay.");
        Console.WriteLine(backtest.Results.FirstOrDefault()?.AnnualizationMethod ?? "Annualization method unavailable.");
        Console.WriteLine();
        Console.WriteLine("SIGNAL TRACE");
        Console.WriteLine("Decision     Execution    Zone                      Reliability  Exposure");
        foreach (var signal in backtest.SignalTrace.Take(12))
        {
            Console.WriteLine(
                $"{signal.DecisionDate:yyyy-MM-dd}   {signal.ExecutionDate:yyyy-MM-dd}   {Truncate(signal.Zone, 24),-24} {FormatPercentShort(signal.Reliability),11} {FormatPercentShort(signal.TargetExposure),9}");
        }

        Console.WriteLine();
    }

    private static string ReadRequiredOption(string[] args, string optionName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        throw new ArgumentException($"Missing required option {optionName}.");
    }

    private static double ReadOptionalDouble(string[] args, string optionName, double defaultValue)
    {
        var value = ReadOptionalOption(args, optionName);
        if (value is null)
        {
            return defaultValue;
        }

        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Option {optionName} requires a numeric value.");
        }

        return parsed;
    }

    private static double? ReadOptionalNullableDouble(string[] args, string optionName)
    {
        var value = ReadOptionalOption(args, optionName);
        if (value is null)
        {
            return null;
        }

        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Option {optionName} requires a numeric value.");
        }

        return parsed;
    }

    private static int ReadOptionalInt32(string[] args, string optionName, int defaultValue)
    {
        var value = ReadOptionalOption(args, optionName);
        if (value is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"Option {optionName} requires an integer value.");
        }

        return parsed;
    }

    private static string? ReadOptionalOption(string[] args, string optionName)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index == args.Length - 1)
            {
                throw new ArgumentException($"Option {optionName} requires a value.");
            }

            return args[index + 1];
        }

        return null;
    }

    private static bool HasFlag(string[] args, string optionName)
    {
        return args.Any(value => string.Equals(value, optionName, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] RemoveSimulationOptions(string[] args)
    {
        var sourceArguments = new List<string>(args.Length);
        for (var index = 0; index < args.Length; index++)
        {
            if (!IsSimulationOption(args[index]))
            {
                sourceArguments.Add(args[index]);
                continue;
            }

            if (index == args.Length - 1)
            {
                throw new ArgumentException($"Option {args[index]} requires a value.");
            }

            index++;
        }

        return sourceArguments.ToArray();
    }

    private static string[] RemoveBacktestOptions(string[] args)
    {
        var sourceArguments = new List<string>(args.Length);
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--no-initial-cost", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsBacktestOption(args[index]))
            {
                sourceArguments.Add(args[index]);
                continue;
            }

            if (index == args.Length - 1)
            {
                throw new ArgumentException($"Option {args[index]} requires a value.");
            }

            index++;
        }

        return sourceArguments.ToArray();
    }

    private static bool IsSimulationOption(string value)
    {
        return string.Equals(value, "--initial", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--monthly", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--years", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--paths", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--seed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBacktestOption(string value)
    {
        return string.Equals(value, "--cost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--slippage", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--delay", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--max-exposure", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "--periods-per-year", StringComparison.OrdinalIgnoreCase);
    }

    private static DateOnly? ParseOptionalDate(string[] args, string optionName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return DateOnly.ParseExact(args[index + 1], "yyyy-MM-dd", InvariantCulture);
            }
        }

        return null;
    }

    private static FundIdentifier ParseFundIdentifier(string value)
    {
        return Isin.IsValid(value)
            ? new Isin(value).ToFundIdentifier()
            : new FundIdentifier(FundIdentifierKind.Provider, value);
    }

    private static string GetLedgerPath()
    {
        var configured = Environment.GetEnvironmentVariable("ALETHEIA_LEDGER_PATH");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.CurrentDirectory, "data", "aletheia.db")
            : configured;
    }

    private static void WriteHeader(DatasetSummary dataset)
    {
        var sourceObservationCount = dataset.SourceObservationCount == 0
            ? dataset.ObservationCount
            : dataset.SourceObservationCount;
        var sourceStartDate = dataset.SourceStartDate ?? dataset.StartDate;
        var sourceEndDate = dataset.SourceEndDate ?? dataset.EndDate;

        Console.WriteLine("ALETHEIA FUND ANALYSIS");
        Console.WriteLine("================================================");
        Console.WriteLine();
        Console.WriteLine($"Fund:                         {dataset.FundName}");
        Console.WriteLine($"Effective observations:       {dataset.ObservationCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Source observations:          {sourceObservationCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Synthetic/carry-forward rows: {dataset.SyntheticObservationCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Effective frequency:          {dataset.ObservationFrequency}");
        Console.WriteLine($"Effective period:             {dataset.StartDate:yyyy-MM-dd} to {dataset.EndDate:yyyy-MM-dd}");
        Console.WriteLine($"Source period:                {sourceStartDate:yyyy-MM-dd} to {sourceEndDate:yyyy-MM-dd}");
        Console.WriteLine($"Effective policy:             {dataset.EffectiveObservationPolicy}");
        if (dataset.Freshness is not null)
        {
            Console.WriteLine($"Data freshness:               {dataset.Freshness.Status} ({dataset.Freshness.DataAgeDays.ToString(InvariantCulture)} days)");
            Console.WriteLine($"Freshness diagnostic:         {dataset.Freshness.Diagnostic}");
        }

        Console.WriteLine($"Dataset fingerprint:          {FormatFingerprint(dataset.DatasetFingerprint)}");
        if (dataset.Provenance is not null)
        {
            Console.WriteLine($"Provider:                     {dataset.Provenance.ProviderDisplayName}");
            Console.WriteLine($"ISIN:                         {dataset.Provenance.Isin ?? "n/a"}");
            Console.WriteLine($"Source reference:             {dataset.Provenance.SourceReference ?? "n/a"}");
            Console.WriteLine($"Cache:                        {(dataset.Provenance.IsFromCache ? "cached provider payload" : "fresh/local provider payload")}");
            Console.WriteLine($"Original / normalized obs:    {dataset.Provenance.OriginalObservationCount.ToString(InvariantCulture)} / {dataset.Provenance.NormalizedObservationCount.ToString(InvariantCulture)}");
        }

        Console.WriteLine();
    }

    private static void WriteDataQuality(DataQualityReport quality)
    {
        Console.WriteLine("DATA QUALITY");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Quality score:                {quality.QualityScore.ToString(InvariantCulture)} / 100");
        Console.WriteLine($"Missing business days:        {quality.MissingBusinessDayCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Large gaps:                   {quality.LargeGapCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Suspicious jumps:             {quality.SuspiciousJumpCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Coverage:                     {FormatPercent(quality.CoverageRatio)}");
        Console.WriteLine();
    }

    private static void WritePerformance(
        double cagr,
        double annualVolatility,
        DrawdownResult drawdown,
        double sharpe,
        double sortino,
        double autocorrelation)
    {
        Console.WriteLine("PERFORMANCE");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"CAGR:                         {FormatPercent(cagr)}");
        Console.WriteLine($"Annual volatility:            {FormatPercent(annualVolatility)}");
        Console.WriteLine($"Maximum drawdown:             {FormatPercent(drawdown.MaximumDrawdown)}");
        Console.WriteLine($"Drawdown duration:            {drawdown.DurationDays.ToString(InvariantCulture)} days");
        Console.WriteLine($"Sharpe ratio:                 {FormatNumber(sharpe)}");
        Console.WriteLine($"Sortino ratio:                {FormatNumber(sortino)}");
        Console.WriteLine($"Lag-1 autocorrelation:        {FormatNumber(autocorrelation)}");
        Console.WriteLine();
    }

    private static void WriteDynamics(DynamicState state)
    {
        Console.WriteLine("DYNAMICS");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"State date:                   {state.Date:yyyy-MM-dd}");
        Console.WriteLine($"State data adequacy:          {FormatPercent(state.DataAdequacy)}");
        Console.WriteLine($"State schema fingerprint:     {FormatFingerprint(state.Schema?.Fingerprint)}");
        Console.WriteLine($"Trend:                        {FormatNumber(GetDimension(state, StandardStateDimensions.Trend))}");
        Console.WriteLine($"Simple return:                {FormatPercent(GetDimension(state, StandardStateDimensions.SimpleReturn))}");
        Console.WriteLine($"Log return:                   {FormatNumber(GetDimension(state, StandardStateDimensions.LogReturn))}");
        Console.WriteLine($"Momentum:                     {FormatPercent(GetDimension(state, StandardStateDimensions.Momentum))}");
        Console.WriteLine($"Current drawdown:             {FormatPercent(GetDimension(state, StandardStateDimensions.Drawdown))}");
        Console.WriteLine($"Log-NAV velocity/obs:         {FormatNumber(GetDimension(state, StandardStateDimensions.LogNavVelocityPerObservation))}");
        Console.WriteLine($"Log-NAV accel/obs^2:          {FormatNumber(GetDimension(state, StandardStateDimensions.LogNavAccelerationPerObservationSquared))}");
        Console.WriteLine();
    }

    private static void WriteArDiagnostics(DynamicModelResult arFit, DynamicForecast forecast)
    {
        Console.WriteLine("AR(1) LOG-RETURN BASELINE");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"AR(1) phi:                    {FormatNumber(arFit.Parameters["Phi"])}");
        Console.WriteLine($"Intercept:                    {FormatNumber(arFit.Parameters["Intercept"])}");
        Console.WriteLine($"Innovation variance:          {FormatNumber(arFit.InnovationVariance)}");
        Console.WriteLine($"Stationary:                   {(arFit.IsStationary ? "YES" : "NO")}");
        Console.WriteLine($"Forecast horizon:             {forecast.Horizon}");
        Console.WriteLine($"Expected log return:          {FormatNumber(forecast.CumulativeExpectedLogReturn)}");
        Console.WriteLine($"Median simple return:         {FormatPercent(forecast.MedianSimpleReturn)}");
        Console.WriteLine($"Expected simple return:       {FormatPercent(forecast.ExpectedSimpleReturn)}");
        Console.WriteLine();
    }

    private static void WriteSpectralAnalysis(
        SpectralAnalysisResult spectrum,
        RollingSpectralStabilityResult stability)
    {
        Console.WriteLine("SPECTRAL ANALYSIS");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Dominant period:              {FormatObservations(spectrum.DominantFrequency?.PeriodObservations)}");
        Console.WriteLine($"Dominant amplitude:           {FormatNumber(spectrum.DominantFrequency?.Amplitude)}");
        Console.WriteLine($"Peak concentration:           {FormatNumber(spectrum.PeakPowerFraction)}");
        Console.WriteLine($"Peak/background ratio:        {FormatNumber(spectrum.PeakToBackgroundRatio)}");
        Console.WriteLine($"Diagnostic strength:          {spectrum.DiagnosticStrength}");
        Console.WriteLine($"Signal samples:               {spectrum.OriginalSampleCount.ToString(InvariantCulture)}");
        Console.WriteLine($"FFT length:                   {spectrum.TransformLength.ToString(InvariantCulture)}");
        Console.WriteLine($"Window:                       {spectrum.Options.Window}");
        Console.WriteLine($"Coherent gain:                {FormatNumber(spectrum.CoherentGain)}");
        Console.WriteLine($"Zero padding applied:         {(spectrum.ZeroPaddingApplied ? "YES" : "NO")}");
        Console.WriteLine($"Rolling persistence:          {FormatNumber(stability.DominantPeriodPersistence)}");
        Console.WriteLine($"Window detection rate:        {FormatNumber(stability.WindowDetectionRate)}");
        Console.WriteLine();
    }

    private static void WriteHistoricalAnalogues(
        HistoricalAnalogueSearchResult search,
        AnalogueOutcomeSummary outcome30,
        AnalogueOutcomeSummary outcome90)
    {
        Console.WriteLine("HISTORICAL ANALOGUES");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"Candidate states:             {search.CandidateCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Schema-compatible:            {search.SchemaCompatibleCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Rejected incompatible:        {search.RejectedSchemaIncompatibleCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Rejected missing dimensions:  {search.RejectedMissingDimensionCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Matches:                      {search.Matches.Count.ToString(InvariantCulture)}");
        Console.WriteLine($"P(+30d return):               {FormatPercent(outcome30.ProbabilityPositive)}");
        Console.WriteLine($"Median +30d return:           {FormatPercent(outcome30.MedianReturn)}");
        Console.WriteLine($"P(+90d return):               {FormatPercent(outcome90.ProbabilityPositive)}");
        Console.WriteLine($"Median +90d return:           {FormatPercent(outcome90.MedianReturn)}");
        Console.WriteLine();
    }

    private static void WriteCurrentForecasts(ForecastCollectionResult forecasts)
    {
        Console.WriteLine("CURRENT FORECASTS");
        Console.WriteLine("------------------------------------------------");
        foreach (var run in forecasts.Runs.Where(item => item.RequestedHorizon.Unit == ForecastHorizonUnit.CalendarDays))
        {
            Console.WriteLine($"{run.Model.Name} / {run.RequestedHorizon}");
            if (run.Distribution is null)
            {
                Console.WriteLine($"Status:                       {run.Status}");
                Console.WriteLine($"Reason:                       {run.FailureReason ?? "n/a"}");
                continue;
            }

            Console.WriteLine($"Point statistic:              {run.PointForecastStatistic}");
            Console.WriteLine($"Point forecast:               {FormatCapabilityPercent(run.Distribution, ForecastCapabilities.PointForecast, run.Distribution.PointForecastReturn)}");
            Console.WriteLine($"Expected return:              {FormatCapabilityPercent(run.Distribution, ForecastCapabilities.ExpectedReturn, run.Distribution.ExpectedReturn)}");
            Console.WriteLine($"Probability positive:         {FormatCapabilityPercent(run.Distribution, ForecastCapabilities.ProbabilityPositive, run.Distribution.ProbabilityPositive)}");
            Console.WriteLine();
        }
    }

    private static void WriteArena(ModelArenaResult result, string ledgerPath)
    {
        Console.WriteLine("ALETHEIA MODEL ARENA");
        Console.WriteLine("============================================================");
        Console.WriteLine();
        Console.WriteLine($"Fund:                         {result.Dataset.History.Fund.Name}");
        Console.WriteLine($"Evaluation period:            {FormatDate(result.EvaluationStartDate)} -> {FormatDate(result.EvaluationEndDate)}");
        Console.WriteLine($"Forecast horizon:             {result.Horizon}");
        Console.WriteLine($"Ledger:                       {ledgerPath}");
        Console.WriteLine($"Models evaluated:             {result.Models.Count.ToString(InvariantCulture)}");
        Console.WriteLine($"Point common-support events:  {result.PointCommonSupportEventCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Probability common-support:   {result.ProbabilityCommonSupportEventCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Quantile common-support:      {result.QuantileCommonSupportEventCount.ToString(InvariantCulture)}");
        Console.WriteLine($"Point baseline:               {result.PointForecastBaseline?.Name ?? "N/A"}");
        Console.WriteLine($"Probability baseline:         {result.ProbabilityBaseline?.Name ?? "N/A"}");
        Console.WriteLine();
        if (result.BaselineDiagnostics.Count > 0)
        {
            Console.WriteLine("BASELINE DIAGNOSTICS");
            Console.WriteLine("------------------------------------------------------------");
            foreach (var diagnostic in result.BaselineDiagnostics)
            {
                Console.WriteLine(diagnostic);
            }

            Console.WriteLine();
        }

        Console.WriteLine("EVALUATION COVERAGE");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Model                         Success / Eligible   Failed   Coverage");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {model.Coverage.SuccessfulForecasts,4} / {model.Coverage.EligibleEvents,-8} {model.Coverage.FailedForecasts,6}   {FormatPercentShort(model.Coverage.CoverageRatio),8}");
        }

        Console.WriteLine();
        Console.WriteLine("FORECAST CAPABILITIES");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Model                         Point   Prob    Quantiles   Point statistic");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {FormatYesNo(model.Capabilities.HasFlag(ForecastCapabilities.PointForecast)),5}   {FormatYesNo(model.Capabilities.HasFlag(ForecastCapabilities.ProbabilityPositive)),5}   {FormatYesNo(model.Capabilities.HasFlag(ForecastCapabilities.Quantiles)),9}   {model.PointForecastStatistic}");
        }

        Console.WriteLine();
        Console.WriteLine("COMMON-SUPPORT POINT FORECAST METRICS");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Events: {result.CommonSupportEventCount.ToString(InvariantCulture)}");
        Console.WriteLine("Model                         N     MAE      RMSE     DirAcc");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            var metrics = model.PointCommonSupportMetrics;
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {metrics.Point.SampleCount,4}  {FormatPercentShort(metrics.Point.MeanAbsoluteError),7}  {FormatPercentShort(metrics.Point.RootMeanSquaredError),7}  {FormatPercentShort(metrics.Point.DirectionalAccuracy),7}");
        }

        Console.WriteLine();
        Console.WriteLine("PROBABILITY FORECAST METRICS");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Model                         N     Brier     ECE");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            var metrics = model.ProbabilityCommonSupportMetrics;
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {metrics.Probability.SampleCount,4}  {FormatNumberOrDash(metrics.Probability.BrierScore),8}  {FormatNumberOrDash(metrics.Probability.ExpectedCalibrationError),7}");
        }

        Console.WriteLine();
        Console.WriteLine("QUANTILE FORECAST METRICS");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Model                         N     Pinball p50   Coverage");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            var metrics = model.QuantileCommonSupportMetrics;
            var p50 = metrics.Quantile.MeanPinballLossByPercentile.GetValueOrDefault(50);
            var p50Value = metrics.Quantile.MeanPinballLossByPercentile.ContainsKey(50) ? p50 : (double?)null;
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {metrics.IntervalCoverage.SampleCount,4}  {FormatNumberOrDash(p50Value),12}  {FormatPercentShort(metrics.IntervalCoverage.ObservedCoverage),8}");
        }

        Console.WriteLine();
        Console.WriteLine("NON-OVERLAPPING SAMPLES");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("Model                         N     MAE      DirAcc");
        Console.WriteLine("------------------------------------------------------------");
        foreach (var model in result.Models)
        {
            var metrics = model.Evaluation.NonOverlappingMetrics;
            Console.WriteLine(
                $"{Truncate(model.Model.Name, 28),-28} {metrics.Point.SampleCount,4}  {FormatPercentShort(metrics.Point.MeanAbsoluteError),7}  {FormatPercentShort(metrics.Point.DirectionalAccuracy),7}");
        }

        var calibrationModel = result.Models.FirstOrDefault(item =>
            item.ProbabilityCommonSupportMetrics.Probability.Status == MetricStatus.Available);
        if (calibrationModel is not null)
        {
            Console.WriteLine();
            Console.WriteLine("PROBABILITY CALIBRATION");
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine($"Model: {calibrationModel.Model.Name}");
            Console.WriteLine("Predicted       Observed        Samples");
            foreach (var bin in calibrationModel.ProbabilityCommonSupportMetrics.Probability.CalibrationBins)
            {
                Console.WriteLine(
                    $"{FormatRatioBound(bin.LowerBoundInclusive)}-{FormatRatioBound(bin.UpperBoundInclusive)}       {FormatNullableRatio(bin.ObservedPositiveFrequency),8}        {bin.SampleCount.ToString(InvariantCulture),7}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("RANKING POLICY");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine(result.RankingDiagnostic);
        Console.WriteLine("Eligible point models are sorted by minimum common-support MAE, then RMSE, then maximum directional accuracy.");
        foreach (var entry in result.Ranking)
        {
            Console.WriteLine($"{entry.Rank.ToString(InvariantCulture)}. {entry.Model.Name}: {entry.Reason}");
        }

        if (result.Ranking.Count == 0)
        {
            Console.WriteLine("No ranking entries available.");
        }

        Console.WriteLine();
        Console.WriteLine("INVESTMENT SIGNAL");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("NO CALL");
        Console.WriteLine();
        Console.WriteLine("Model Arena evaluates predictive performance; decision labels");
        Console.WriteLine("are emitted by the research report after synthesis.");
    }

    private static void WriteSignal()
    {
        Console.WriteLine("SIGNAL");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine("NO CALL");
        Console.WriteLine();
        Console.WriteLine("Aletheia does not yet have sufficient validated");
        Console.WriteLine("evidence to generate an investment signal.");
    }

    private static double GetDimension(DynamicState state, StateDimension dimension)
    {
        return state.TryGetValue(dimension, out var value) ? value : 0d;
    }

    private static MarketTimingArenaResult? FindPrimaryTimingArena(MarketTimingAssessment timing)
    {
        var primary = timing.Decision.PrimaryHorizon;
        if (primary is not null)
        {
            var match = timing.ModelArenaResults.FirstOrDefault(result => result.Definition.Horizon.Equals(primary));
            if (match is not null)
            {
                return match;
            }
        }

        return timing.ModelArenaResults.FirstOrDefault();
    }

    private static string FormatPercent(double value)
    {
        return value.ToString("P2", InvariantCulture);
    }

    private static string FormatNullablePercent(double? value)
    {
        return value.HasValue ? FormatPercent(value.Value) : "N/A";
    }

    private static string FormatPercentShort(double? value)
    {
        return value.HasValue ? value.Value.ToString("P1", InvariantCulture) : "N/A";
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.0000", InvariantCulture);
    }

    private static string FormatNumber(double? value)
    {
        return value.HasValue ? FormatNumber(value.Value) : "n/a";
    }

    private static string FormatNumberOrDash(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.000", InvariantCulture) : "N/A";
    }

    private static string FormatNullableRatio(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.00", InvariantCulture) : "N/A";
    }

    private static string FormatCapabilityPercent(
        PredictionRecord prediction,
        ForecastCapabilities capability,
        double value)
    {
        return prediction.Supports(capability) ? FormatPercent(value) : "N/A";
    }

    private static string FormatCapabilityPercent(
        ForecastDistribution distribution,
        ForecastCapabilities capability,
        double value)
    {
        return distribution.Capabilities.HasFlag(capability) ? FormatPercent(value) : "N/A";
    }

    private static string FormatCapabilityNumber(
        PredictionRecord prediction,
        ForecastCapabilities capability,
        double value)
    {
        return prediction.Supports(capability) ? FormatNumber(value) : "N/A";
    }

    private static string FormatCapabilities(ForecastCapabilities capabilities)
    {
        return capabilities == ForecastCapabilities.None ? "None" : capabilities.ToString();
    }

    private static string FormatYesNo(bool value)
    {
        return value ? "YES" : "NO";
    }

    private static string FormatRatioBound(double value)
    {
        return value.ToString("0.0", InvariantCulture);
    }

    private static string FormatDate(DateOnly? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd", InvariantCulture) : "n/a";
    }

    private static string Truncate(string value, int length)
    {
        return value.Length <= length ? value : value[..length];
    }

    private static string FormatObservations(double? value)
    {
        return value.HasValue
            ? $"{value.Value.ToString("0.0", InvariantCulture)} observations"
            : "n/a";
    }

    private static string FormatFingerprint(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "n/a"
            : $"{value[..Math.Min(12, value.Length)]}...";
    }
}
