using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Configures out-of-sample walk-forward validation.
/// </summary>
public sealed class WalkForwardEvaluationOptions
{
    /// <summary>
    /// Gets or sets the minimum number of observations available before the first prediction.
    /// </summary>
    public int MinimumTrainingObservations { get; set; } = 500;

    /// <summary>
    /// Gets or sets the forecast horizon evaluated at every cutoff.
    /// </summary>
    public ForecastHorizon ForecastHorizon { get; set; } = ForecastHorizon.Observations(20);

    /// <summary>
    /// Gets or sets the number of observations advanced between prediction cutoffs.
    /// </summary>
    public int StepSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the training-window policy.
    /// </summary>
    public TrainingWindowMode WindowMode { get; set; } = TrainingWindowMode.Expanding;

    /// <summary>
    /// Gets or sets the fixed trailing observation count used by rolling windows.
    /// </summary>
    public int? TrainingWindowLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only non-overlapping cutoffs should be generated.
    /// </summary>
    public bool RequireNonOverlappingTargets { get; set; }

    /// <summary>
    /// Gets or sets the sample count required before a model is ranking-eligible.
    /// </summary>
    public int MinimumEvaluationSamples { get; set; } = 30;

    /// <summary>
    /// Gets or sets the observations skipped between cutoffs to reduce adjacent target overlap.
    /// </summary>
    public int EmbargoObservations { get; set; }

    /// <summary>
    /// Gets or sets the optional first cutoff date included in the evaluation.
    /// </summary>
    public DateOnly? EvaluationStartDate { get; set; }

    /// <summary>
    /// Gets or sets the optional latest target date included in the evaluation.
    /// </summary>
    public DateOnly? EvaluationEndDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether each cutoff refits the model from the active training window.
    /// </summary>
    public bool RefitEveryStep { get; set; } = true;

    /// <summary>
    /// Gets or sets the absolute return tolerance used to classify flat outcomes.
    /// </summary>
    public double FlatReturnTolerance { get; set; }

    /// <summary>
    /// Gets or sets the absolute return tolerance used to classify flat outcomes.
    /// </summary>
    public double DirectionZeroTolerance
    {
        get => this.FlatReturnTolerance;
        set => this.FlatReturnTolerance = value;
    }

    /// <summary>
    /// Gets or sets the rule used to convert forecasts into direction labels.
    /// </summary>
    public DirectionPredictionRule DirectionRule { get; set; } = DirectionPredictionRule.Automatic;

    /// <summary>
    /// Gets or sets calibration-bin options.
    /// </summary>
    public CalibrationOptions Calibration { get; set; } = new();

    /// <summary>
    /// Gets or sets the event sink used for coarse validation logging.
    /// </summary>
    public IValidationEventSink EventSink { get; set; } = NullValidationEventSink.Instance;

    /// <summary>
    /// Validates the options before evaluation starts.
    /// </summary>
    public void Validate()
    {
        if (this.MinimumTrainingObservations <= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.MinimumTrainingObservations),
                this.MinimumTrainingObservations,
                "At least two training observations are required.");
        }

        if (this.StepSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(this.StepSize), this.StepSize, "Step size must be positive.");
        }

        if (this.WindowMode == TrainingWindowMode.Rolling &&
            (!this.TrainingWindowLength.HasValue || this.TrainingWindowLength.Value < this.MinimumTrainingObservations))
        {
            throw new ArgumentException("Rolling validation requires a training window length at least as large as the minimum training size.");
        }

        if (this.MinimumEvaluationSamples < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.MinimumEvaluationSamples),
                this.MinimumEvaluationSamples,
                "Minimum evaluation samples cannot be negative.");
        }

        if (this.EmbargoObservations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.EmbargoObservations),
                this.EmbargoObservations,
                "Embargo observations cannot be negative.");
        }

        if (!this.RefitEveryStep)
        {
            throw new NotSupportedException("RefitEveryStep=false is not supported until reusable fitted-state semantics are implemented.");
        }

        if (this.FlatReturnTolerance < 0d ||
            double.IsNaN(this.FlatReturnTolerance) ||
            double.IsInfinity(this.FlatReturnTolerance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(this.FlatReturnTolerance),
                this.FlatReturnTolerance,
                "Direction tolerance must be finite and nonnegative.");
        }

        this.Calibration.Validate();
    }
}
