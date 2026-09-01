using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Contains only information available inside one walk-forward training window.
/// </summary>
public sealed class ForecastTrainingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastTrainingContext"/> class.
    /// </summary>
    /// <param name="dataset">The evaluation dataset identity and fund metadata.</param>
    /// <param name="trainingSeries">The active training series ending at the prediction cutoff.</param>
    /// <param name="split">The split being evaluated.</param>
    /// <param name="horizonResolution">The resolved forecast horizon at the cutoff.</param>
    public ForecastTrainingContext(
        ForecastEvaluationDataset dataset,
        NavSeries trainingSeries,
        WalkForwardSplit split,
        ForecastHorizonResolution horizonResolution)
    {
        this.Dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        this.TrainingSeries = trainingSeries ?? throw new ArgumentNullException(nameof(trainingSeries));
        this.Split = split ?? throw new ArgumentNullException(nameof(split));
        this.HorizonResolution = horizonResolution ?? throw new ArgumentNullException(nameof(horizonResolution));
    }

    /// <summary>
    /// Gets the evaluation dataset identity and fund metadata.
    /// </summary>
    public ForecastEvaluationDataset Dataset { get; }

    /// <summary>
    /// Gets the training observations available to the model.
    /// </summary>
    public NavSeries TrainingSeries { get; }

    /// <summary>
    /// Gets the split being evaluated.
    /// </summary>
    public WalkForwardSplit Split { get; }

    /// <summary>
    /// Gets the resolved forecast horizon.
    /// </summary>
    public ForecastHorizonResolution HorizonResolution { get; }
}
