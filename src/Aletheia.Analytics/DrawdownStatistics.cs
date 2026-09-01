namespace Aletheia.Analytics;

/// <summary>
/// Summarizes drawdown-path severity and duration.
/// </summary>
/// <param name="AverageDrawdown">The average negative drawdown across underwater observations.</param>
/// <param name="AverageDurationDays">The average completed underwater duration in calendar days.</param>
/// <param name="MaximumDurationDays">The maximum underwater duration in calendar days.</param>
/// <param name="CompletedDrawdownCount">The number of completed drawdown episodes.</param>
public sealed record DrawdownStatistics(
    double AverageDrawdown,
    double AverageDurationDays,
    int MaximumDurationDays,
    int CompletedDrawdownCount);
