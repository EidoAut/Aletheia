#pragma warning disable SA1402 // The fitted result stays next to the hazard fitter that produces it.

using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Fits a discrete-time competing-risk hazard model for upside and downside events.
/// </summary>
public sealed class CompetingRiskHazardModel
{
    /// <summary>
    /// Fits hazards from realized triple-barrier outcomes.
    /// </summary>
    /// <param name="outcomes">Training outcomes.</param>
    /// <param name="horizon">The modeled horizon.</param>
    /// <returns>The fitted hazard model.</returns>
    public CompetingRiskHazardFit Fit(IReadOnlyList<TripleBarrierOutcome> outcomes, ForecastHorizon horizon)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        var steps = Math.Max(1, horizon.Value);
        if (outcomes.Count == 0)
        {
            return new CompetingRiskHazardFit(horizon, Array.Empty<EventHazardPoint>(), "No outcomes were available for hazard fitting.");
        }

        var hazardPoints = new List<EventHazardPoint>(steps);
        var survival = 1d;
        var cumulativeUp = 0d;
        var cumulativeDown = 0d;
        for (var step = 1; step <= steps; step++)
        {
            var atRisk = outcomes.Count(outcome => outcome.TimeToEvent >= step);
            var upAtStep = outcomes.Count(outcome =>
                outcome.Outcome == TripleBarrierOutcomeType.UpperHitFirst &&
                outcome.TimeToEvent == step);
            var downAtStep = outcomes.Count(outcome =>
                outcome.Outcome == TripleBarrierOutcomeType.LowerHitFirst &&
                outcome.TimeToEvent == step);
            var hazardUp = atRisk == 0 ? 0d : upAtStep / (double)atRisk;
            var hazardDown = atRisk == 0 ? 0d : downAtStep / (double)atRisk;
            cumulativeUp += survival * hazardUp;
            cumulativeDown += survival * hazardDown;
            hazardPoints.Add(new EventHazardPoint(
                step,
                hazardUp,
                hazardDown,
                survival,
                cumulativeUp,
                cumulativeDown));
            survival *= Math.Max(0d, 1d - hazardUp - hazardDown);
        }

        return new CompetingRiskHazardFit(horizon, hazardPoints, "Historical unconditional competing-risk hazards fitted.");
    }
}

/// <summary>
/// Stores a fitted competing-risk hazard model.
/// </summary>
/// <param name="Horizon">The modeled horizon.</param>
/// <param name="HazardPoints">The hazard points.</param>
/// <param name="Diagnostic">The fit diagnostic.</param>
public sealed record CompetingRiskHazardFit(
    ForecastHorizon Horizon,
    IReadOnlyList<EventHazardPoint> HazardPoints,
    string Diagnostic)
{
    /// <summary>
    /// Converts the hazard fit to a forecast summary.
    /// </summary>
    /// <returns>The competing-risk forecast.</returns>
    public CompetingRiskForecast Forecast()
    {
        if (this.HazardPoints.Count == 0)
        {
            return new CompetingRiskForecast(this.Horizon, Array.Empty<EventHazardPoint>(), 0d, 0d, 1d, null, null, 0d);
        }

        var last = this.HazardPoints[^1];
        var medianUp = this.HazardPoints.FirstOrDefault(point => point.CumulativeIncidenceUp >= 0.5d)?.Step;
        var medianDown = this.HazardPoints.FirstOrDefault(point => point.CumulativeIncidenceDown >= 0.5d)?.Step;
        var expected = this.HazardPoints.Sum(point =>
        {
            var eventProbabilityAtStep = point.Survival * (point.HazardUp + point.HazardDown);
            return point.Step * eventProbabilityAtStep;
        });
        expected += this.HazardPoints.Count * Math.Max(0d, 1d - last.CumulativeIncidenceUp - last.CumulativeIncidenceDown);
        return new CompetingRiskForecast(
            this.Horizon,
            this.HazardPoints,
            last.CumulativeIncidenceUp,
            last.CumulativeIncidenceDown,
            Math.Max(0d, 1d - last.CumulativeIncidenceUp - last.CumulativeIncidenceDown),
            medianUp == 0 ? null : medianUp,
            medianDown == 0 ? null : medianDown,
            expected);
    }
}
