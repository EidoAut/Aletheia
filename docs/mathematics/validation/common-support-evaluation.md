# Common-Support Evaluation

## Purpose

Common support prevents direct model comparisons from mixing different historical events.

## Formula

For models $m = 1,\ldots,M$ with successful evaluation-event sets $T_m$:

$$
T_{\text{common}} = \bigcap_{m=1}^{M}T_m
$$

Milestone 2.2 applies that rule per forecast-capability family instead of using one global support set:

$$
\begin{aligned}
T_{\text{point}} &= \bigcap_{m\in\mathcal{M}_{\text{point}}}T_m,\\
T_{\text{probability}} &= \bigcap_{m\in\mathcal{M}_{\text{probability}}}T_m,\\
T_{\text{quantile}} &= \bigcap_{m\in\mathcal{M}_{\text{quantile}}}T_m
\end{aligned}
$$

Probability-only models therefore do not reduce $T_{\text{point}}$, and point-only models do not reduce $T_{\text{probability}}$ or $T_{\text{quantile}}$.

An event key contains the fund, dataset fingerprint, prediction cutoff, target, requested horizon, and resolved observation count.

## Interpretation

All-sample metrics remain useful diagnostics for each model's successful cases. Common-support metrics answer a different question: how models compare on exactly the same forecast events.

## Implementation Notes

`EvaluationEventKey` is created from each `PredictionLedgerRecord`. `ModelArena` intersects those keys across registered models by metric family and calculates `PointCommonSupportMetrics`, `ProbabilityCommonSupportMetrics`, and `QuantileCommonSupportMetrics` for each `ModelArenaModelResult`. Ranking uses point-common-support metrics only when the configured minimum point-common-support count is met.
