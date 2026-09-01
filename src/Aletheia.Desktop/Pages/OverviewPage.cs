#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays the primary fund overview.
/// </summary>
internal sealed partial class OverviewPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="OverviewPage"/> class.
    /// </summary>
    public OverviewPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Overview";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var analysis = workspace.Analysis;
        var report = analysis.ResearchReport;
        var twelveMonthForecast = report?.TwelveMonthForecast;
        var twelveMonthExpectedReturn = twelveMonthForecast?.ExpectedReturnOrNull;
        var twelveMonthProbabilityPositive = twelveMonthForecast?.ProbabilityPositiveOrNull;
        var twelveMonthLossProbability = twelveMonthForecast?.ProbabilityLossGreaterThanTenPercentOrNull;
        var sourceObservationCount = analysis.Dataset.SourceObservationCount == 0
            ? analysis.Dataset.ObservationCount
            : analysis.Dataset.SourceObservationCount;
        this.metrics.SetMetrics([
            ("Fund quality", report is null ? "n/a" : $"{report.FundScore.Score:0.0}/10", ThemePalette.Accent),
            ("Evidence", report?.FundScore.Confidence.ToString() ?? "n/a", null),
            ("Investor view", report is null ? "n/a" : $"{report.CurrentAttractiveness.Category}", ThemePalette.AccentSecondary),
            ("Guidance", report?.DecisionSignal.DisplayLabel ?? "NO CALL", report is null ? ThemePalette.Warning : ResolveSignalColor(report.DecisionSignal)),
            ("Actionable", report?.Actionability.Confidence.ToString() ?? "n/a", report is null ? null : ResolveActionabilityColor(report.Actionability)),
            ("CAGR", QuantitativeFormatter.FormatReturn(analysis.Performance.Cagr), ResolveReturnColor(analysis.Performance.Cagr)),
            ("12M expected", QuantitativeFormatter.FormatReturn(twelveMonthExpectedReturn), ResolveReturnColor(twelveMonthExpectedReturn)),
            ("Downside >10%", QuantitativeFormatter.FormatPercentShort(twelveMonthLossProbability), ThemePalette.Warning),
            ("Max drawdown", QuantitativeFormatter.FormatReturn(analysis.Performance.MaximumDrawdown.MaximumDrawdown), ThemePalette.Negative),
        ]);
        this.navChart.PlotLine("NAV history", analysis.Nav, "NAV");
        this.drawdownChart.PlotLine("Drawdown", analysis.Drawdown, "Drawdown");
        this.rollingChart.PlotLine("Rolling volatility", analysis.RollingVolatility, "Annualized volatility");
        GridFactory.SetNameValueRows(this.detailsCard.Grid, [
            ("Fund", analysis.Dataset.FundName),
            ("Identifier", analysis.Dataset.Identifier.ToString()),
            ("Effective date range", $"{analysis.Dataset.StartDate:yyyy-MM-dd} → {analysis.Dataset.EndDate:yyyy-MM-dd}"),
            ("Source date range", $"{QuantitativeFormatter.FormatDate(analysis.Dataset.SourceStartDate ?? analysis.Dataset.StartDate)} → {QuantitativeFormatter.FormatDate(analysis.Dataset.SourceEndDate ?? analysis.Dataset.EndDate)}"),
            ("Effective observations", analysis.Dataset.ObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Source observations", sourceObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Carry-forward rows excluded", analysis.Dataset.SyntheticObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Effective frequency", analysis.Dataset.ObservationFrequency.ToString()),
            ("Effective policy", analysis.Dataset.EffectiveObservationPolicy),
            ("Latest effective obs", report is null ? QuantitativeFormatter.FormatDate(analysis.Dataset.LastEffectiveObservationDate ?? analysis.Dataset.EndDate) : QuantitativeFormatter.FormatDate(report.DataFreshness.LastEffectiveObservationDate)),
            ("Data freshness", report is null ? "n/a" : $"{report.DataFreshness.Status} ({report.DataFreshness.DataAgeDays.ToString(System.Globalization.CultureInfo.InvariantCulture)} days)"),
            ("Provider", analysis.Dataset.Provenance?.ProviderDisplayName ?? analysis.Dataset.Provider ?? "n/a"),
            ("ISIN", analysis.Dataset.Provenance?.Isin ?? "n/a"),
            ("Source", analysis.Dataset.Provenance?.SourceReference ?? analysis.Dataset.SourcePath ?? "n/a"),
            ("Cache", analysis.Dataset.Provenance is null ? "n/a" : analysis.Dataset.Provenance.IsFromCache ? "Cached provider payload" : "Fresh/local provider payload"),
            ("Original / normalized obs", analysis.Dataset.Provenance is null ? "n/a" : $"{analysis.Dataset.Provenance.OriginalObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)} / {analysis.Dataset.Provenance.NormalizedObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}"),
            ("Quality score", analysis.DataQuality.QualityScore.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Dataset fingerprint", QuantitativeFormatter.FormatFingerprint(analysis.Dataset.DatasetFingerprint)),
            ("INVESTOR GUIDANCE", string.Empty),
            ("Fund score", report is null ? "n/a" : $"{report.FundScore.Score:0.0} / 10"),
            ("Score confidence", report?.FundScore.Confidence.ToString() ?? "n/a"),
            ("Strategic attractiveness", report is null ? "n/a" : $"{report.CurrentAttractiveness.Score:0.0} / 10 - {report.CurrentAttractiveness.Category}"),
            ("Strategic decision", report is null ? "NO CALL" : $"{report.DecisionSignal.DisplayLabel} ({report.DecisionSignal.Qualification})"),
            ("Actionability status", report is null ? "n/a" : $"{report.Actionability.Status} ({report.Actionability.Level}, {report.Actionability.Confidence})"),
            ("Plain-language rule", "Good funds need quality, fresh data, tolerable risk and validated evidence."),
            ("Latest effective regime", report?.CurrentRegimeLabel ?? "n/a"),
            ("Regime probability", report?.CurrentRegimeProbability is null ? "n/a" : QuantitativeFormatter.FormatPercentShort(report.CurrentRegimeProbability.Value)),
            ("12M expected return", QuantitativeFormatter.FormatReturn(twelveMonthExpectedReturn)),
            ("12M P(positive)", QuantitativeFormatter.FormatPercentShort(twelveMonthProbabilityPositive)),
            ("12M P(loss > 10%)", QuantitativeFormatter.FormatPercentShort(twelveMonthLossProbability)),
            ("Model agreement", report?.Ensemble is null ? "n/a" : QuantitativeFormatter.FormatPercentShort(1d - Math.Min(1d, report.Ensemble.ModelDisagreement))),
            ("Warnings", report is null ? "n/a" : report.Warnings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("DYNAMIC STATE", string.Empty),
            ("Trend", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.Trend))),
            ("Momentum", QuantitativeFormatter.FormatReturn(GetStateValue(analysis.CurrentState, StandardStateDimensions.Momentum))),
            ("Current drawdown", QuantitativeFormatter.FormatReturn(analysis.Performance.CurrentDrawdown)),
            ("Log-NAV velocity", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.LogNavVelocityPerObservation))),
            ("Log-NAV acceleration", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.LogNavAccelerationPerObservationSquared))),
            ("Dominant spectral period", QuantitativeFormatter.FormatNumber(analysis.Spectrum.DominantFrequency?.PeriodObservations)),
            ("Spectral persistence", QuantitativeFormatter.FormatNumber(analysis.SpectralStability.DominantPeriodPersistence)),
            ("Forecast availability", analysis.Forecasts.Runs.Any(run => run.Distribution is not null) ? "Available" : "N/A"),
            ("Model Arena status", workspace.Arena is null ? "Not run" : "Available"),
            ("Investment signal", report?.DecisionSignal.DisplayLabel ?? "NO CALL"),
        ]);
    }

    private static double? GetStateValue(DynamicState state, StateDimension dimension)
    {
        return state.TryGetValue(dimension, out var value) ? value : null;
    }

    private static Color? ResolveReturnColor(double? value)
    {
        return value.HasValue ? value.Value >= 0d ? ThemePalette.Positive : ThemePalette.Negative : null;
    }

    private static Color? ResolveSignalColor(DecisionSignal signal)
    {
        if (signal.Qualification == SignalQualification.Unavailable)
        {
            return ThemePalette.SubtleText;
        }

        return signal.Direction switch
        {
            DirectionalSignal.Buy => ThemePalette.Positive,
            DirectionalSignal.Sell => ThemePalette.Negative,
            DirectionalSignal.Hold => ThemePalette.AccentSecondary,
            _ => ThemePalette.Warning,
        };
    }

    private static Color? ResolveActionabilityColor(ActionabilityAssessment actionability)
    {
        return actionability.Level switch
        {
            SignalActionabilityLevel.Actionable => ThemePalette.Positive,
            SignalActionabilityLevel.Unavailable => ThemePalette.Warning,
            _ => ThemePalette.AccentSecondary,
        };
    }
}
