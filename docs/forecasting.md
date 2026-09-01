# Forecasting

Forecasting is model-agnostic. Individual models declare capabilities, validation evaluates them on common support, and the research report only builds an ensemble from models with accepted validation evidence.

## Forecast Capabilities

Forecast distributions can expose:

- point forecast;
- expected return;
- median return;
- probability of positive return;
- probability of return greater than 5 percent;
- probability of loss greater than 10 percent;
- quantiles;
- full distribution.

The point forecast statistic is explicit, so mean and median forecasts are not silently mixed.

## State-Space Forecast Model

`StateSpaceForecastModel` fits a local-linear Kalman model to log NAV levels at each training cutoff. The observation, level, and trend noise variances are deterministic heuristics scaled from historical log-return variance; they are not presented as maximum-likelihood hyperparameters. For a horizon of `H` effective observations, the terminal log-NAV forecast is differenced against the last observed log NAV:

$$
\begin{aligned}
\mu_{T,H} &= \widehat{L}_{T+H} - L_T,\\
\sigma^2_{T,H} &= \widehat{\mathrm{Var}}(L_{T+H}),\\
\mathrm{ExpectedSimpleReturn}
&= \exp\left(\mu_{T,H} + \frac{1}{2}\sigma^2_{T,H}\right)-1,\\
\mathrm{MedianSimpleReturn}
&= \exp(\mu_{T,H})-1
\end{aligned}
$$

Probability and quantile outputs use the standard normal CDF and inverse CDF over cumulative log-return space, then convert back to simple returns.

The adapter rejects projected distributions when cumulative variance is non-finite or excessive, or when expected/median simple returns fall outside conservative fund-return plausibility bounds. Such cases return `ModelRejected` with diagnostics instead of surfacing a numerically explosive forecast.

## Evidence-Weighted Ensemble

`ForecastEnsemble` combines eligible forecast distributions with validation-weighted exponential weights:

$$
\begin{aligned}
\tilde{w}_i
&= \exp\left[-\lambda\left(L_i + C_i\right)\right],\\
w_i
&= \frac{\tilde{w}_i}{\sum_j \tilde{w}_j}
\end{aligned}
$$

Models with insufficient validation evidence, mismatched validation horizon, ineligible rankings, invalid loss, or non-positive relative skill receive zero weight through exclusion from the eligible set. Evidence from a 90-day arena, for example, cannot weight a 365-day forecast.

The ensemble combines expected return and event probabilities linearly because those are expectations. Quantiles are not averaged; they are obtained by inverting a deterministic approximation to the weighted mixture CDF built from each member's reported quantile knots. Model disagreement is the weighted standard deviation of member point forecasts. Reliability increases with effective model count and same-horizon sample evidence, and decreases with disagreement.

## Qualified Defaults

If no eligible model is available, the ensemble returns no distribution and reports a diagnostic. The application layer does not fabricate validated confidence from that absence.

Current model forecasts may still be summarized as a qualified direction when they contain a consistent expected-return or probability-positive bias. Such output is labeled `BUY?`, `HOLD?`, or `SELL?`, not as a fully validated call. If the forecasts are missing, contradictory, or invalidated by stronger guardrails, the report emits `NO CALL`.
