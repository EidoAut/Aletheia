namespace Aletheia.Validation;

/// <summary>
/// Configures probability calibration diagnostics.
/// </summary>
public sealed class CalibrationOptions
{
    /// <summary>
    /// Gets or sets the number of equal-width bins on the probability interval [0, 1].
    /// </summary>
    public int BinCount { get; set; } = 10;

    /// <summary>
    /// Validates the calibration configuration.
    /// </summary>
    public void Validate()
    {
        if (this.BinCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.BinCount), this.BinCount, "Calibration bin count must be positive.");
        }
    }
}
