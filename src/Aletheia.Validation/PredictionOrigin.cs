namespace Aletheia.Validation;

/// <summary>
/// Distinguishes retrospectively simulated predictions from predictions emitted live.
/// </summary>
public enum PredictionOrigin
{
    /// <summary>
    /// The prediction was generated inside a historical walk-forward backtest.
    /// </summary>
    HistoricalWalkForward,

    /// <summary>
    /// The prediction was generated for live tracking using the information available at wall-clock time.
    /// </summary>
    Live,

    /// <summary>
    /// The prediction was imported from an external ledger.
    /// </summary>
    Imported,
}
