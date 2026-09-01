#pragma warning disable SA1204 // Existing designer-backed helper ordering is kept stable.
#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Validation;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays common-support Model Arena results.
/// </summary>
internal sealed partial class ModelArenaPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelArenaPage"/> class.
    /// </summary>
    public ModelArenaPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Model Arena";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        this.SetArena(workspace?.Arena);
    }

    /// <inheritdoc />
    public override void SetArena(ModelArenaResult? arena)
    {
        if (arena is null)
        {
            this.metrics.SetMetrics([
                ("Model Arena", "NOT RUN", ThemePalette.Warning),
                ("Next action", "RUN ARENA", ThemePalette.Accent),
            ]);
            GridFactory.SetNameValueRows(this.coverageCard.Grid, [("Coverage", "Run Model Arena to populate results.")]);
            GridFactory.SetNameValueRows(this.pointCard.Grid, [("Point metrics", "N/A")]);
            GridFactory.SetNameValueRows(this.probabilityCard.Grid, [("Probability metrics", "N/A")]);
            GridFactory.SetNameValueRows(this.quantileCard.Grid, [("Quantile metrics", "N/A")]);
            return;
        }

        this.metrics.SetMetrics([
            ("Point common support", arena.PointCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.AccentSecondary),
            ("Probability support", arena.ProbabilityCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.Positive),
            ("Quantile support", arena.QuantileCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.Warning),
            ("Point baseline", arena.PointForecastBaseline?.Name ?? "N/A", null),
            ("Probability baseline", arena.ProbabilityBaseline?.Name ?? "N/A", null),
            ("Signal", "NO CALL", ThemePalette.Warning),
        ]);
        this.FillCoverage(arena);
        this.FillPoint(arena);
        this.FillProbability(arena);
        this.FillQuantile(arena);
    }

    private void FillCoverage(ModelArenaResult arena)
    {
        var coverage = this.coverageCard.Grid;
        coverage.Columns.Clear();
        coverage.Rows.Clear();
        coverage.Columns.Add("model", "Model");
        coverage.Columns.Add("eligible", "Eligible");
        coverage.Columns.Add("success", "Success");
        coverage.Columns.Add("failed", "Failed");
        coverage.Columns.Add("coverage", "Coverage");
        coverage.Columns.Add("caps", "Capabilities");
        coverage.Columns["model"]!.FillWeight = 130;
        coverage.Columns["caps"]!.FillWeight = 170;
        foreach (var model in arena.Models)
        {
            coverage.Rows.Add(
                model.Model.Name,
                model.Coverage.EligibleEvents,
                model.Coverage.SuccessfulForecasts,
                model.Coverage.FailedForecasts,
                QuantitativeFormatter.FormatPercentShort(model.Coverage.CoverageRatio),
                FormatCapabilities(model.Capabilities));
        }
    }

    private void FillPoint(ModelArenaResult arena)
    {
        var point = this.pointCard.Grid;
        point.Columns.Clear();
        point.Rows.Clear();
        point.Columns.Add("model", "Model");
        point.Columns.Add("n", "N");
        point.Columns.Add("mae", "MAE");
        point.Columns.Add("rmse", "RMSE");
        point.Columns.Add("dir", "Directional");
        point.Columns.Add("skill", "Baseline skill");
        point.Columns["model"]!.FillWeight = 140;
        foreach (var model in arena.Models)
        {
            var pointMetrics = model.PointCommonSupportMetrics.Point;
            point.Rows.Add(
                model.Model.Name,
                pointMetrics.SampleCount,
                QuantitativeFormatter.FormatReturn(pointMetrics.MeanAbsoluteError),
                QuantitativeFormatter.FormatReturn(pointMetrics.RootMeanSquaredError),
                QuantitativeFormatter.FormatPercentShort(pointMetrics.DirectionalAccuracy),
                QuantitativeFormatter.FormatPercentShort(model.RelativeSkill?.MeanAbsoluteErrorSkill));
        }
    }

    private void FillProbability(ModelArenaResult arena)
    {
        var probability = this.probabilityCard.Grid;
        probability.Columns.Clear();
        probability.Rows.Clear();
        probability.Columns.Add("model", "Model");
        probability.Columns.Add("n", "N");
        probability.Columns.Add("brier", "Brier");
        probability.Columns.Add("ece", "ECE");
        probability.Columns.Add("skill", "Brier skill");
        probability.Columns["model"]!.FillWeight = 150;
        foreach (var model in arena.Models)
        {
            var probabilityMetrics = model.ProbabilityCommonSupportMetrics.Probability;
            probability.Rows.Add(
                model.Model.Name,
                probabilityMetrics.SampleCount,
                QuantitativeFormatter.FormatScore(probabilityMetrics.BrierScore),
                QuantitativeFormatter.FormatScore(probabilityMetrics.ExpectedCalibrationError),
                QuantitativeFormatter.FormatPercentShort(model.RelativeSkill?.BrierScoreSkill));
        }
    }

    private void FillQuantile(ModelArenaResult arena)
    {
        var quantile = this.quantileCard.Grid;
        quantile.Columns.Clear();
        quantile.Rows.Clear();
        quantile.Columns.Add("model", "Model");
        quantile.Columns.Add("p50", "Pinball p50");
        quantile.Columns.Add("coverage", "Coverage");
        quantile.Columns.Add("width", "Average width");
        quantile.Columns["model"]!.FillWeight = 150;
        foreach (var model in arena.Models)
        {
            var quantileMetrics = model.QuantileCommonSupportMetrics;
            var hasP50 = quantileMetrics.Quantile.MeanPinballLossByPercentile.TryGetValue(50, out var p50);
            quantile.Rows.Add(
                model.Model.Name,
                hasP50 ? QuantitativeFormatter.FormatScore(p50) : "N/A",
                QuantitativeFormatter.FormatPercentShort(quantileMetrics.IntervalCoverage.ObservedCoverage),
                QuantitativeFormatter.FormatReturn(quantileMetrics.IntervalCoverage.AverageIntervalWidth));
        }
    }

    private static string FormatCapabilities(ForecastCapabilities capabilities)
    {
        return capabilities == ForecastCapabilities.None ? "None" : capabilities.ToString();
    }
}
