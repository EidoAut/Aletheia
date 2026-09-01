namespace Aletheia.Application;

/// <summary>
/// Provides reusable method metadata for desktop theory panels.
/// </summary>
public sealed class TheoryCatalog
{
    private readonly IReadOnlyList<TheoryArticle> articles =
    [
        new TheoryArticle(
            "Log Returns",
            "r_t = ln(P_t / P_(t-1))",
            "Represent multiplicative NAV changes in an additive time-series form.",
            "NAV observations are positive and chronologically ordered.",
            "Consecutive log returns add across horizons and are useful for dynamic models.",
            "Log-return additivity does not make returns normally distributed or independently sampled."),
        new TheoryArticle(
            "CAGR",
            "CAGR = (P_T / P_0)^(1 / years) - 1",
            "Summarize the realized geometric growth rate over the observed period.",
            "The start and end NAV values are representative of the period being summarized.",
            "Useful as a compact historical growth statistic, not as a forecast.",
            "Sensitive to endpoint selection and silent about drawdown path risk."),
        new TheoryArticle(
            "Volatility",
            "sigma_ann = stdev(R_t) * sqrt(periods per year)",
            "Measure realized dispersion of periodic simple returns.",
            "The observation-frequency convention maps periods to years.",
            "Higher values indicate larger historical return variation.",
            "Volatility is symmetric and does not distinguish upside from downside moves."),
        new TheoryArticle(
            "Sharpe",
            "Sharpe = mean(excess R_t) / stdev(R_t) * sqrt(periods per year)",
            "Compare realized return per unit of total volatility.",
            "Periodic excess returns and annualization convention are meaningful for the series.",
            "Useful for baseline risk-adjusted comparison.",
            "Can be unstable for short histories and non-normal return distributions."),
        new TheoryArticle(
            "AR(1)",
            "r_t = c + phi r_(t-1) + epsilon_t",
            "Model first-order linear dependence in log returns.",
            "|phi| < 1 for stationarity in the fitted baseline.",
            "A transparent dynamic baseline for forecast evaluation.",
            "Financial returns often exhibit weak linear autocorrelation; in-sample fit is not predictive skill."),
        new TheoryArticle(
            "FFT Spectrum",
            "X_k = sum_n x_n exp(-2*pi*i*k*n/N)",
            "Inspect observation-index frequency content after explicit preprocessing.",
            "Samples are ordered by observation index; frequency is cycles per observation.",
            "Dominant periods can reveal cyclic structure in returns.",
            "Spectral peaks can be unstable and must not be interpreted as calendar-day cycles without frequency semantics."),
        new TheoryArticle(
            "Brier Score",
            "Brier = mean((p_t - o_t)^2)",
            "Score probability forecasts for positive returns.",
            "Each outcome is binary and forecasts are calibrated probabilities in [0, 1].",
            "Lower values indicate better probability accuracy.",
            "Brier score does not evaluate point forecast magnitude."),
        new TheoryArticle(
            "MAE",
            "MAE = mean(|y_t - yhat_t|)",
            "Measure point-forecast error in decimal return units.",
            "Point forecasts and realized outcomes share the same horizon and return semantics.",
            "Lower values indicate smaller average absolute error.",
            "MAE does not measure calibration or asymmetric decision utility."),
        new TheoryArticle(
            "Calibration",
            "x = predicted probability, y = observed frequency",
            "Compare predicted positive-return probabilities against realized bin frequencies.",
            "Bins contain enough samples to make observed frequencies meaningful.",
            "A calibrated model lies near the y = x reference line.",
            "Sparse bins can look noisy even when probabilities are sensible."),
        new TheoryArticle(
            "Historical Analogues",
            "d(x, z) = sqrt(sum_i ((x_i - z_i) / s_i)^2)",
            "Find historical dynamic states close to the current state under schema-compatible dimensions.",
            "State dimensions and schema fingerprints are compatible and scaled consistently.",
            "Nearest states help inspect recurrent regions and subsequent historical paths.",
            "Analogues are descriptive and must not be treated as a validated decision engine."),
    ];

    /// <summary>
    /// Gets all theory articles.
    /// </summary>
    public IReadOnlyList<TheoryArticle> Articles => this.articles;
}
