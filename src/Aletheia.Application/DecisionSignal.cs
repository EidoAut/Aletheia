using Aletheia.Core;

namespace Aletheia.Application;

/// <summary>
/// Stores an interpretive decision signal with evidence and caveats.
/// </summary>
/// <param name="Action">The signal category.</param>
/// <param name="Strength">The signal strength in [0, 1].</param>
/// <param name="Confidence">The confidence level.</param>
/// <param name="PrimaryHorizon">The primary horizon used by the signal.</param>
/// <param name="Evidence">Supporting evidence.</param>
/// <param name="CounterEvidence">Counter-evidence.</param>
/// <param name="Warnings">Warnings and caveats.</param>
public sealed record DecisionSignal(
    DecisionSignalAction Action,
    double Strength,
    ConfidenceLevel Confidence,
    ForecastHorizon? PrimaryHorizon,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> CounterEvidence,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets the investor-facing direction.
    /// </summary>
    public DirectionalSignal Direction { get; init; } = DirectionalSignal.None;

    /// <summary>
    /// Gets the validation qualification attached to the direction.
    /// </summary>
    public SignalQualification Qualification { get; init; } = SignalQualification.Unavailable;

    /// <summary>
    /// Gets directional or no-action support in [0, 1].
    /// </summary>
    public double DirectionalStrength { get; init; }

    /// <summary>
    /// Gets validation support in [0, 1].
    /// </summary>
    public double ValidationStrength { get; init; }

    /// <summary>
    /// Gets deterministic reasons for the label and qualification.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the visible investor label.
    /// </summary>
    public string DisplayLabel => DecisionSignalLabels.ToDisplayLabel(this.Direction, this.Qualification);

    /// <summary>
    /// Gets a value indicating whether the signal is qualified with a question mark.
    /// </summary>
    public bool IsTentative => this.Qualification == SignalQualification.Tentative;
}
