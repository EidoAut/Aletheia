using Aletheia.Core;
using Aletheia.Dynamics;
using Aletheia.Forecasting;
using Aletheia.Simulation;
using Aletheia.Spectral;
using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Aggregates the quantitative research output for one fund.
/// </summary>
/// <param name="Dataset">The dataset summary.</param>
/// <param name="ScientificVersion">The scientific version.</param>
/// <param name="DataFreshness">The freshness assessment for the effective dataset.</param>
/// <param name="Performance">The performance and risk summary.</param>
/// <param name="CurrentState">The current causal state vector.</param>
/// <param name="RegimeModel">The fitted regime model, when available.</param>
/// <param name="SpectralEvidence">The dominant spectral component evidence, when available.</param>
/// <param name="Forecasts">The current model forecasts.</param>
/// <param name="Ensemble">The evidence-weighted ensemble, when available.</param>
/// <param name="StressScenarios">Deterministic stress scenarios.</param>
/// <param name="FundScore">The long-run fund quality score.</param>
/// <param name="CurrentAttractiveness">The current attractiveness assessment.</param>
/// <param name="DecisionSignal">The interpretive decision signal.</param>
/// <param name="Actionability">The overall actionable-confidence assessment.</param>
/// <param name="EnsembleAudit">Per-model ensemble audit entries.</param>
/// <param name="Warnings">Top-level warnings.</param>
/// <param name="Provenance">Deterministic provenance metadata.</param>
public sealed record FundResearchReport(
    DatasetSummary Dataset,
    string ScientificVersion,
    DataFreshnessAssessment DataFreshness,
    PerformanceSummary Performance,
    DynamicState CurrentState,
    GaussianHmmResult? RegimeModel,
    SpectralComponentEvidence? SpectralEvidence,
    ForecastCollectionResult Forecasts,
    ForecastEnsembleResult? Ensemble,
    IReadOnlyList<StressScenarioResult> StressScenarios,
    FundScore FundScore,
    CurrentOpportunityAssessment CurrentAttractiveness,
    DecisionSignal DecisionSignal,
    ActionabilityAssessment Actionability,
    IReadOnlyList<ForecastEnsembleAuditEntry> EnsembleAudit,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string> Provenance)
{
    /// <summary>
    /// Gets the most probable current regime label, when available.
    /// </summary>
    public string? CurrentRegimeLabel
    {
        get
        {
            if (this.RegimeModel is null || this.RegimeModel.States.Count == 0 || this.RegimeModel.LatestProbabilities.Count == 0)
            {
                return null;
            }

            var bestIndex = 0;
            var bestProbability = this.RegimeModel.LatestProbabilities[0];
            for (var index = 1; index < this.RegimeModel.LatestProbabilities.Count; index++)
            {
                if (this.RegimeModel.LatestProbabilities[index] > bestProbability)
                {
                    bestIndex = index;
                    bestProbability = this.RegimeModel.LatestProbabilities[index];
                }
            }

            return this.RegimeModel.States[bestIndex].Label;
        }
    }

    /// <summary>
    /// Gets the most probable current regime probability, when available.
    /// </summary>
    public double? CurrentRegimeProbability => this.RegimeModel is null || this.RegimeModel.LatestProbabilities.Count == 0
        ? null
        : this.RegimeModel.LatestProbabilities.Max();

    /// <summary>
    /// Gets the preferred twelve-month forecast, when available.
    /// </summary>
    public ForecastDistribution? TwelveMonthForecast => this.Ensemble?.Distribution is { RequestedHorizon.Unit: ForecastHorizonUnit.CalendarDays, RequestedHorizon.Value: >= 360 } ensemble
        ? ensemble
        : this.Forecasts.Runs
        .Where(run => run.Distribution is not null &&
            run.RequestedHorizon.Unit == ForecastHorizonUnit.CalendarDays &&
            run.RequestedHorizon.Value >= 360)
        .Select(run => run.Distribution)
        .FirstOrDefault();
}
