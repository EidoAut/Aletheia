#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays realized performance charts.
/// </summary>
internal sealed partial class PerformancePage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformancePage"/> class.
    /// </summary>
    public PerformancePage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Performance";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var analysis = workspace.Analysis;
        this.metrics.SetMetrics([
            ("CAGR", QuantitativeFormatter.FormatReturn(analysis.Performance.Cagr), ResolveReturnColor(analysis.Performance.Cagr)),
            ("Cumulative return", QuantitativeFormatter.FormatReturn(analysis.Performance.CumulativeReturn), ResolveReturnColor(analysis.Performance.CumulativeReturn)),
            ("Lag-1 autocorr.", QuantitativeFormatter.FormatNumber(analysis.Performance.Lag1Autocorrelation), ThemePalette.AccentSecondary),
        ]);
        this.navChart.PlotLine("NAV", analysis.Nav, "NAV");
        this.cumulativeChart.PlotLine("Cumulative return", analysis.CumulativeReturn, "Return");
        this.returnsChart.PlotLines("Simple and log returns", [("Simple", analysis.SimpleReturns), ("Log", analysis.LogReturns)], "Return");
        this.rollingReturnChart.PlotLine("Rolling return", analysis.RollingReturn, "Return");
    }

    private static Color? ResolveReturnColor(double? value)
    {
        return value.HasValue ? value.Value >= 0d ? ThemePalette.Positive : ThemePalette.Negative : null;
    }
}
