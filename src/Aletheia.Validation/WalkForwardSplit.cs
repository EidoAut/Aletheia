namespace Aletheia.Validation;

/// <summary>
/// Describes one walk-forward train/test split.
/// </summary>
public sealed class WalkForwardSplit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WalkForwardSplit"/> class.
    /// </summary>
    /// <param name="trainStartIndex">The inclusive training start index.</param>
    /// <param name="trainEndIndex">The inclusive training end index.</param>
    /// <param name="testStartIndex">The inclusive test start index.</param>
    /// <param name="testEndIndex">The inclusive test end index.</param>
    /// <param name="predictionCutoffIndex">The observation index available to the prediction.</param>
    /// <param name="targetIndex">The target observation index, when known.</param>
    /// <param name="predictionCutoffDate">The observation date available to the prediction, when known.</param>
    /// <param name="targetDate">The target date, when known.</param>
    public WalkForwardSplit(
        int trainStartIndex,
        int trainEndIndex,
        int testStartIndex,
        int testEndIndex,
        int predictionCutoffIndex,
        int? targetIndex = null,
        DateOnly? predictionCutoffDate = null,
        DateOnly? targetDate = null)
    {
        this.TrainStartIndex = trainStartIndex;
        this.TrainEndIndex = trainEndIndex;
        this.TestStartIndex = testStartIndex;
        this.TestEndIndex = testEndIndex;
        this.PredictionCutoffIndex = predictionCutoffIndex;
        this.TargetIndex = targetIndex;
        this.PredictionCutoffDate = predictionCutoffDate;
        this.TargetDate = targetDate;
    }

    /// <summary>
    /// Gets the inclusive training start index.
    /// </summary>
    public int TrainStartIndex { get; }

    /// <summary>
    /// Gets the inclusive training end index.
    /// </summary>
    public int TrainEndIndex { get; }

    /// <summary>
    /// Gets the inclusive test start index.
    /// </summary>
    public int TestStartIndex { get; }

    /// <summary>
    /// Gets the inclusive test end index.
    /// </summary>
    public int TestEndIndex { get; }

    /// <summary>
    /// Gets the observation index available to the prediction.
    /// </summary>
    public int PredictionCutoffIndex { get; }

    /// <summary>
    /// Gets the target observation index, when known.
    /// </summary>
    public int? TargetIndex { get; }

    /// <summary>
    /// Gets the observation date available to the prediction, when known.
    /// </summary>
    public DateOnly? PredictionCutoffDate { get; }

    /// <summary>
    /// Gets the target date, when known.
    /// </summary>
    public DateOnly? TargetDate { get; }
}
