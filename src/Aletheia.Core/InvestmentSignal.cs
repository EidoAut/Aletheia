namespace Aletheia.Core;

/// <summary>
/// Represents the explainable signal vocabulary that Aletheia may emit after validation.
/// </summary>
/// <remarks>
/// Milestone 1 intentionally returns <see cref="NoReliableSignal"/> from the CLI.
/// Directional signals should not be produced until the validation and prediction
/// ledger can support them with out-of-sample evidence.
/// </remarks>
public enum InvestmentSignal
{
    /// <summary>
    /// No reliable signal can be justified.
    /// </summary>
    NoReliableSignal,

    /// <summary>
    /// The model evidence strongly favors reducing exposure.
    /// </summary>
    StrongSell,

    /// <summary>
    /// The model evidence favors selling.
    /// </summary>
    Sell,

    /// <summary>
    /// The model evidence favors reducing exposure without a full exit.
    /// </summary>
    Reduce,

    /// <summary>
    /// The model evidence favors holding the current exposure.
    /// </summary>
    Hold,

    /// <summary>
    /// The model evidence favors gradual accumulation.
    /// </summary>
    Accumulate,

    /// <summary>
    /// The model evidence favors buying.
    /// </summary>
    Buy,

    /// <summary>
    /// The model evidence strongly favors buying.
    /// </summary>
    StrongBuy,
}
