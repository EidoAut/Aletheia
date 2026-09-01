using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Converts timing-domain results into deterministic natural-language explanations.
/// </summary>
public sealed class PredictionExplanationBuilder
{
    /// <summary>
    /// Builds a narrative for a market-timing assessment.
    /// </summary>
    /// <param name="primary">The primary horizon assessment.</param>
    /// <param name="decision">The timing decision.</param>
    /// <param name="warnings">Warnings.</param>
    /// <returns>The narrative.</returns>
    public MarketTimingNarrative Build(
        MarketTimingHorizonAssessment? primary,
        TimingDecision decision,
        IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (primary is null)
        {
            return new MarketTimingNarrative(
                "Aletheia cannot identify a statistically supported timing view.",
                "There is not enough validated event evidence for a directional estimate.",
                "No supported event window is available.",
                "Risk cannot be separated from noise with the current data.",
                "Confidence is low because validation evidence is insufficient.",
                "No accumulation or reduction signal is active.");
        }

        var direction = primary.ProbabilityUp > primary.ProbabilityDown
            ? "a rise is currently estimated to be more likely than a fall"
            : primary.ProbabilityDown > primary.ProbabilityUp
                ? "a fall is currently estimated to be more likely than a rise"
                : "rise and fall probabilities are currently balanced";
        var summary = decision.Qualification == SignalQualification.Unavailable
            ? "Aletheia cannot issue a timing decision because the current evidence does not support a defensible call."
            : decision.Qualification == SignalQualification.Tentative
                ? $"Aletheia shows {decision.DisplayLabel}: a qualified timing view, not a fully validated action."
                : decision.Direction == DirectionalSignal.Hold
                    ? "Aletheia currently sees no clear timing advantage."
                    : $"Aletheia currently sees a {decision.Confidence.ToString().ToLowerInvariant()}-confidence {ReadableAction(decision.Action)} setup.";
        var timing = primary.MedianTimeToUp ?? primary.MedianTimeToDown;
        var timingExplanation = timing.HasValue
            ? $"The expected significant-move window is centered around roughly {timing.Value} sessions."
            : "No median event time is available because the event probability does not reach 50%.";
        return new MarketTimingNarrative(
            summary,
            $"Over {primary.Horizon}, {direction}.",
            timingExplanation,
            $"Estimated downside-event probability is {primary.ProbabilityDown:P0}; ReliabilityIndex is {primary.ReliabilityIndex:P0} and is not a hit probability.",
            BuildConfidenceExplanation(primary, decision, warnings),
            BuildActionExplanation(decision));
    }

    /// <summary>
    /// Builds positive and negative evidence text.
    /// </summary>
    /// <param name="primary">The primary horizon assessment.</param>
    /// <param name="arena">The underlying arena result.</param>
    /// <returns>Evidence and counter-evidence.</returns>
    public (IReadOnlyList<string> Evidence, IReadOnlyList<string> CounterEvidence) BuildWhy(
        MarketTimingHorizonAssessment? primary,
        MarketTimingArenaResult? arena)
    {
        if (primary is null || arena is null)
        {
            return (
                Array.Empty<string>(),
                ["No validated timing horizon is available."]);
        }

        var evidence = new List<string>
        {
            $"{primary.ProbabilityUp:P0} probability of the upside barrier being reached first.",
            $"Model agreement is {primary.ModelAgreement:P0}.",
            $"Out-of-sample evidence is {primary.EvidenceStrength}.",
        };
        if (primary.ForecastExpectedReturn.HasValue)
        {
            evidence.Add($"Forecast expected terminal return is {primary.ForecastExpectedReturn.Value:P1}.");
        }

        var risks = new List<string>
        {
            $"Downside-event probability is still {primary.ProbabilityDown:P0}.",
        };
        if (arena.Ensemble.Components.Count > 0)
        {
            evidence.Add($"{arena.Ensemble.Components.Count} validated timing model(s) entered the ensemble.");
        }
        else
        {
            risks.Add($"No model entered the ensemble: {arena.Ensemble.FallbackReason}");
        }

        if (primary.Reliability < 0.6d)
        {
            risks.Add("ReliabilityIndex is not high enough to classify this as a strong timing signal.");
        }

        if (arena.OutOfDistribution.OutOfDistribution)
        {
            risks.Add("Current conditions are unusual relative to historical feature support.");
        }

        return (evidence, risks);
    }

    private static string BuildConfidenceExplanation(
        MarketTimingHorizonAssessment primary,
        TimingDecision decision,
        IReadOnlyList<string> warnings)
    {
        if (warnings.Count > 0)
        {
            return $"Confidence is {decision.Confidence} because ReliabilityIndex is {primary.ReliabilityIndex:P0} and warnings are present.";
        }

        return $"Confidence is {decision.Confidence} based on validation evidence, calibration, ReliabilityIndex and model agreement.";
    }

    private static string BuildActionExplanation(TimingDecision decision)
    {
        if (decision.Qualification == SignalQualification.Unavailable)
        {
            return DecisionSignalLabels.NoCallExplanation;
        }

        if (decision.Qualification == SignalQualification.Tentative)
        {
            return decision.Direction == DirectionalSignal.Hold
                ? "The setup is balanced, but validation caveats keep HOLD tentative."
                : $"{decision.DisplayLabel} means the directional estimate is visible, but the evidence is not strong enough for a fully validated current decision.";
        }

        return decision.Action switch
        {
            TimingDecisionAction.InsufficientEvidence => "Aletheia abstains because the available out-of-sample evidence is not sufficient for this timing horizon.",
            TimingDecisionAction.StrongBuy or TimingDecisionAction.StrongAccumulate => "For a new contribution, conditions are unusually favorable, but this is still research output.",
            TimingDecisionAction.Buy or TimingDecisionAction.Accumulate => "For a new contribution, conditions are more favorable than neutral.",
            TimingDecisionAction.Hold or TimingDecisionAction.WatchPositive => "The setup is not strong enough to justify changing exposure.",
            TimingDecisionAction.WatchNegative => "The setup calls for caution rather than new risk.",
            TimingDecisionAction.Reduce => "For an existing position, evidence favors reducing exposure.",
            TimingDecisionAction.Sell or TimingDecisionAction.StrongReduce => "For an existing position, evidence strongly favors reducing exposure.",
            _ => "Aletheia would not change exposure based on the current evidence.",
        };
    }

    private static string ReadableAction(TimingDecisionAction action)
    {
        return action switch
        {
            TimingDecisionAction.StrongBuy or TimingDecisionAction.StrongAccumulate => "strong buy",
            TimingDecisionAction.Buy or TimingDecisionAction.Accumulate => "buy",
            TimingDecisionAction.Hold or TimingDecisionAction.WatchPositive => "hold",
            TimingDecisionAction.WatchNegative => "caution",
            TimingDecisionAction.Reduce => "reduction",
            TimingDecisionAction.Sell or TimingDecisionAction.StrongReduce => "sell",
            TimingDecisionAction.InsufficientEvidence => "insufficient-evidence",
            _ => "neutral",
        };
    }
}
