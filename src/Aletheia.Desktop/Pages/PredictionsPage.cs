#pragma warning disable SA1642 // Existing constructor summaries are kept stable.

using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays the SQLite prediction ledger.
/// </summary>
internal sealed partial class PredictionsPage : WorkspacePageBase
{
    private AletheiaApplicationService application = null!;
    private IReadOnlyList<PredictionLedgerSummary> predictions = [];

    /// <summary>
    /// Initializes a designer-safe prediction ledger page.
    /// </summary>
    public PredictionsPage()
    {
        this.InitializeComponent();
        this.listCard.Grid.SelectionChanged += async (_, _) => await this.UpdateDetailsAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Initializes a runtime prediction ledger page.
    /// </summary>
    /// <param name="application">The application service.</param>
    public PredictionsPage(AletheiaApplicationService application)
        : this()
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
    }

    /// <inheritdoc />
    public override string PageTitle => "Predictions";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
    }

    /// <summary>
    /// Refreshes the prediction ledger view.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel I/O.</param>
    /// <returns>A task representing the refresh.</returns>
    public async Task RefreshPredictionsAsync(CancellationToken cancellationToken = default)
    {
        if (this.application is null)
        {
            return;
        }

        this.predictions = await this.application.GetPredictionListAsync(100, cancellationToken).ConfigureAwait(true);
        var list = this.listCard.Grid;
        list.Columns.Clear();
        list.Rows.Clear();
        GridFactory.SetNameValueRows(this.detailsCard.Grid, [("Prediction", "Select a ledger record.")]);
        list.Columns.Add("id", "ID");
        list.Columns.Add("origin", "Origin");
        list.Columns.Add("fund", "Fund");
        list.Columns.Add("cutoff", "Cutoff");
        list.Columns.Add("model", "Model");
        list.Columns.Add("horizon", "Horizon");
        list.Columns.Add("point", "Point");
        list.Columns.Add("expected", "Expected");
        list.Columns.Add("prob", "P+");
        list.Columns.Add("target", "Target");
        list.Columns.Add("dataset", "Dataset");
        list.Columns["id"]!.FillWeight = 45;
        list.Columns["fund"]!.FillWeight = 125;
        list.Columns["model"]!.FillWeight = 125;
        list.Columns["dataset"]!.FillWeight = 85;
        foreach (var prediction in this.predictions)
        {
            list.Rows.Add(
                prediction.PredictionId,
                prediction.Origin,
                prediction.FundIdentifier,
                prediction.CutoffDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                prediction.ModelName,
                prediction.Horizon,
                QuantitativeFormatter.FormatReturn(prediction.PointForecast),
                QuantitativeFormatter.FormatReturn(prediction.ExpectedReturn),
                QuantitativeFormatter.FormatPercentShort(prediction.ProbabilityPositive),
                QuantitativeFormatter.FormatDate(prediction.TargetDate),
                QuantitativeFormatter.FormatFingerprint(prediction.DatasetFingerprint));
        }

        if (list.Rows.Count > 0)
        {
            list.ClearSelection();
            list.CurrentCell = list.Rows[0].Cells[0];
            list.Rows[0].Selected = true;
        }
    }

    private async Task UpdateDetailsAsync()
    {
        var list = this.listCard.Grid;
        if (list.CurrentRow is null || list.CurrentRow.Index < 0 || list.CurrentRow.Index >= this.predictions.Count)
        {
            return;
        }

        var selected = this.predictions[list.CurrentRow.Index];
        if (this.application is null)
        {
            return;
        }

        var detailsResult = await this.application.GetPredictionDetailsAsync(selected.PredictionId).ConfigureAwait(true);
        if (detailsResult is null)
        {
            return;
        }

        var prediction = detailsResult.Prediction.Prediction;
        var rows = new List<(string Name, string Value)>
        {
            ("PREDICTION", string.Empty),
            ("Prediction ID", prediction.PredictionId.ToString()),
            ("Logical key", detailsResult.Prediction.LogicalKey),
            ("Content fingerprint", QuantitativeFormatter.FormatFingerprint(detailsResult.Prediction.ContentFingerprint, 18)),
            ("Origin", detailsResult.Prediction.Origin.ToString()),
            ("Fund", prediction.FundIdentifier.ToString()),
            ("Dataset fingerprint", QuantitativeFormatter.FormatFingerprint(prediction.DatasetIdentity.DatasetFingerprintSha256, 18)),
            ("MODEL", string.Empty),
            ("Model", prediction.Model.Name),
            ("Version", prediction.Model.Version),
            ("Configuration fingerprint", QuantitativeFormatter.FormatFingerprint(detailsResult.Prediction.ModelConfigurationFingerprint, 18)),
            ("State schema fingerprint", QuantitativeFormatter.FormatFingerprint(prediction.StateSchemaFingerprint, 18)),
            ("Cutoff", prediction.DataCutoffDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            ("Horizon", prediction.RequestedHorizon.ToString()),
            ("Resolution", prediction.HorizonResolution.ResolutionPolicyName),
            ("Target", QuantitativeFormatter.FormatDate(detailsResult.Prediction.TargetDate)),
            ("Point statistic", prediction.PointForecastStatistic.ToString()),
            ("Point forecast", QuantitativeFormatter.FormatCapabilityReturn(prediction.ForecastCapabilities, ForecastCapabilities.PointForecast, prediction.PointForecastReturn)),
            ("Expected return", QuantitativeFormatter.FormatCapabilityReturn(prediction.ForecastCapabilities, ForecastCapabilities.ExpectedReturn, prediction.ExpectedReturn)),
            ("Median", QuantitativeFormatter.FormatCapabilityReturn(prediction.ForecastCapabilities, ForecastCapabilities.Median, prediction.MedianReturn)),
            ("ProbabilityPositive", prediction.Supports(ForecastCapabilities.ProbabilityPositive) ? QuantitativeFormatter.FormatPercentShort(prediction.ProbabilityPositive) : "N/A"),
            ("Quantiles", prediction.ReturnPercentiles.Count == 0 ? "N/A" : string.Join(", ", prediction.ReturnPercentiles.Select(pair => $"{pair.Key}%={QuantitativeFormatter.FormatReturn(pair.Value)}"))),
            ("EVALUATION", string.Empty),
        };
        foreach (var evaluation in detailsResult.Evaluations)
        {
            rows.Add(("Evaluation fingerprint", QuantitativeFormatter.FormatFingerprint(evaluation.EvaluationContentFingerprint, 18)));
            rows.Add(("Direction rule", evaluation.DirectionRule.ToString()));
            rows.Add(("Actual return", QuantitativeFormatter.FormatReturn(evaluation.ActualReturn)));
            rows.Add(("Absolute error", prediction.Supports(ForecastCapabilities.PointForecast) ? QuantitativeFormatter.FormatReturn(evaluation.AbsoluteError) : "N/A"));
            rows.Add(("Direction correct", QuantitativeFormatter.FormatYesNo(evaluation.DirectionCorrect)));
            rows.Add(("Brier contribution", prediction.Supports(ForecastCapabilities.ProbabilityPositive) ? QuantitativeFormatter.FormatScore(evaluation.BrierContribution) : "N/A"));
        }

        GridFactory.SetNameValueRows(this.detailsCard.Grid, rows);
    }
}
