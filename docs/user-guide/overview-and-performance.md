# Overview and Performance

The Overview page combines investor-facing summaries with enough context to avoid reading the final
signal in isolation. The Performance page focuses on historical realized return behavior.

## What These Pages Show

| Area | Meaning |
| --- | --- |
| Guidance | Current decision label from the research report, or `NO CALL` when unavailable. |
| Fund score | Long-run 1 to 10 quality score, separate from current attractiveness. |
| Current attractiveness | Present opportunity score/category when model evidence exists. |
| Data freshness | Whether the latest observation is recent enough for actionability. |
| CAGR and cumulative return | Historical realized growth over the available window. |
| Sharpe and Sortino | Risk-adjusted historical performance diagnostics. |

## How To Interpret

Performance metrics describe the sample period. They do not prove that the same behavior will repeat.
A high fund score can coexist with weak current timing evidence, stale data, or `NO CALL`.

!!! info "Three layers"
    `Fund score`, `current attractiveness`, and `decision signal` are intentionally separate. See
    [Decision Signals](../concepts/decision-signals.md).

## Common Warning States

- insufficient observations for robust confidence;
- stale provider data;
- missing Model Arena validation;
- forecast ensemble unavailable;
- contradictory current model evidence.

## Related Documentation

- [Scoring](../scoring.md)
- [Returns and Annualization](../concepts/returns-and-annualization.md)
- [Forecast Metrics](../validation/forecast-metrics.md)
