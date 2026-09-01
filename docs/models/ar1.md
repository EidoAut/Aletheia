# AR(1)

The AR(1) model is an autoregressive log-return forecast model.

## Intuition

AR(1) asks whether the latest return contains information about the next return. It is intentionally
simple and useful as a transparent benchmark.

## Mathematical Definition

For log return \(g_t\):

\[
g_t = c + \phi g_{t-1} + \epsilon_t
\]

where \(c\) is an intercept, \(\phi\) is the lag coefficient, and \(\epsilon_t\) is innovation noise.
Aletheia rejects the fitted model for forecasting when \(|\phi| \ge 1\).

## Implementation in Aletheia

`AutoregressiveForecastModel` adapts `AutoregressiveStateModel` to the common forecast interface. It:

- requires at least four NAV observations and at least three log returns;
- refits at every validation cutoff;
- builds the current dynamic state with the shared state pipeline;
- converts cumulative log-return moments into simple-return forecasts and quantiles.

## Interpretation

A positive AR(1) point forecast can support a directional estimate, but it must pass validation before
it can support a confirmed signal.

## Assumptions

- The fitted process is stationary.
- The relevant dependence is captured by a single lag.
- The historical return process is informative for the evaluated horizon.

## Limitations

AR(1) cannot model nonlinear regimes, volatility clustering, or structural breaks by itself.

## Source and Tests

- Source: `src/Aletheia.Validation/AutoregressiveForecastModel.cs`,
  `src/Aletheia.Dynamics/AutoregressiveStateModel.cs`
- Tests: `tests/Aletheia.Dynamics.Tests`, `tests/Aletheia.Validation.Tests`

Related math: [AR(1) Log-Return Model](../mathematics/ar1.md).
