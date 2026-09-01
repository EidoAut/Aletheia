# Market-Timing Models

Market-timing models estimate probabilities for triple-barrier events. They are not trading bots.

## Intuition

Instead of saying "buy now" directly, the timing engine asks which event is more likely over a
specific horizon: upside barrier first, downside barrier first, or no barrier hit.

## Event Definition

For starting NAV \(P_t\), upside threshold \(u\), downside threshold \(d\), and horizon \(h\):

- `UpperHitFirst` occurs if the upper return barrier is reached first.
- `LowerHitFirst` occurs if the lower return barrier is reached first.
- `NoBarrierHit` occurs only when the full horizon is evaluable and neither barrier is hit.

Volatility-scaled barriers use causal volatility available at the label start. If volatility is not
available, the label is skipped rather than using a fabricated fixed fallback.

## Implemented Candidates

| Candidate | Role |
| --- | --- |
| Historical event-rate baseline | Simple class-prevalence benchmark. |
| Regime-transition timing | Adjusts event rates using HMM regime probabilities. |
| Historical analogue timing | Uses robustly scaled similar historical states. |
| Regularized event classifier | Multinomial classifier with L2 regularization. |
| Competing-risk hazard model | Reports cumulative-incidence diagnostics. |
| Spectral timing candidate | Experimental and ineligible until causal OOS spectral reconstruction exists. |

## Validation Requirements

Candidates need enough horizon-specific OOS samples, acceptable calibration, positive Brier skill
against baseline, and a non-negative bootstrap skill lower bound before they enter the ensemble.

## Interpretation

Timing probabilities describe events. The investor-facing label is a separate conservative
presentation layer that can downgrade to tentative labels or `NO CALL`.

## Source and Tests

- Source: `src/Aletheia.Validation/MarketTimingModelArena.cs`,
  `src/Aletheia.Validation/TripleBarrierLabeler.cs`,
  `src/Aletheia.Validation/MarketTimingFeaturePipeline.cs`,
  `src/Aletheia.Application/MarketTimingAssessmentBuilder.cs`
- Tests: `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs`,
  `tests/Aletheia.Application.Tests`

Related guide: [Market Timing](../user-guide/market-timing.md).
