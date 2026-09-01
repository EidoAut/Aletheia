# AR(1) Log-Return Model

## Model

The baseline dynamic model is:

\[
r_t = c + \phi r_{t-1} + \varepsilon_t
\]

where \(r_t\) is a log return and \(\varepsilon_t\) is a zero-mean innovation
with variance \(\sigma_{\varepsilon}^{2}\).

The model never consumes the `SimpleReturn` state dimension. Forecasting reads `StandardStateDimensions.LogReturn`, which matches the quantity used during fitting.

## Recursive Forecast

One-step forecast:

\[
\hat{r}_{t+1} = c + \phi r_t
\]

Multi-step forecast:

\[
\hat{r}_{t+k} = c + \phi \hat{r}_{t+k-1}
\]

Cumulative expected log return over `h` observations:

\[
\hat{R}_{\log,h} = \sum_{k=1}^{h}\hat{r}_{t+k}
\]

Median simple cumulative return and transformed point forecast:

\[
\operatorname{Median}(R_{\text{simple}}) = \exp(\mu) - 1
\]

If cumulative log return is modeled as:

\[
X \sim \mathcal{N}(\mu, \sigma^2)
\]

then the expected simple return is:

\[
\mathbb{E}[\exp(X) - 1] = \exp\left(\mu + \frac{1}{2}\sigma^2\right) - 1
\]

Aletheia exposes both `MedianSimpleReturn` and `ExpectedSimpleReturn`. They are equal only when cumulative log-return variance is zero. This replaces both the incorrect shortcut of multiplying one expected return by the horizon and the incorrect practice of calling \(\exp(\mu) - 1\) an expectation.

## Forecast Variance

The implementation calculates exact cumulative forecast-error variance for the AR(1) recursion under homoskedastic innovations:

\[
\operatorname{Var}\left(\sum_{k=1}^{h}e_k\right)
= \sigma_{\varepsilon}^{2}
  \sum_{m=1}^{h}
  \left(\sum_{j=0}^{h-m}\phi^j\right)^2
\]

When \(\phi = 0\), this reduces to:

\[
h\sigma_{\varepsilon}^{2}
\]

For positive or negative \(\phi\), covariance between future forecast errors changes the cumulative uncertainty.

## Stationarity

The fitted model exposes:

- `Intercept`;
- `Phi`;
- `InnovationVariance`;
- `IsStationary`.

The process is marked stationary only when:

\[
|\phi| < 1
\]

Non-stationary fits are surfaced in model metadata and CLI diagnostics rather than silently treated as ordinary stationary predictors.

## Horizon Unit

The AR(1) model forecasts over observation counts. A calendar-day request must be resolved before calling the model. Passing a calendar-day horizon directly to the AR(1) forecast raises an error.

The model declares `LogReturn` as a required state dimension. If a state contains only `SimpleReturn`, forecasting fails with an explicit compatibility error instead of substituting zero.
