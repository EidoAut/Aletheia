using Aletheia.Dynamics;

namespace Aletheia.Application;

/// <summary>
/// Groups historical analogue diagnostics, outcomes, and chart paths.
/// </summary>
public sealed record AnalogueAnalysisResult(
    HistoricalAnalogueSearchResult Search,
    IReadOnlyList<AnalogueMatchSummary> Matches,
    IReadOnlyList<AnaloguePath> Paths,
    IReadOnlyList<AnalogueAggregatePoint> AggregatePath,
    AnalogueOutcomeSummary Outcome30CalendarDays,
    AnalogueOutcomeSummary Outcome90CalendarDays,
    AnalogueOutcomeSummary Outcome180CalendarDays);
