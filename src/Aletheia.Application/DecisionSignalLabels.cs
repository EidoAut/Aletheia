using Aletheia.Validation;

namespace Aletheia.Application;

/// <summary>
/// Centralizes investor-facing labels for strategic and tactical signals.
/// </summary>
public static class DecisionSignalLabels
{
    /// <summary>
    /// Explains the meaning of the tentative marker.
    /// </summary>
    public const string TentativeMarkerExplanation =
        "A question mark means Aletheia has a directional estimate, but evidence is not strong enough for a fully validated current decision.";

    /// <summary>
    /// Explains the meaning of no call.
    /// </summary>
    public const string NoCallExplanation =
        "NO CALL means Aletheia cannot defend a directional conclusion from the available evidence.";

    /// <summary>
    /// Converts a direction and qualification into the visible investor label.
    /// </summary>
    /// <param name="direction">The directional signal.</param>
    /// <param name="qualification">The evidence qualification.</param>
    /// <returns>The display label.</returns>
    public static string ToDisplayLabel(DirectionalSignal direction, SignalQualification qualification)
    {
        if (direction == DirectionalSignal.None || qualification == SignalQualification.Unavailable)
        {
            return "NO CALL";
        }

        var label = direction switch
        {
            DirectionalSignal.Buy => "BUY",
            DirectionalSignal.Hold => "HOLD",
            DirectionalSignal.Sell => "SELL",
            _ => "NO CALL",
        };
        return qualification == SignalQualification.Tentative ? $"{label}?" : label;
    }

    /// <summary>
    /// Maps the legacy strategic action to the coarse investor direction.
    /// </summary>
    /// <param name="action">The legacy strategic action.</param>
    /// <returns>The coarse direction.</returns>
    public static DirectionalSignal ToDirection(DecisionSignalAction action)
    {
        return action switch
        {
            DecisionSignalAction.Accumulate or DecisionSignalAction.MildAccumulate => DirectionalSignal.Buy,
            DecisionSignalAction.Reduce or DecisionSignalAction.StrongReduce => DirectionalSignal.Sell,
            DecisionSignalAction.Neutral => DirectionalSignal.Hold,
            _ => DirectionalSignal.None,
        };
    }

    /// <summary>
    /// Maps the legacy timing action to the coarse investor direction.
    /// </summary>
    /// <param name="action">The legacy timing action.</param>
    /// <returns>The coarse direction.</returns>
    public static DirectionalSignal ToDirection(TimingDecisionAction action)
    {
        return action switch
        {
            TimingDecisionAction.StrongBuy or
                TimingDecisionAction.Buy or
                TimingDecisionAction.StrongAccumulate or
                TimingDecisionAction.Accumulate or
                TimingDecisionAction.WatchPositive => DirectionalSignal.Buy,
            TimingDecisionAction.Reduce or
                TimingDecisionAction.Sell or
                TimingDecisionAction.StrongReduce or
                TimingDecisionAction.WatchNegative => DirectionalSignal.Sell,
            TimingDecisionAction.Hold or TimingDecisionAction.Neutral => DirectionalSignal.Hold,
            _ => DirectionalSignal.None,
        };
    }

    /// <summary>
    /// Maps a deterministic actionability status to a structured level.
    /// </summary>
    /// <param name="status">The actionability status.</param>
    /// <returns>The structured level.</returns>
    public static SignalActionabilityLevel ToActionabilityLevel(string status)
    {
        return status switch
        {
            "QualifiedActionable" => SignalActionabilityLevel.Actionable,
            "CurrentDecisionUnavailable" or "NoDefensibleCurrentSignal" => SignalActionabilityLevel.Unavailable,
            _ => SignalActionabilityLevel.Caution,
        };
    }
}
