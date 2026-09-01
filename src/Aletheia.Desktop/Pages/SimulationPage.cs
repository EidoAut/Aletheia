#pragma warning disable SA1516 // Existing field grouping is kept stable.
#pragma warning disable SA1642 // Existing constructor summaries are kept stable.

using System.Globalization;
using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays periodic-investment Monte Carlo baseline scenarios. The visual hierarchy is
/// declared in <c>SimulationPage.Designer.cs</c>; this partial contains simulation behavior.
/// </summary>
internal sealed partial class SimulationPage : WorkspacePageBase
{
    private Func<InvestmentSimulationRequest, Task<InvestmentSimulationSummary?>> runSimulation =
        _ => Task.FromResult<InvestmentSimulationSummary?>(null);
    private FundWorkspace? workspace;
    private string? datasetFingerprint;

    /// <summary>
    /// Initializes a designer-safe simulation page.
    /// </summary>
    public SimulationPage()
    {
        this.InitializeComponent();
        this.runButton.Click += async (_, _) => await this.RunAsync().ConfigureAwait(true);
        this.runButton.Enabled = false;
        this.detailsCard.ShowCount = false;
        this.ClearResults();
    }

    /// <summary>
    /// Initializes a runtime simulation page.
    /// </summary>
    /// <param name="runSimulation">The shell-managed simulation action.</param>
    public SimulationPage(Func<InvestmentSimulationRequest, Task<InvestmentSimulationSummary?>> runSimulation)
        : this()
    {
        this.runSimulation = runSimulation ?? throw new ArgumentNullException(nameof(runSimulation));
    }

