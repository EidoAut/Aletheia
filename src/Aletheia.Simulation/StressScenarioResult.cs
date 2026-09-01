namespace Aletheia.Simulation;

/// <summary>
/// Stores a deterministic stress scenario outcome.
/// </summary>
/// <param name="Name">The scenario name.</param>
/// <param name="PeakLoss">The peak loss during the scenario.</param>
/// <param name="TerminalReturn">The terminal simple return.</param>
/// <param name="Diagnostic">The scenario diagnostic.</param>
/// <param name="StartDate">The start date for historical scenarios, when applicable.</param>
/// <param name="EndDate">The end date for historical scenarios, when applicable.</param>
/// <param name="WindowLengthObservations">The effective window length, when applicable.</param>
/// <param name="SelectionCriterion">The deterministic selection criterion, when applicable.</param>
public sealed record StressScenarioResult(
    string Name,
    double PeakLoss,
    double TerminalReturn,
    string Diagnostic,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? WindowLengthObservations = null,
    string? SelectionCriterion = null);
