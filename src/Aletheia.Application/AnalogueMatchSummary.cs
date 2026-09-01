namespace Aletheia.Application;

/// <summary>
/// Summarizes one historical analogue match.
/// </summary>
public sealed record AnalogueMatchSummary(
    DateOnly Date,
    double Distance,
    double? Return30CalendarDays,
    double? Return90CalendarDays,
    double? Return180CalendarDays);
