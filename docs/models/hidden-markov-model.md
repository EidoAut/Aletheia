# Hidden Markov Model

Aletheia includes a univariate Gaussian hidden Markov model for regime diagnostics and timing
features.

## Intuition

An HMM assumes the observed return stream may switch between hidden states such as calmer and more
volatile regimes. The state is not observed directly; it is inferred probabilistically.

## Mathematical Definition

Let \(z_t\) be a hidden state and \(x_t\) be the observed return:

\[
P(z_t = j \mid z_{t-1}=i) = A_{ij}
\]

\[
x_t \mid z_t=j \sim N(\mu_j, \sigma_j^2)
\]

where \(A\) is the transition matrix and each state has a Gaussian mean and variance.

## Implementation in Aletheia

`GaussianHiddenMarkovModel`:

- supports 2 to 4 states;
- requires at least ten observations per state;
- uses scaled Baum-Welch updates;
- reports convergence versus maximum-iteration exit;
- exposes filtered probabilities through `FilterNext`.

Timing features use filtered online probabilities rather than smoothed full-sample posteriors. This
matters because smoothed probabilities can use future observations.

## Interpretation

Regime probabilities are context. They can inform timing candidates, but they do not prove causal
market predictability.

## Assumptions

- Hidden states follow a Markov transition process.
- Conditional emissions are Gaussian.
- The selected state count is adequate for the sample.

## Limitations

HMM regimes are descriptive unless validated out of sample as predictors. They can be unstable on
short or structurally changing histories.

## Source and Tests

- Source: `src/Aletheia.Dynamics/GaussianHiddenMarkovModel.cs`,
  `src/Aletheia.Dynamics/GaussianHmmResult.cs`,
  `src/Aletheia.Validation/RegimeTransitionForecaster.cs`
- Tests: `tests/Aletheia.Dynamics.Tests/DynamicVolatilityAndRegimeTests.cs`,
  `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs`
