# Dynamics

The dynamics layer models time-varying behavior in returns without crossing into I/O or application policy. It is deterministic, univariate, and designed for diagnostics and validation.

## EWMA Volatility

`EwmaVolatilityEstimator` estimates a recursively updated variance from finite returns or residuals:

$$
\begin{aligned}
\operatorname{var}_t
&= \lambda\operatorname{var}_{t-1} + (1-\lambda)r_{t-1}^{2},\\
\operatorname{vol}_t
&= \sqrt{\operatorname{var}_t}
\end{aligned}
$$

The initial variance is the sample variance when enough observations exist. The estimator is useful as a fast volatility state, not as a calibrated probability model by itself.

## GARCH(1,1)

`Garch11Estimator` fits a Gaussian GARCH(1,1) model to centered observations:

$$
h_t = \omega + \alpha e_{t-1}^{2} + \beta h_{t-1}
$$

The fit enforces:

- $\omega > 0$;
- $\alpha \ge 0$;
- $\beta \ge 0$;
- $\alpha+\beta < 1$.

The implementation uses a deterministic constrained likelihood search. It returns parameters, the fitted mean, conditional variances, log likelihood, convergence state, and a diagnostic message. Consumers that evaluate rolling cutoffs should advance the variance state with `NextConditionalVariance(previousObservation, previousConditionalVariance)` between refits rather than freezing the last refit variance.

Main failure cases:

- fewer than 30 observations;
- non-finite observations;
- near-constant series;
- no admissible parameter set;
- non-finite likelihood.

## Local Linear Trend Kalman Model

`LocalLinearTrendKalmanModel` uses a two-state local-linear system over an ordered univariate signal:

$$
\begin{aligned}
\operatorname{state}_t &= [\ell_t, b_t],\\
\ell_t &= \ell_{t-1} + b_{t-1} + \eta^{(\ell)}_t,\\
b_t &= b_{t-1} + \eta^{(b)}_t,\\
y_t &= \ell_t + \varepsilon_t
\end{aligned}
$$

The filter records level, trend, state variances, level/trend covariance, innovation, innovation variance, and log likelihood. Forecasts propagate the full two-state covariance forward and emit expected value, variance, and approximate 95 percent bounds.

`StateSpaceForecastModel` adapts this Kalman model to `IForecastModel` by fitting it to historical log NAV only up to each validation cutoff. Future terminal log NAV is differenced against the last observed log NAV, represented as a cumulative log return, and converted to simple-return distributions.

## Gaussian HMM

`GaussianHiddenMarkovModel` fits a univariate hidden Markov model with 2 to 4 Gaussian-emission states. It uses scaled Baum-Welch updates to avoid forward probability underflow:

$$
\begin{aligned}
P(x_t \mid z_t=j) &= \mathcal{N}(\mu_j,\sigma_j^2),\\
P(z_t=j \mid z_{t-1}=i) &= A_{ij}
\end{aligned}
$$

The result includes state labels, initial probabilities, transition matrix, smoothed posterior state probabilities, filtered online probabilities, latest probabilities, log likelihood, convergence state, and diagnostics. Online feature pipelines use filtered probabilities and update them forward with `FilterNext`; smoothed posteriors are retained for descriptive diagnostics, not for historical cutoff features.

The model is descriptive. Regime labels and probabilities are not investment recommendations and do not imply that a state transition is predictable.
