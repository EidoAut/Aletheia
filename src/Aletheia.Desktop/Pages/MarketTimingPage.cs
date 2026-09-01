#pragma warning disable SA1204 // Formatting helpers are grouped after the page population workflow.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Validation;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays probabilistic market-timing diagnostics.
/// </summary>
internal sealed partial class MarketTimingPage : WorkspacePageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketTimingPage"/> class.
    /// </summary>
    public MarketTimingPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Market Timing";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        var timing = workspace?.Analysis.MarketTiming;
        if (timing is null)
        {
            this.metrics.SetMetrics([
                ("Market timing", "NOT AVAILABLE", ThemePalette.Warning),
                ("Next action", "LOAD DATASET", ThemePalette.Accent),
            ]);
            GridFactory.SetNameValueRows(this.summaryCard.Grid, [("Timing", "No assessment has been generated for this dataset.")]);
            GridFactory.SetNameValueRows(this.horizonsCard.Grid, [("Horizons", "N/A")]);
            GridFactory.SetNameValueRows(this.whyCard.Grid, [("Evidence", "N/A")]);
            GridFactory.SetNameValueRows(this.advancedCard.Grid, [("Diagnostics", "N/A")]);
            GridFactory.SetNameValueRows(this.economicCard.Grid, [("Economic backtest", "N/A")]);
            return;
        }

        var primary = ResolvePrimary(timing);
        this.metrics.SetMetrics([
            ("Zone", FormatZone(timing.CurrentTimingZone), ResolveZoneColor(timing.CurrentTimingZone)),
            ("Investor action", timing.Decision.DisplayLabel, ResolveActionColor(timing.Decision)),
            ("Timing window", primary?.Horizon.ToString() ?? "N/A", ThemePalette.AccentSecondary),
            ("P(up first)", FormatPercent(primary?.ProbabilityUp), ThemePalette.Positive),
            ("P(down first)", FormatPercent(primary?.ProbabilityDown), ThemePalette.Negative),
            ("ReliabilityIndex", FormatPercent(primary?.ReliabilityIndex), ResolveReliabilityColor(primary?.ReliabilityIndex)),
            ("Economic BT", timing.EconomicBacktest?.Status ?? "N/A", timing.EconomicBacktest?.IsReliable == true ? ThemePalette.Positive : ThemePalette.Warning),
        ]);
        this.FillSummary(timing, primary);
        this.FillHorizons(timing);
        this.FillWhy(timing);
        this.FillAdvanced(timing);
        this.FillEconomic(timing);
    }

    private static MarketTimingHorizonAssessment? ResolvePrimary(MarketTimingAssessment timing)
    {
        return timing.Decision.PrimaryHorizon is null
            ? timing.Horizons.FirstOrDefault()
            : timing.Horizons.FirstOrDefault(item => item.Horizon.Equals(timing.Decision.PrimaryHorizon)) ?? timing.Horizons.FirstOrDefault();
    }

    private void FillSummary(MarketTimingAssessment timing, MarketTimingHorizonAssessment? primary)
    {
        GridFactory.SetNameValueRows(this.summaryCard.Grid, [
            ("ACTION GUIDE", string.Empty),
            ("Current zone", FormatZone(timing.CurrentTimingZone)),
            ("Investor action", timing.Decision.DisplayLabel),
            ("Direction", timing.Decision.Direction.ToString()),
            ("Qualification", timing.Decision.Qualification.ToString()),
            ("Signal strength", FormatPercent(timing.Decision.Strength)),
            ("Validation strength", FormatPercent(timing.Decision.ValidationStrength)),
            ("Decision probability", FormatPercent(timing.Decision.Probability)),
            ("Confidence", timing.Decision.Confidence.ToString()),
            ("Forecast expected return", QuantitativeFormatter.FormatReturn(primary?.ForecastExpectedReturn)),
            ("Expected barrier payoff", QuantitativeFormatter.FormatReturn(primary?.ExpectedBarrierPayoff)),
            ("Risk-adjusted utility", QuantitativeFormatter.FormatReturn(timing.Decision.RiskAdjustedUtility)),
            ("TIMING WINDOW", string.Empty),
            ("Window", primary?.Horizon.ToString() ?? "N/A"),
            ("Why this window", timing.PrimaryHorizonSelectionReason),
            ("P(up before down)", FormatPercent(primary?.ProbabilityUpBeforeDown)),
            ("P(down before up)", FormatPercent(primary?.ProbabilityDownBeforeUp)),
            ("P(no event)", FormatPercent(primary?.ProbabilityNeutral)),
            ("Evidence reliability", FormatPercent(primary?.ReliabilityIndex)),
            ("Reliability meaning", "Validation-quality index; not a probability of a correct call."),
            ("Upside barrier", QuantitativeFormatter.FormatReturn(primary?.UpsideBarrier)),
            ("Downside barrier", primary is null ? "N/A" : $"-{QuantitativeFormatter.FormatReturn(primary.DownsideBarrier)}"),
            ("Expected time to first event", primary is null ? "N/A" : FormatObservations(primary.ExpectedTimeToFirstEvent)),
            ("Median time to up", FormatObservations(primary?.MedianTimeToUp)),
            ("Median time to down", FormatObservations(primary?.MedianTimeToDown)),
            ("Expected NAV P10", QuantitativeFormatter.FormatNumber(primary?.ExpectedNavP10)),
            ("Expected NAV P50", QuantitativeFormatter.FormatNumber(primary?.ExpectedNavP50)),
            ("Expected NAV P90", QuantitativeFormatter.FormatNumber(primary?.ExpectedNavP90)),
            ("Quantile basis", primary?.ReturnQuantiles?.Method ?? "Unavailable: insufficient terminal-return samples."),
            ("ECONOMIC BACKTEST", string.Empty),
            ("Economic status", timing.EconomicBacktest?.Status ?? "Not evaluated"),
            ("Economic diagnostic", timing.EconomicBacktest?.Diagnostic ?? "N/A"),
            ("PROVENANCE", string.Empty),
            ("Training cutoff", QuantitativeFormatter.FormatDate(timing.TrainingCutoff)),
            ("Generated", QuantitativeFormatter.FormatTimestamp(timing.GeneratedAt)),
        ]);
    }

    private void FillHorizons(MarketTimingAssessment timing)
    {
        var grid = this.horizonsCard.Grid;
        grid.Columns.Clear();
        grid.Rows.Clear();
        grid.Columns.Add("horizon", "Horizon");
        grid.Columns.Add("zone", "Zone");
        grid.Columns.Add("up", "P Up");
        grid.Columns.Add("down", "P Down");
        grid.Columns.Add("neutral", "P Neutral");
        grid.Columns.Add("expected", "Exp Return");
        grid.Columns.Add("payoff", "Barrier Payoff");
        grid.Columns.Add("reliability", "ReliabilityIndex");
        grid.Columns.Add("evidence", "Evidence");
        grid.Columns["horizon"]!.FillWeight = 92;
        grid.Columns["zone"]!.FillWeight = 116;
        grid.Columns["payoff"]!.FillWeight = 78;
        foreach (var horizon in timing.Horizons)
        {
            var index = grid.Rows.Add(
                horizon.Horizon.ToString(),
                FormatZone(horizon.Zone),
                FormatPercent(horizon.ProbabilityUp),
                FormatPercent(horizon.ProbabilityDown),
                FormatPercent(horizon.ProbabilityNeutral),
                QuantitativeFormatter.FormatReturn(horizon.ForecastExpectedReturn),
                QuantitativeFormatter.FormatReturn(horizon.ExpectedBarrierPayoff),
                FormatPercent(horizon.ReliabilityIndex),
                horizon.EvidenceStrength.ToString());
            grid.Rows[index].Cells["zone"]!.Style.ForeColor = ResolveZoneColor(horizon.Zone);
            grid.Rows[index].Cells["up"]!.Style.ForeColor = ThemePalette.Positive;
            grid.Rows[index].Cells["down"]!.Style.ForeColor = ThemePalette.Negative;
        }
    }

    private void FillEconomic(MarketTimingAssessment timing)
    {
        var economic = timing.EconomicBacktest;
        if (economic is null)
        {
            GridFactory.SetNameValueRows(this.economicCard.Grid, [("Status", "Not evaluated")]);
            return;
        }

        var rows = new List<(string Name, string Value)>
        {
            ("Status", economic.Status),
            ("Diagnostic", economic.Diagnostic),
            ("Horizon", economic.Horizon?.ToString() ?? "N/A"),
            ("Signals", economic.SignalCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Signal period", $"{QuantitativeFormatter.FormatDate(economic.FirstSignalDate)} to {QuantitativeFormatter.FormatDate(economic.LastSignalDate)}"),
            ("Execution delay", $"{economic.ExecutionDelayObservations.ToString(System.Globalization.CultureInfo.InvariantCulture)} observation(s)"),
            ("Cost", QuantitativeFormatter.FormatReturn(economic.TransactionCostRate)),
            ("Slippage", QuantitativeFormatter.FormatReturn(economic.SlippageRate)),
            ("Brier caveat", "A good Brier score does not guarantee economic profitability."),
        };

        foreach (var result in economic.Results)
        {
            rows.Add(($"{result.StrategyName} cumulative", QuantitativeFormatter.FormatReturn(result.CumulativeReturn)));
            rows.Add(($"{result.StrategyName} annual", QuantitativeFormatter.FormatReturn(result.AnnualizedReturn)));
            rows.Add(($"{result.StrategyName} Sharpe", QuantitativeFormatter.FormatScore(result.Sharpe)));
            rows.Add(($"{result.StrategyName} max drawdown", QuantitativeFormatter.FormatReturn(result.MaximumDrawdown)));
            rows.Add(($"{result.StrategyName} turnover", QuantitativeFormatter.FormatScore(result.Turnover)));
            rows.Add(($"{result.StrategyName} in market", QuantitativeFormatter.FormatPercentShort(result.TimeInMarket)));
        }

        GridFactory.SetNameValueRows(this.economicCard.Grid, rows);
    }

    private void FillWhy(MarketTimingAssessment timing)
    {
        var rows = new List<(string Kind, string Message)>();
        rows.Add(("Summary", timing.Narrative.Summary));
        rows.Add(("Direction", timing.Narrative.DirectionExplanation));
        rows.Add(("Timing", timing.Narrative.TimingExplanation));
        rows.Add(("Risk", timing.Narrative.RiskExplanation));
        rows.Add(("Confidence", timing.Narrative.ConfidenceExplanation));
        rows.Add(("Action", timing.Narrative.ActionExplanation));
        rows.AddRange(timing.Evidence.Select(item => ("Evidence", item)));
        rows.AddRange(timing.CounterEvidence.Select(item => ("Counter", item)));
        rows.AddRange(timing.Warnings.Select(item => ("Warning", item)));
        rows.AddRange(timing.AlertConditions.Where(item => item.Active).Select(item => ($"Alert: {item.Kind}", item.Message)));
        if (timing.AssessmentChange is not null)
        {
            rows.Add((
                "Zone change",
                $"{FormatZone(timing.AssessmentChange.PreviousZone)} to {FormatZone(timing.AssessmentChange.CurrentZone)} {timing.AssessmentChange.ChangedObservationsAgo} observation(s) ago."));
            rows.AddRange(timing.AssessmentChange.Reasons.Select(item => ("Change reason", item)));
        }

        var grid = this.whyCard.Grid;
        grid.Columns.Clear();
        grid.Rows.Clear();
        grid.Columns.Add("kind", "Kind");
        grid.Columns.Add("message", "Message");
        grid.Columns["kind"]!.FillWeight = 30;
        grid.Columns["message"]!.FillWeight = 70;
        foreach (var row in rows)
        {
            var index = grid.Rows.Add(row.Kind, row.Message);
            grid.Rows[index].Cells["kind"]!.Style.ForeColor = row.Kind switch
            {
                "Evidence" => ThemePalette.Positive,
                "Counter" or "Warning" => ThemePalette.Warning,
                _ when row.Kind.StartsWith("Alert:", StringComparison.Ordinal) => ThemePalette.Negative,
                _ => ThemePalette.AccentSecondary,
            };
        }
    }

    private void FillAdvanced(MarketTimingAssessment timing)
    {
        var grid = this.advancedCard.Grid;
        grid.Columns.Clear();
        grid.Rows.Clear();
        grid.Columns.Add("scope", "Scope");
        grid.Columns.Add("model", "Model");
        grid.Columns.Add("samples", "OOS");
        grid.Columns.Add("weight", "Weight");
        grid.Columns.Add("brier", "Brier skill");
        grid.Columns.Add("ece", "ECE");
        grid.Columns.Add("logloss", "Log loss");
        grid.Columns.Add("calibration", "Calibration");
        grid.Columns.Add("eligible", "Eligible");
        grid.Columns.Add("reason", "Reason");
        grid.Columns.Add("diagnostic", "Diagnostic");
        grid.Columns["scope"]!.FillWeight = 86;
        grid.Columns["model"]!.FillWeight = 126;
        grid.Columns["diagnostic"]!.FillWeight = 180;
        foreach (var result in timing.ModelArenaResults)
        {
            var horizon = result.Definition.Horizon.ToString();
            foreach (var component in result.Ensemble.Components)
            {
                grid.Rows.Add(
                    horizon,
                    component.ModelName,
                    "N/A",
                    FormatPercent(component.Weight),
                    QuantitativeFormatter.FormatScore(component.BrierSkill),
                    QuantitativeFormatter.FormatScore(component.CalibrationPenalty),
                    "N/A",
                    "Weighted",
                    "Ensemble",
                    "Eligible",
                    result.Ensemble.Diagnostic);
            }

            foreach (var model in result.Models)
            {
                grid.Rows.Add(
                    horizon,
                    model.ModelName,
                    model.Calibration.SampleCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "N/A",
                    QuantitativeFormatter.FormatScore(model.BrierSkillVsBaseline),
                    QuantitativeFormatter.FormatScore(model.Calibration.ExpectedCalibrationError),
                    QuantitativeFormatter.FormatScore(model.Calibration.LogLoss),
                    model.CalibrationDiagnostic.Status.ToString(),
                    model.EligibilityStatus.ToString(),
                    model.RejectionReason,
                    model.Diagnostic);
            }

            grid.Rows.Add(
                horizon,
                "Hazard",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                $"CIF up {FormatPercent(result.HazardForecast.ProbabilityUpByHorizon)}, CIF down {FormatPercent(result.HazardForecast.ProbabilityDownByHorizon)}, survival {FormatPercent(result.HazardForecast.ProbabilityNoEventByHorizon)}.");
        }

        if (grid.Rows.Count == 0)
        {
            grid.Rows.Add("Timing arena", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", timing.VolatilityDiagnostic);
        }
    }

    private static string FormatAction(TimingDecisionAction action)
    {
        return action switch
        {
            TimingDecisionAction.InsufficientEvidence => "Insufficient evidence",
            TimingDecisionAction.StrongBuy or TimingDecisionAction.StrongAccumulate => "Strong buy",
            TimingDecisionAction.Buy or TimingDecisionAction.Accumulate => "Buy",
            TimingDecisionAction.Hold => "Hold",
            TimingDecisionAction.WatchPositive => "Watch positive",
            TimingDecisionAction.WatchNegative => "Watch negative",
            TimingDecisionAction.Reduce => "Reduce",
            TimingDecisionAction.Sell or TimingDecisionAction.StrongReduce => "Sell",
            _ => "Hold",
        };
    }

    private static string FormatZone(MarketTimingZone zone)
    {
        return zone switch
        {
            MarketTimingZone.InsufficientEvidence => "Insufficient evidence",
            MarketTimingZone.StrongAccumulation => "Strong accumulation",
            MarketTimingZone.Accumulation => "Accumulation",
            MarketTimingZone.Reduction => "Reduction",
            MarketTimingZone.StrongReduction => "Strong reduction",
            MarketTimingZone.WatchPositive => "Watch positive",
            MarketTimingZone.WatchNegative => "Watch negative",
            _ => "Neutral",
        };
    }

    private static string FormatObservations(double? observations)
    {
        return observations is null || !double.IsFinite(observations.Value)
            ? "N/A"
            : $"{observations.Value:0.0} obs";
    }

    private static string FormatObservations(int? observations)
    {
        return observations is null
            ? "N/A"
            : $"{observations.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} obs";
    }

    private static string FormatPercent(double? value)
    {
        return value is null || !double.IsFinite(value.Value)
            ? "N/A"
            : QuantitativeFormatter.FormatPercentShort(value.Value);
    }

    private static Color ResolveActionColor(TimingDecision decision)
    {
        if (decision.Qualification == SignalQualification.Unavailable)
        {
            return ThemePalette.SubtleText;
        }

        return decision.Direction switch
        {
            DirectionalSignal.Buy => ThemePalette.Positive,
            DirectionalSignal.Sell => ThemePalette.Negative,
            DirectionalSignal.Hold => ThemePalette.AccentSecondary,
            _ => ThemePalette.AccentSecondary,
        };
    }

    private static Color ResolveReliabilityColor(double? reliability)
    {
        return reliability switch
        {
            >= 0.66d => ThemePalette.Positive,
            >= 0.35d => ThemePalette.Warning,
            _ => ThemePalette.SubtleText,
        };
    }

    private static Color ResolveZoneColor(MarketTimingZone zone)
    {
        return zone switch
        {
            MarketTimingZone.InsufficientEvidence => ThemePalette.SubtleText,
            MarketTimingZone.StrongAccumulation or MarketTimingZone.Accumulation => ThemePalette.Positive,
            MarketTimingZone.StrongReduction or MarketTimingZone.Reduction => ThemePalette.Negative,
            MarketTimingZone.WatchNegative or MarketTimingZone.WatchPositive => ThemePalette.Warning,
            _ => ThemePalette.AccentSecondary,
        };
    }
}