    /// <inheritdoc />
    public override string PageTitle => "Simulation";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        var nextFingerprint = workspace?.Analysis.Dataset.DatasetFingerprint;
        var changed = !string.Equals(this.datasetFingerprint, nextFingerprint, StringComparison.Ordinal);
        this.workspace = workspace;
        this.datasetFingerprint = nextFingerprint;
        this.runButton.Enabled = workspace is not null;
        this.fundNameLabel.Text = workspace?.Analysis.Dataset.FundName ?? "No active dataset";
        this.fundMetaLabel.Text = workspace is null
            ? "Load a fund before running a scenario."
            : BuildDatasetMeta(workspace);
        if (changed)
        {
            this.ClearResults();
        }
    }

    private static string BuildDatasetMeta(FundWorkspace workspace)
    {
        var dataset = workspace.Analysis.Dataset;
        var currency = string.IsNullOrWhiteSpace(dataset.Currency) ? "currency n/a" : dataset.Currency;
        return $"{dataset.Identifier}  ·  {currency}  ·  {dataset.ObservationCount:N0} observations";
    }

    private static IReadOnlyList<DatedValue> ToSeries(
        IReadOnlyList<InvestmentValueProjectionPoint> points,
        Func<InvestmentValueProjectionPoint, double> selector)
    {
        return points.Select(point => new DatedValue(point.Date, selector(point))).ToArray();
    }

    private async Task RunAsync()
    {
        if (this.workspace is null)
        {
            return;
        }

        var request = new InvestmentSimulationRequest(
            decimal.ToDouble(this.initialInvestment.Value),
            decimal.ToDouble(this.monthlyContribution.Value),
            decimal.ToInt32(this.horizonYears.Value),
            decimal.ToInt32(this.pathCount.Value));
        this.runButton.Enabled = false;
        this.runButton.Text = "Running scenario...";
        this.stateLabel.Text = "Generating deterministic baseline paths...";
        this.stateLabel.ForeColor = ThemePalette.Accent;
        try
        {
            var result = await this.runSimulation(request).ConfigureAwait(true);
            if (result is null)
            {
                this.stateLabel.Text = "Simulation cancelled or unavailable.";
                this.stateLabel.ForeColor = ThemePalette.Warning;
                return;
            }

            this.DisplayResult(result);
            this.stateLabel.Text = "Scenario complete. No investment signal is inferred.";
            this.stateLabel.ForeColor = ThemePalette.Positive;
        }
        finally
        {
            this.runButton.Text = "Run baseline scenario";
            this.runButton.Enabled = this.workspace is not null;
        }
    }

    private void DisplayResult(InvestmentSimulationSummary result)
    {
        var currency = result.Dataset.Currency;
        this.metrics.SetMetrics([
            ("Total contributed", QuantitativeFormatter.FormatCurrency(result.TotalContributed, currency), null),
            ("Median terminal", QuantitativeFormatter.FormatCurrency(result.MedianTerminalValue, currency), ThemePalette.Accent),
            ("P10 terminal", QuantitativeFormatter.FormatCurrency(result.P10TerminalValue, currency), ThemePalette.Negative),
            ("P90 terminal", QuantitativeFormatter.FormatCurrency(result.P90TerminalValue, currency), ThemePalette.Positive),
            ("Mean terminal", QuantitativeFormatter.FormatCurrency(result.MeanTerminalValue, currency), null),
            ("P(below contributions)", QuantitativeFormatter.FormatPercentShort(result.ProbabilityTerminalBelowContributions), ThemePalette.Warning),
        ]);
        this.trajectoryChart.PlotLines(
            "Periodic-investment value bands",
            [
                ("Contributed", ToSeries(result.Trajectory, point => point.TotalContributed), ThemePalette.MutedText),
                ("P10", ToSeries(result.Trajectory, point => point.P10Value), ThemePalette.Negative),
                ("Median", ToSeries(result.Trajectory, point => point.MedianValue), ThemePalette.Accent),
                ("P90", ToSeries(result.Trajectory, point => point.P90Value), ThemePalette.Positive),
            ],
            string.IsNullOrWhiteSpace(currency) ? "Portfolio value" : $"Portfolio value ({currency})");
        GridFactory.SetNameValueRows(this.detailsCard.Grid, [
            ("DATASET", string.Empty),
            ("Fund", result.Dataset.FundName),
            ("Dataset fingerprint", QuantitativeFormatter.FormatFingerprint(result.Dataset.DatasetFingerprint, 18)),
            ("Simulation period", $"{result.StartDate:yyyy-MM-dd} -> {result.TargetDate:yyyy-MM-dd}"),
            ("PLAN", string.Empty),
            ("Initial capital", QuantitativeFormatter.FormatCurrency(result.Request.InitialInvestment, currency)),
            ("Monthly contribution", QuantitativeFormatter.FormatCurrency(result.Request.MonthlyContribution, currency)),
            ("Horizon", $"{result.Request.HorizonYears.ToString(CultureInfo.InvariantCulture)} years / {(result.Request.HorizonYears * 12).ToString(CultureInfo.InvariantCulture)} months"),
            ("Paths", result.Request.PathCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Seed", result.Request.Seed.ToString(CultureInfo.InvariantCulture)),
            ("RETURN SCALING", string.Empty),
            ("Observation periods / month", result.ObservationPeriodsPerMonth.ToString("0.###", CultureInfo.InvariantCulture)),
            ("Historical mean log return / obs", QuantitativeFormatter.FormatReturn(result.HistoricalMeanLogReturnPerObservation)),
            ("Historical std. deviation / obs", QuantitativeFormatter.FormatReturn(result.HistoricalStandardDeviationPerObservation)),
            ("Scaled monthly mean log return", QuantitativeFormatter.FormatReturn(result.MonthlyMeanLogReturn)),
            ("Scaled monthly std. deviation", QuantitativeFormatter.FormatReturn(result.MonthlyStandardDeviation)),
            ("DISTRIBUTION", string.Empty),
            ("P25 terminal", QuantitativeFormatter.FormatCurrency(result.P25TerminalValue, currency)),
            ("P75 terminal", QuantitativeFormatter.FormatCurrency(result.P75TerminalValue, currency)),
            ("DISCIPLINE", string.Empty),
            ("Methodology", result.Methodology),
            ("Interpretation", "Scenario distribution only; not a validated forecast or recommendation."),
            ("Investment signal", "NO CALL"),
        ]);
    }

    private void ClearResults()
    {
        this.metrics.SetMetrics([
            ("Simulation", "Not run", null),
            ("Signal", "NO CALL", ThemePalette.Warning),
        ]);
        this.trajectoryChart.ShowEmpty("Periodic-investment value bands", "Date", "Portfolio value");
        GridFactory.SetNameValueRows(this.detailsCard.Grid, [
            ("SCENARIO", string.Empty),
            ("Status", "Configure capital, contribution, horizon and paths, then run the simulation."),
            ("Scope", "Fees, taxes and inflation are not included."),
        ]);
        this.stateLabel.Text = this.workspace is null ? "Load a fund to begin." : "Ready to run.";
        this.stateLabel.ForeColor = ThemePalette.MutedText;
    }
}
