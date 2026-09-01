# Mathematics

This layer provides deterministic numeric primitives used by analytics, dynamics, validation, and reporting. Methods validate finite inputs aggressively and avoid hidden calendar assumptions.

## Descriptive Statistics

`DescriptiveStatistics` exposes:

- arithmetic mean, median, percentile, and quantile;
- sample variance and sample standard deviation with Bessel correction;
- population standard deviation;
- median absolute deviation;
- Fisher-Pearson sample skewness;
- unbiased excess kurtosis and non-excess kurtosis;
- covariance, Pearson correlation, autocorrelation, and partial autocorrelation.

Variance is calculated as:

\[
s^2 = \frac{\sum_{i=1}^{n}(x_i - \bar{x})^2}{n - 1}
\]

Skewness uses standardized residuals and the finite-sample Fisher-Pearson correction:

\[
\operatorname{skew}
= \frac{n}{(n - 1)(n - 2)}
  \sum_{i=1}^{n}\left(\frac{x_i - \bar{x}}{s}\right)^3
\]

Excess kurtosis uses the unbiased sample adjustment. Non-excess kurtosis is \(\operatorname{excess} + 3\).

Quantiles use linear interpolation over sorted observations:

\[
\begin{aligned}
a &= p(n - 1),\\
j &= \lfloor a \rfloor,\\
k &= \lceil a \rceil,\\
w &= a - j,\\
q(p) &= x_{[j]} + \left(x_{[k]} - x_{[j]}\right)w
\end{aligned}
\]

Here \(x_{[j]}\) and \(x_{[k]}\) are zero-based positions in the sorted sample.

Autocorrelation at lag `k` uses the full-sample mean and denominator, and partial autocorrelation is estimated by the Durbin-Levinson recursion from lag 1 through the requested maximum lag.

## Causal Normalization

`CausalNormalizer` emits one `CausalNormalizationPoint` for every input observation. Each point is calculated from data available at that point in time only.

Supported modes:

- `ExpandingZScore`: location and scale use observations \(0,\ldots,t\).
- `RollingZScore`: location and scale use the latest rolling window ending at `t`.
- `RollingRobust`: location is the rolling median and scale is \(1.4826 \cdot \operatorname{MAD}\).

The normalized value is:

\[
z_t = \frac{x_t - \operatorname{location}_t}{\operatorname{scale}_t}
\]

If fewer than `minimumSamples` are available, the point is aligned but marked unavailable. If scale is zero, the point is available with normalized value `0`.

## Failure Modes

- Empty series fail for statistics that require at least one observation.
- Variance-like methods require at least two observations.
- Skewness requires at least three observations.
- Excess kurtosis requires at least four observations.
- Quantile probabilities must be in `[0, 1]`.
- Percentiles must be in `[0, 100]`.
- Normalization windows and minimum sample counts must be positive.
- Non-finite observations are rejected instead of silently dropped.
