# Scoring

Fund scoring summarizes long-run fund quality on a 1 to 10 scale. It is intentionally separate from current attractiveness and from the decision signal.

## Inputs

`FundResearchReportBuilder` combines:

- performance quality;
- risk quality;
- risk-adjusted performance;
- stability;
- predictive evidence;
- data quality.

Default weights come from `FundScoringOptions`:

```text
performance quality        0.22
risk quality               0.22
risk-adjusted performance  0.18
stability                  0.14
predictive evidence        0.12
data quality               0.12
```

Each component is mapped to a 1 to 10 score through thresholded unit scores. The final score is the weighted sum of component scores.

## Component Semantics

- Performance quality uses CAGR, cumulative return, and positive-period ratio.
- Risk quality uses annualized volatility and maximum drawdown severity.
- Risk-adjusted performance uses Sharpe and Sortino ratios.
- Stability uses current-state adequacy, rolling-volatility stability, and lag-1 autocorrelation.
- Predictive evidence uses Model Arena eligibility and positive relative skill when available.
- Data quality uses provider quality diagnostics and history depth.

## Confidence

Confidence combines data quality, observation count, and predictive evidence. A high fund score with low confidence should be read as a provisional research summary, not a durable conclusion.

## Caveats

The score rewards long-run quality. A high-quality fund can still have a neutral or unfavorable current attractiveness assessment if validation-gated forward evidence is weak.
