# Kalman

Aletheia uses a local-linear trend Kalman model for log-NAV filtering and a state-space forecast
adapter for cumulative returns.

## Intuition

The Kalman model separates an observed log NAV into a latent level and trend. It updates those hidden
state estimates as new observations arrive and can project the level forward with uncertainty.

## Mathematical Definition

Observation equation:

$$
y_t = \ell_t + \eta_t
$$

State transition:

$$
\ell_t = \ell_{t-1} + b_{t-1} + \zeta_t
$$

$$
b_t = b_{t-1} + \xi_t
$$

where $y_t$ is log NAV, $\ell_t$ is latent level, $b_t$ is trend, and noise terms carry
observation, level, and trend variances.

## Implementation in Aletheia

`LocalLinearTrendKalmanModel` filters finite observations and stores level, trend, covariance,
innovation, innovation variance, and log likelihood. Default variances are deterministic heuristics
scaled from sample variance:

- observation variance: $0.25 \cdot \mathrm{scale}$;
- level variance: $0.05 \cdot \mathrm{scale}$;
- trend variance: $0.005 \cdot \mathrm{scale}$.

`StateSpaceForecastModel` fits this model to log NAV at every cutoff and converts the terminal
log-NAV projection into cumulative simple-return distribution outputs.

## Interpretation

Kalman forecasts are model-based distributions. Aletheia rejects implausible projected distributions
when cumulative variance or simple returns exceed conservative bounds.

## Assumptions

- A local-linear latent trend is an adequate approximation for the horizon.
- Noise variance heuristics are acceptable for the research baseline.
- NAV values are strictly positive.

## Limitations

The implementation does not claim maximum-likelihood hyperparameter estimation. It is a deterministic
research model that must be validated out of sample.

## Source and Tests

- Source: `src/Aletheia.Dynamics/LocalLinearTrendKalmanModel.cs`,
  `src/Aletheia.Validation/StateSpaceForecastModel.cs`
- Tests: `tests/Aletheia.Validation.Tests/StateSpaceForecastModelTests.cs`,
  `tests/Aletheia.Dynamics.Tests/DynamicVolatilityAndRegimeTests.cs`
