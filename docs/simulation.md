# Simulation

Simulation is used for scenario exploration. It is not a substitute for validation and does not create a forecast claim by itself.

## Investment Plan Simulation

`InvestmentPlanSimulator` simulates periodic investing over explicit monthly horizons and path counts. NAV returns are treated as fund-net returns. Only external investor effects are applied explicitly:

- entry fee on initial and monthly contributions;
- external annual service cost as a monthly multiplicative drag;
- exit fee on terminal value;
- optional annual inflation adjustment for real terminal values.

This prevents double-counting when NAV already includes fund-internal expenses.

The simulator reports nominal percentiles, optional real percentiles, probability of loss, and methodology text. It validates finite capital inputs, positive horizon, path-count limits, and fee/inflation bounds.

## Historical Bootstrap

`ReturnPathBootstrapSimulator` simulates cumulative returns by resampling historical log returns:

- historical bootstrap samples individual observed log returns with replacement;
- block bootstrap samples contiguous blocks with replacement.

Each path sums sampled log returns over the resolved horizon and converts to simple return:

\[
\operatorname{simpleReturn}
= \exp\left(\sum_{t=1}^{h}r_t\right)-1
\]

The output includes raw samples and a `ForecastDistribution` built from the samples. It is still labeled as simulation evidence, not a validated forecast.

## Stress Scenarios

`StressScenarioAnalyzer` emits deterministic adverse scenarios:

- historically worst contiguous window;
- instant return shock;
- prolonged bear regime.

Stress outputs include maximum drawdown, terminal return, and methodology. They are "what if" paths, not probability estimates.
