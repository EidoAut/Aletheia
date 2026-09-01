# GARCH(1,1)

GARCH(1,1) estimates time-varying conditional volatility from return-like observations.

## Intuition

Financial return volatility often clusters: calm periods tend to follow calm periods, and volatile
periods tend to follow volatile periods. GARCH captures that persistence in conditional variance.

## Mathematical Definition

For centered return \(e_t\), the conditional variance is:

\[
\sigma_t^2 = \omega + \alpha e_{t-1}^2 + \beta \sigma_{t-1}^2
\]

where \(\omega\) is the long-run variance component, \(\alpha\) weights the lagged shock, and
\(\beta\) weights prior conditional variance.

## Implementation in Aletheia

`Garch11Estimator`:

- requires at least 30 observations;
- validates finite input;
- rejects near-constant series;
- searches deterministic constrained parameter grids;
- keeps \(\alpha + \beta < 1\) through admissibility checks;
- reports convergence, likelihood, parameters, conditional variances, and diagnostics.

Market-timing features use GARCH conditional volatility when it has converged. Otherwise Aletheia
falls back to causal EWMA volatility and reports the diagnostic.

## Interpretation

GARCH volatility can change barrier sizing and state diagnostics. It is not a direction forecast by
itself.

## Assumptions

- Conditional variance follows the GARCH(1,1) recursion.
- Residuals/returns are finite and contain enough variation.
- The deterministic search finds an admissible research estimate.

## Limitations

This is a constrained deterministic estimator, not a full production volatility-modeling suite. It
does not model leverage effects or distributional tails beyond the implemented likelihood path.

## Source and Tests

- Source: `src/Aletheia.Dynamics/Garch11Estimator.cs`,
  `src/Aletheia.Dynamics/Garch11FitResult.cs`,
  `src/Aletheia.Validation/MarketTimingFeaturePipeline.cs`
- Tests: `tests/Aletheia.Dynamics.Tests/DynamicVolatilityAndRegimeTests.cs`,
  `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs`
