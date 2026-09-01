namespace Aletheia.Validation;

/// <summary>
/// Evaluates forecast models under chronological walk-forward rules.
/// </summary>
public interface IWalkForwardEvaluator
{
    /// <summary>
    /// Evaluates one model against one dataset.
    /// </summary>
    /// <param name="model">The model to evaluate.</param>
    /// <param name="dataset">The evaluation dataset.</param>
    /// <param name="options">The evaluation options.</param>
    /// <param name="ledger">The optional prediction ledger.</param>
    /// <param name="cancellationToken">A token used to cancel long-running validation.</param>
    /// <returns>The model evaluation result.</returns>
    Task<WalkForwardModelEvaluationResult> EvaluateAsync(
        IForecastModel model,
        ForecastEvaluationDataset dataset,
        WalkForwardEvaluationOptions options,
        IPredictionLedger? ledger = null,
        CancellationToken cancellationToken = default);
}
