namespace Aletheia.Dynamics;

/// <summary>
/// Stores a fitted Gaussian Hidden Markov Model.
/// </summary>
/// <param name="States">The fitted state emissions.</param>
/// <param name="InitialProbabilities">The initial state probabilities.</param>
/// <param name="TransitionMatrix">The transition matrix.</param>
/// <param name="PosteriorProbabilities">Smoothed posterior probabilities indexed by time and state.</param>
/// <param name="LogLikelihood">The final scaled log likelihood.</param>
/// <param name="Converged">A value indicating whether EM converged.</param>
/// <param name="Diagnostic">The fit diagnostic.</param>
/// <param name="FilteredProbabilities">Forward-filtered probabilities indexed by time and state.</param>
public sealed record GaussianHmmResult(
    IReadOnlyList<GaussianHmmState> States,
    IReadOnlyList<double> InitialProbabilities,
    double[,] TransitionMatrix,
    double[,] PosteriorProbabilities,
    double LogLikelihood,
    bool Converged,
    string Diagnostic,
    double[,]? FilteredProbabilities = null)
{
    /// <summary>
    /// Gets latest forward-filtered state probabilities for real-time feature use.
    /// </summary>
    public IReadOnlyList<double> LatestProbabilities
    {
        get
        {
            var probabilities = this.FilteredProbabilities ?? this.PosteriorProbabilities;
            if (probabilities.GetLength(0) == 0)
            {
                return Array.Empty<double>();
            }

            var stateCount = probabilities.GetLength(1);
            var result = new double[stateCount];
            var row = probabilities.GetLength(0) - 1;
            for (var state = 0; state < stateCount; state++)
            {
                result[state] = probabilities[row, state];
            }

            return result;
        }
    }

    /// <summary>
    /// Gets latest smoothed posterior probabilities for diagnostics.
    /// </summary>
    public IReadOnlyList<double> LatestSmoothedProbabilities
    {
        get
        {
            if (this.PosteriorProbabilities.GetLength(0) == 0)
            {
                return Array.Empty<double>();
            }

            var stateCount = this.PosteriorProbabilities.GetLength(1);
            var result = new double[stateCount];
            var row = this.PosteriorProbabilities.GetLength(0) - 1;
            for (var state = 0; state < stateCount; state++)
            {
                result[state] = this.PosteriorProbabilities[row, state];
            }

            return result;
        }
    }
}
