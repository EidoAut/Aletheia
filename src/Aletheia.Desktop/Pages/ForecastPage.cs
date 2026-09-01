using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays current forecast model outputs.
/// </summary>
internal sealed partial class ForecastPage : WorkspacePageBase
{
    private FundWorkspace? workspace;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastPage"/> class.
    /// </summary>
    public ForecastPage()
    {
        this.InitializeComponent();
        this.forecastCard.Grid.SelectionChanged += (_, _) => this.UpdateSelectedForecast();
    }

    /// <inheritdoc />
    public override string PageTitle => "Forecast";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        this.workspace = workspace;
        var forecastGrid = this.forecastCard.Grid;
        forecastGrid.Columns.Clear();
        forecastGrid.Rows.Clear();
        GridFactory.SetNameValueRows(this.detailCard.Grid, [("Forecast", "Select a completed forecast run.")]);
        this.quantileChart.ShowEmpty("Forecast quantiles", "Quantile (%)", "Return");
        if (workspace is null)
        {
            return;
        }

        forecastGrid.Columns.Add("model", "Model");
        forecastGrid.Columns.Add("horizon", "Horizon");
        forecastGrid.Columns.Add("point", "Point");
        forecastGrid.Columns.Add("prob", "P(Return > 0)");
        forecastGrid.Columns.Add("status", "Status");
        forecastGrid.Columns["model"]!.FillWeight = 150;
        forecastGrid.Columns["horizon"]!.FillWeight = 82;
        forecastGrid.Columns["point"]!.FillWeight = 80;
        forecastGrid.Columns["prob"]!.FillWeight = 92;
        forecastGrid.Columns["status"]!.FillWeight = 78;
        foreach (var run in workspace.Analysis.Forecasts.Runs)
        {
            forecastGrid.Rows.Add(
                run.Model.Name,
                run.RequestedHorizon.ToString(),
                run.Distribution is null ? "N/A" : QuantitativeFormatter.FormatCapabilityReturn(run.Capabilities, ForecastCapabilities.PointForecast, run.Distribution.PointForecastReturn),
                run.Distribution is null || !run.Capabilities.HasFlag(ForecastCapabilities.ProbabilityPositive) ? "N/A" : QuantitativeFormatter.FormatPercentShort(run.Distribution.ProbabilityPositive),
                run.Status.ToString());
        }

        if (forecastGrid.Rows.Count > 0)
        {
            forecastGrid.ClearSelection();
            forecastGrid.CurrentCell = forecastGrid.Rows[0].Cells[0];
            forecastGrid.Rows[0].Selected = true;
        }

        this.UpdateSelectedForecast();
    }

    private void UpdateSelectedForecast()
    {
        var forecastGrid = this.forecastCard.Grid;
        if (this.workspace is null || forecastGrid.CurrentRow is null)
        {
            return;
        }

        var index = forecastGrid.CurrentRow.Index;
        if (index < 0 || index >= this.workspace.Analysis.Forecasts.Runs.Count)
        {
            return;
        }

        var run = this.workspace.Analysis.Forecasts.Runs[index];
        GridFactory.SetNameValueRows(this.detailCard.Grid, [
            ("MODEL", string.Empty),
            ("Model", run.Model.Name),
            ("Version", run.Model.Version),
            ("Configuration fingerprint", QuantitativeFormatter.FormatFingerprint(run.ConfigurationFingerprint)),
            ("Requested horizon", run.RequestedHorizon.ToString()),
            ("Resolved observations", run.Distribution?.HorizonResolution.EffectiveObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"),
            ("Target date", QuantitativeFormatter.FormatDate(run.Distribution?.HorizonResolution.TargetDate)),
            ("Capabilities", run.Capabilities.ToString()),
            ("DISTRIBUTION", string.Empty),
            ("Point statistic", run.PointForecastStatistic.ToString()),
            ("Point forecast", run.Distribution is null ? "N/A" : QuantitativeFormatter.FormatCapabilityReturn(run.Capabilities, ForecastCapabilities.PointForecast, run.Distribution.PointForecastReturn)),
            ("Expected return", run.Distribution is null ? "N/A" : QuantitativeFormatter.FormatCapabilityReturn(run.Capabilities, ForecastCapabilities.ExpectedReturn, run.Distribution.ExpectedReturn)),
            ("Median", run.Distribution is null ? "N/A" : QuantitativeFormatter.FormatCapabilityReturn(run.Capabilities, ForecastCapabilities.Median, run.Distribution.MedianReturn)),
            ("P(Return > 0)", run.Distribution is null || !run.Capabilities.HasFlag(ForecastCapabilities.ProbabilityPositive) ? "N/A" : QuantitativeFormatter.FormatPercentShort(run.Distribution.ProbabilityPositive)),
            ("Status", run.Status.ToString()),
            ("Failure", run.FailureReason ?? "N/A"),
        ]);
        var quantiles = run.Distribution?.Percentiles.OrderBy(pair => pair.Key).ToArray() ?? [];
        this.quantileChart.PlotXYLine(
            "Forecast quantiles",
            quantiles.Select(pair => (double)pair.Key).ToArray(),
            quantiles.Select(pair => pair.Value).ToArray(),
            "Quantile (%)",
            "Return");
    }
}
