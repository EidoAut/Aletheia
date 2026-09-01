using Aletheia.Core;

namespace Aletheia.Simulation;

/// <summary>
/// Builds deterministic adverse scenarios from historical log returns.
/// </summary>
public sealed class StressScenarioAnalyzer
{
    /// <summary>
    /// Replays the historically worst contiguous log-return drawdown over the requested horizon length.
    /// </summary>
    /// <param name="historicalLogReturns">The finite historical log returns.</param>
    /// <param name="horizonObservations">The positive horizon length.</param>
    /// <returns>The stress scenario result.</returns>
    public StressScenarioResult HistoricalWorstWindow(
        IReadOnlyList<double> historicalLogReturns,
        int horizonObservations)
    {
        ArgumentNullException.ThrowIfNull(historicalLogReturns);
        Validate(historicalLogReturns, horizonObservations);

        var effectiveHorizon = Math.Min(horizonObservations, historicalLogReturns.Count);
        var worstReturn = double.PositiveInfinity;
        for (var start = 0; start <= historicalLogReturns.Count - effectiveHorizon; start++)
        {
            var cumulative = 0d;
            for (var offset = 0; offset < effectiveHorizon; offset++)
            {
                cumulative += historicalLogReturns[start + offset];
            }

            worstReturn = Math.Min(worstReturn, Math.Exp(cumulative) - 1d);
        }

        return new StressScenarioResult(
            "Historical worst contiguous window",
            Math.Min(0d, worstReturn),
            worstReturn,
            "Deterministic replay of the worst historical contiguous window; not a probability estimate.",
            WindowLengthObservations: effectiveHorizon,
            SelectionCriterion: "Minimum terminal return over a fixed effective-observation window.");
    }

    /// <summary>
    /// Replays the historically worst contiguous NAV window over the requested horizon length.
    /// </summary>
    /// <param name="navSeries">The finite historical NAV observations.</param>
    /// <param name="horizonObservations">The positive horizon length.</param>
    /// <returns>The stress scenario result.</returns>
    public StressScenarioResult HistoricalWorstWindow(
        NavSeries navSeries,
        int horizonObservations)
    {
        ArgumentNullException.ThrowIfNull(navSeries);
        if (navSeries.Count < 2)
        {
            throw new ArgumentException("At least two NAV observations are required.", nameof(navSeries));
        }

        Validate(navSeries.Points.Select(point => Math.Log((double)point.Value)).ToArray(), Math.Min(horizonObservations, navSeries.Count - 1));

        var effectiveHorizon = Math.Min(horizonObservations, navSeries.Count - 1);
        var best = default(WindowCandidate);
        var found = false;
        for (var start = 0; start <= navSeries.Count - effectiveHorizon - 1; start++)
        {
            var end = start + effectiveHorizon;
            var candidate = BuildWindowCandidate(navSeries, start, end);
            if (!found ||
                candidate.TerminalReturn < best.TerminalReturn ||
                (candidate.TerminalReturn == best.TerminalReturn && candidate.PeakLoss < best.PeakLoss))
            {
                best = candidate;
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException("No historical window could be selected.");
        }

        return new StressScenarioResult(
            "Historical worst contiguous window",
            best.PeakLoss,
            best.TerminalReturn,
            "Deterministic replay of the worst historical contiguous window; peak loss is intra-window drawdown, not terminal return.",
            best.StartDate,
            best.EndDate,
            effectiveHorizon,
            "Minimum terminal return over a fixed effective-observation window.");
    }

    /// <summary>
    /// Applies an instantaneous return shock followed by zero returns.
    /// </summary>
    /// <param name="shockReturn">The simple return shock.</param>
    /// <returns>The stress scenario result.</returns>
    public StressScenarioResult ReturnShock(double shockReturn)
    {
        if (!double.IsFinite(shockReturn) || shockReturn <= -1d)
        {
            throw new ArgumentOutOfRangeException(nameof(shockReturn), shockReturn, "Shock return must be finite and greater than -100%.");
        }

        return new StressScenarioResult(
            "Instant return shock",
            Math.Min(0d, shockReturn),
            shockReturn,
            "Deterministic instantaneous shock; not a historical probability estimate.");
    }

    /// <summary>
    /// Applies repeated adverse returns equal to a configured loss.
    /// </summary>
    /// <param name="monthlyLoss">The repeated monthly simple loss.</param>
    /// <param name="months">The number of months.</param>
    /// <returns>The stress scenario result.</returns>
    public StressScenarioResult ProlongedBear(double monthlyLoss = -0.03d, int months = 12)
    {
        if (!double.IsFinite(monthlyLoss) || monthlyLoss <= -1d || monthlyLoss > 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyLoss), monthlyLoss, "Monthly loss must be finite and in (-1, 0].");
        }

        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), months, "Month count must be positive.");
        }

        var terminal = Math.Pow(1d + monthlyLoss, months) - 1d;
        return new StressScenarioResult(
            "Prolonged bear regime",
            Math.Min(0d, terminal),
            terminal,
            "Configurable adverse path; not a calibrated regime probability.");
    }

    private static void Validate(IReadOnlyList<double> historicalLogReturns, int horizonObservations)
    {
        if (horizonObservations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonObservations), horizonObservations, "Horizon must be positive.");
        }

        if (historicalLogReturns.Count == 0)
        {
            throw new ArgumentException("At least one historical log return is required.", nameof(historicalLogReturns));
        }

        for (var index = 0; index < historicalLogReturns.Count; index++)
        {
            if (!double.IsFinite(historicalLogReturns[index]))
            {
                throw new ArgumentException("Stress scenarios require finite log returns.", nameof(historicalLogReturns));
            }
        }
    }

    private static WindowCandidate BuildWindowCandidate(NavSeries navSeries, int start, int end)
    {
        var startValue = (double)navSeries[start].Value;
        if (startValue <= 0d)
        {
            throw new ArgumentException("Historical stress windows require strictly positive NAV values.", nameof(navSeries));
        }

        var peak = startValue;
        var peakLoss = 0d;
        for (var index = start + 1; index <= end; index++)
        {
            var value = (double)navSeries[index].Value;
            if (value <= 0d)
            {
                throw new ArgumentException("Historical stress windows require strictly positive NAV values.", nameof(navSeries));
            }

            if (value > peak)
            {
                peak = value;
            }

            peakLoss = Math.Min(peakLoss, (value / peak) - 1d);
        }

        var terminalReturn = ((double)navSeries[end].Value / startValue) - 1d;
        return new WindowCandidate(
            navSeries[start].Date,
            navSeries[end].Date,
            peakLoss,
            terminalReturn);
    }

    private readonly record struct WindowCandidate(
        DateOnly StartDate,
        DateOnly EndDate,
        double PeakLoss,
        double TerminalReturn);
}
