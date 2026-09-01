#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays risk and return distribution diagnostics.
/// </summary>
internal sealed partial class RiskPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskPage"/> class.
    /// </summary>
    public RiskPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Risk";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var analysis = workspace.Analysis;
        this.metrics.SetMetrics([
            ("Max drawdown", QuantitativeFormatter.FormatReturn(analysis.Performance.MaximumDrawdown.MaximumDrawdown), ThemePalette.Negative),
            ("Drawdown duration", $"{analysis.Performance.MaximumDrawdown.DurationDays} days", ThemePalette.Warning),
            ("Annualized volatility", QuantitativeFormatter.FormatReturn(analysis.Performance.AnnualizedVolatility), ThemePalette.AccentSecondary),
            ("Sharpe", QuantitativeFormatter.FormatNumber(analysis.Performance.SharpeRatio), null),
            ("Sortino", QuantitativeFormatter.FormatNumber(analysis.Performance.SortinoRatio), null),
        ]);
        this.drawdown.PlotLine("Drawdown", analysis.Drawdown, "Drawdown");
        this.volatility.PlotLine("Rolling volatility", analysis.RollingVolatility, "Annualized volatility");
        this.histogram.PlotHistogram("Return distribution", analysis.ReturnDistribution.Histogram);
        GridFactory.SetNameValueRows(this.statsCard.Grid, [
            ("Mean", QuantitativeFormatter.FormatReturn(analysis.ReturnDistribution.Mean)),
            ("Median", QuantitativeFormatter.FormatReturn(analysis.ReturnDistribution.Median)),
            ("Std. deviation", QuantitativeFormatter.FormatReturn(analysis.ReturnDistribution.StandardDeviation)),
            ("Minimum", QuantitativeFormatter.FormatReturn(analysis.ReturnDistribution.Minimum)),
            ("Maximum", QuantitativeFormatter.FormatReturn(analysis.ReturnDistribution.Maximum)),
        ]);
    }
}
