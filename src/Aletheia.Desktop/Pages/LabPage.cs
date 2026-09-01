#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays technical method metadata and analysis navigation context.
/// </summary>
internal sealed partial class LabPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="LabPage"/> class.
    /// </summary>
    public LabPage()
    {
        this.InitializeComponent();
        GridFactory.SetNameValueRows(this.sectionsCard.Grid, [
            ("TIME DOMAIN", "NAV, returns, rolling return and drawdown"),
            ("FREQUENCY DOMAIN", "FFT spectrum and rolling persistence"),
            ("STATE SPACE", "Momentum-volatility and velocity-acceleration projections"),
            ("FORECAST VALIDATION", "Common support, calibration, errors and immutable ledger"),
            ("DECISION ENGINE", "NO CALL when evidence cannot support a label"),
        ]);
    }

    /// <inheritdoc />
    public override string PageTitle => "Aletheia Lab";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
    }
}
