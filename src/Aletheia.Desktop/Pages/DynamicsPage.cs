#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Core;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays dynamic-state features and state-space projections.
/// </summary>
internal sealed partial class DynamicsPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicsPage"/> class.
    /// </summary>
    public DynamicsPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Dynamics";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var analysis = workspace.Analysis;
        this.momentumVolatility.PlotStateScatter(
            "Momentum vs volatility",
            analysis.StateProjection,
            point => point.Momentum,
            point => point.Volatility,
            "Momentum",
            "Volatility");
        this.velocityAcceleration.PlotStateScatter(
            "Velocity vs acceleration",
            analysis.StateProjection,
            point => point.Velocity,
            point => point.Acceleration,
            "Log-NAV velocity / observation",
            "Log-NAV acceleration / observation²");
        GridFactory.SetNameValueRows(this.stateCard.Grid, [
            ("State date", analysis.CurrentState.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            ("Simple return", QuantitativeFormatter.FormatReturn(GetStateValue(analysis.CurrentState, StandardStateDimensions.SimpleReturn))),
            ("Log return", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.LogReturn))),
            ("Trend", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.Trend))),
            ("Momentum", QuantitativeFormatter.FormatReturn(GetStateValue(analysis.CurrentState, StandardStateDimensions.Momentum))),
            ("Volatility", QuantitativeFormatter.FormatReturn(GetStateValue(analysis.CurrentState, StandardStateDimensions.Volatility))),
            ("Drawdown", QuantitativeFormatter.FormatReturn(GetStateValue(analysis.CurrentState, StandardStateDimensions.Drawdown))),
            ("LogNavVelocityPerObservation", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.LogNavVelocityPerObservation))),
            ("LogNavAccelerationPerObservationSquared", QuantitativeFormatter.FormatNumber(GetStateValue(analysis.CurrentState, StandardStateDimensions.LogNavAccelerationPerObservationSquared))),
            ("Data adequacy", QuantitativeFormatter.FormatPercentShort(analysis.CurrentState.DataAdequacy)),
            ("State schema", analysis.CurrentState.Schema?.Id ?? "N/A"),
            ("State schema fingerprint", QuantitativeFormatter.FormatFingerprint(analysis.CurrentState.Schema?.Fingerprint)),
        ]);
    }

    private static double? GetStateValue(DynamicState state, StateDimension dimension)
    {
        return state.TryGetValue(dimension, out var value) ? value : null;
    }
}
