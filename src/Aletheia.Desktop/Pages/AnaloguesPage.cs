#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays historical analogue diagnostics and paths.
/// </summary>
internal sealed partial class AnaloguesPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="AnaloguesPage"/> class.
    /// </summary>
    public AnaloguesPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Analogues";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var analogues = workspace.Analysis.Analogues;
        this.metrics.SetMetrics([
            ("Query date", workspace.Analysis.CurrentState.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), null),
            ("Candidates", analogues.Search.CandidateCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.AccentSecondary),
            ("Compatible", analogues.Search.SchemaCompatibleCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.Positive),
            ("Rejected schema", analogues.Search.RejectedSchemaIncompatibleCount.ToString(System.Globalization.CultureInfo.InvariantCulture), ThemePalette.Warning),
            ("Selected", analogues.Search.Matches.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), null),
            ("Median +90d", QuantitativeFormatter.FormatReturn(analogues.Outcome90CalendarDays.MedianReturn), ResolveReturnColor(analogues.Outcome90CalendarDays.MedianReturn)),
        ]);
        this.paths.PlotAnaloguePaths(analogues.Paths, analogues.AggregatePath);
        var matches = this.matchesCard.Grid;
        matches.Columns.Clear();
        matches.Rows.Clear();
        matches.Columns.Add("date", "Historical date");
        matches.Columns.Add("distance", "Distance");
        matches.Columns.Add("r30", "+30 calendar days");
        matches.Columns.Add("r90", "+90 calendar days");
        matches.Columns.Add("r180", "+180 calendar days");
        foreach (var match in analogues.Matches.Take(100))
        {
            matches.Rows.Add(
                match.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                QuantitativeFormatter.FormatNumber(match.Distance),
                QuantitativeFormatter.FormatReturn(match.Return30CalendarDays),
                QuantitativeFormatter.FormatReturn(match.Return90CalendarDays),
                QuantitativeFormatter.FormatReturn(match.Return180CalendarDays));
        }
    }

    private static Color? ResolveReturnColor(double? value)
    {
        return value.HasValue ? value.Value >= 0d ? ThemePalette.Positive : ThemePalette.Negative : null;
    }
}
