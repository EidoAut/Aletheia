using Aletheia.Core;

namespace Aletheia.Dynamics;

/// <summary>
/// Defines a model that estimates and forecasts dynamic state behaviour.
/// </summary>
public interface IDynamicModel
{
    /// <summary>
    /// Gets the model descriptor.
    /// </summary>
    DynamicModelDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the state dimensions required before forecasting.
    /// </summary>
    IReadOnlyList<StateDimension> RequiredStateDimensions { get; }

    /// <summary>
    /// Fits the model to historical observations.
    /// </summary>
    /// <param name="input">The model input.</param>
    /// <returns>The fitted result.</returns>
    DynamicModelResult Fit(DynamicModelInput input);

    /// <summary>
    /// Forecasts from the current state.
    /// </summary>
    /// <param name="currentState">The current reconstructed state.</param>
    /// <param name="horizon">The forecast horizon.</param>
    /// <returns>A dynamic forecast summary.</returns>
    DynamicForecast Forecast(DynamicState currentState, ForecastHorizon horizon);
}
