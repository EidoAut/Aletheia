namespace Aletheia.Validation;

/// <summary>
/// Calculates the timing ReliabilityIndex from explicit evidence and penalty factors.
/// </summary>
public static class ReliabilityIndexCalculator
{
    /// <summary>
    /// Calculates a normalized reliability index in [0, 1].
    /// </summary>
    /// <param name="effectiveOosSampleCount">The effective same-horizon OOS sample count.</param>
    /// <param name="targetOosSampleCount">The sample count at which evidence reaches full strength.</param>
    /// <param name="expectedCalibrationError">The probability calibration error.</param>
    /// <param name="predictiveSkill">The same-horizon predictive skill versus baseline.</param>
    /// <param name="skillIntervalWidth">The bootstrap interval width for predictive skill.</param>
    /// <param name="temporalInstability">A non-negative temporal instability penalty.</param>
    /// <param name="weightDiversification">A concentration/diversity factor in [0, 1].</param>
    /// <param name="modelDisagreement">The model-disagreement magnitude.</param>
    /// <param name="oodDistance">The current robust OOD distance.</param>
    /// <param name="oodThreshold">The OOD threshold.</param>
    /// <returns>The reliability index.</returns>
    public static double Calculate(
        int effectiveOosSampleCount,
        int targetOosSampleCount,
        double expectedCalibrationError,
        double predictiveSkill,
        double skillIntervalWidth,
        double temporalInstability,
        double weightDiversification,
        double modelDisagreement,
        double oodDistance,
        double oodThreshold)
    {
        var sampleEvidenceFactor = targetOosSampleCount <= 0
            ? 0d
            : Math.Clamp(effectiveOosSampleCount / (double)targetOosSampleCount, 0d, 1d);
        var calibrationFactor = 1d - Math.Clamp(expectedCalibrationError, 0d, 1d);
        var skillFactor = Math.Clamp(1d - Math.Exp(-25d * Math.Max(0d, predictiveSkill)), 0d, 1d);
        var uncertaintyFactor = 1d / (1d + Math.Max(0d, skillIntervalWidth * 20d));
        var stabilityFactor = 1d / (1d + Math.Max(0d, temporalInstability));
        var diversityFactor = Math.Clamp(weightDiversification, 0d, 1d);
        var disagreementFactor = 1d / (1d + Math.Max(0d, modelDisagreement));
        var oodFactor = CalculateOodFactor(oodDistance, oodThreshold);

        return Math.Clamp(
            sampleEvidenceFactor *
            calibrationFactor *
            skillFactor *
            uncertaintyFactor *
            stabilityFactor *
            diversityFactor *
            disagreementFactor *
            oodFactor,
            0d,
            1d);
    }

    private static double CalculateOodFactor(double oodDistance, double oodThreshold)
    {
        if (!double.IsFinite(oodDistance))
        {
            return 0d;
        }

        if (!double.IsFinite(oodThreshold) || oodThreshold <= 0d)
        {
            return 0d;
        }

        var ratio = Math.Max(0d, oodDistance) / oodThreshold;
        return 1d / (1d + (ratio * ratio));
    }
}
