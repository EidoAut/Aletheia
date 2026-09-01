using Aletheia.Core;

namespace Aletheia.Validation;

/// <summary>
/// Couples fund history with the reproducibility identity used for validation.
/// </summary>
public sealed class ForecastEvaluationDataset
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastEvaluationDataset"/> class.
    /// </summary>
    /// <param name="history">The fund history.</param>
    /// <param name="datasetIdentity">The deterministic dataset identity.</param>
    /// <param name="aletheiaVersion">The Aletheia version stored with generated predictions.</param>
    public ForecastEvaluationDataset(
        FundHistory history,
        DatasetIdentity datasetIdentity,
        string aletheiaVersion = AletheiaRelease.ScientificVersion)
    {
        this.History = history ?? throw new ArgumentNullException(nameof(history));
        this.DatasetIdentity = datasetIdentity ?? throw new ArgumentNullException(nameof(datasetIdentity));
        this.AletheiaVersion = string.IsNullOrWhiteSpace(aletheiaVersion)
            ? throw new ArgumentException("Aletheia version cannot be empty.", nameof(aletheiaVersion))
            : aletheiaVersion;
    }

    /// <summary>
    /// Gets the fund history.
    /// </summary>
    public FundHistory History { get; }

    /// <summary>
    /// Gets the deterministic dataset identity.
    /// </summary>
    public DatasetIdentity DatasetIdentity { get; }

    /// <summary>
    /// Gets the Aletheia version stored with generated predictions.
    /// </summary>
    public string AletheiaVersion { get; }
}
