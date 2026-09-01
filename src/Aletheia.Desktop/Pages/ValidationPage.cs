#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;
using Aletheia.Validation;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays scientific validation diagnostics.
/// </summary>
internal sealed partial class ValidationPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPage"/> class.
    /// </summary>
    public ValidationPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Validation";

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
            this.calibration.ShowEmpty("Probability calibration", "Predicted probability", "Observed frequency");
            this.predictedActual.ShowEmpty("Predicted vs actual", "Predicted return", "Actual return");
            this.errors.ShowEmpty("Forecast error through time", "Cutoff", "Actual - prediction");
            GridFactory.SetNameValueRows(this.configurationCard.Grid, [("Validation", "Run Model Arena to populate validation diagnostics.")]);
            return;
        }

        var probabilityModel = arena.Models.FirstOrDefault(model => model.ProbabilityCommonSupportMetrics.Probability.Status == MetricStatus.Available);
        if (probabilityModel is not null)
        {
            this.calibration.PlotCalibration(probabilityModel.ProbabilityCommonSupportMetrics.Probability.CalibrationBins);
        }
        else
        {
            this.calibration.ShowEmpty("Probability calibration unavailable", "Predicted probability", "Observed frequency");
        }

        var pointModel = arena.Models.FirstOrDefault(model => model.PointCommonSupportMetrics.Point.Status == MetricStatus.Available);
        if (pointModel is not null)
        {
            this.predictedActual.PlotPredictedVsActual(pointModel.PointCommonSupportSamples);
            this.errors.PlotErrors(pointModel.PointCommonSupportSamples);
        }
        else
        {
            this.predictedActual.ShowEmpty("Predicted vs actual unavailable", "Predicted return", "Actual return");
            this.errors.ShowEmpty("Forecast errors unavailable", "Cutoff", "Actual - prediction");
        }

        GridFactory.SetNameValueRows(this.configurationCard.Grid, [
            ("Evaluation period", $"{QuantitativeFormatter.FormatDate(arena.EvaluationStartDate)} → {QuantitativeFormatter.FormatDate(arena.EvaluationEndDate)}"),
            ("Training mode", "Expanding walk-forward"),
            ("Horizon", arena.Horizon.ToString()),
            ("Point common support", arena.PointCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Probability common support", arena.ProbabilityCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Quantile common support", arena.QuantileCommonSupportEventCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Non-overlapping samples", arena.Models.FirstOrDefault()?.Evaluation.NonOverlappingSamples.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"),
            ("Ranking", arena.RankingDiagnostic),
            ("Investment signal", "NO CALL"),
        ]);
    }
}
