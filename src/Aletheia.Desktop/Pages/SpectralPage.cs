#pragma warning disable SA1505 // Existing designer-backed page spacing is kept stable.

using Aletheia.Application;
using Aletheia.Desktop.Controls;
using Aletheia.Desktop.Infrastructure;

namespace Aletheia.Desktop.Pages;

/// <summary>
/// Displays spectral analysis diagnostics.
/// </summary>
internal sealed partial class SpectralPage : WorkspacePageBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralPage"/> class.
    /// </summary>
    public SpectralPage()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    public override string PageTitle => "Spectral";

    /// <inheritdoc />
    public override void SetWorkspace(FundWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        var spectrum = workspace.Analysis.Spectrum;
        this.spectrumChart.PlotSpectrum(
            "Power spectrum",
            spectrum.Bins.Select(bin => bin.FrequencyCyclesPerObservation).ToArray(),
            spectrum.Bins.Select(bin => bin.Power).ToArray(),
            spectrum.DominantFrequency?.FrequencyCyclesPerObservation);
        GridFactory.SetNameValueRows(this.diagnosticsCard.Grid, [
            ("Dominant period", $"{QuantitativeFormatter.FormatNumber(spectrum.DominantFrequency?.PeriodObservations)} observations"),
            ("Frequency", QuantitativeFormatter.FormatNumber(spectrum.DominantFrequency?.FrequencyCyclesPerObservation)),
            ("Amplitude", QuantitativeFormatter.FormatNumber(spectrum.DominantFrequency?.Amplitude)),
            ("Power", QuantitativeFormatter.FormatNumber(spectrum.DominantFrequency?.Power)),
            ("Peak concentration", QuantitativeFormatter.FormatNumber(spectrum.PeakPowerFraction)),
            ("Peak / background", QuantitativeFormatter.FormatNumber(spectrum.PeakToBackgroundRatio)),
            ("Diagnostic strength", spectrum.DiagnosticStrength.ToString()),
            ("Rolling persistence", QuantitativeFormatter.FormatNumber(workspace.Analysis.SpectralStability.DominantPeriodPersistence)),
            ("TRANSFORM", string.Empty),
            ("Original samples", spectrum.OriginalSampleCount.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("FFT length", spectrum.TransformLength.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("Window", spectrum.Options.Window.ToString()),
            ("Detrending", spectrum.Options.DetrendingMode.ToString()),
            ("Coherent gain", QuantitativeFormatter.FormatNumber(spectrum.CoherentGain)),
            ("Zero padding", QuantitativeFormatter.FormatYesNo(spectrum.ZeroPaddingApplied)),
        ]);
    }
}
