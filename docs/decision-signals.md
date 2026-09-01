# Decision Signals

Decision signals are the most conservative layer of the research report. They are evidence summaries, not financial advice.

## Separation Of Concepts

Aletheia reports three different ideas:

- fund quality: long-run 1 to 10 quality score;
- current attractiveness: current opportunity score and category;
- decision signal: investor direction, validation qualification, strength, confidence, horizon, evidence, counter-evidence, warnings, and deterministic reasons.

Keeping these separate prevents a good historical fund score from automatically becoming an accumulate signal.

## Labels

Aletheia uses a small label vocabulary:

- `BUY`
- `BUY?`
- `HOLD`
- `HOLD?`
- `SELL`
- `SELL?`
- `NO CALL`

`BUY`, `HOLD`, and `SELL` require evidence that passes the configured validation gates. A question mark means a directional estimate exists, but validation, freshness, timing, OOD, or actionability evidence is not strong enough for a fully validated current decision.

`HOLD` is not the same as `NO CALL`. `HOLD` means available evidence supports no change in exposure. `NO CALL` means Aletheia cannot defend a conclusion from the available evidence.

## Gating

A fully confirmed strategic label requires a validation-gated ensemble distribution with reliability at or above `MinimumSignalReliability`. Low reliability does not become false certainty; it downgrades the label to tentative when a directional estimate exists.

If no validation-gated ensemble is available, Aletheia may still expose a qualified direction from current model forecasts, but only as `BUY?`, `HOLD?`, or `SELL?`. If forecasts are missing, contradictory, stale without usable direction, or invalidated by severe OOD, the label is `NO CALL`.

## Action Thresholds

When ensemble evidence is available, current attractiveness maps to the legacy diagnostic action:

```text
score >= 8.0   -> Accumulate
score >= 6.2   -> MildAccumulate
score <= 2.2   -> StrongReduce
score <= 3.8   -> Reduce
otherwise      -> Neutral
```

Signal strength is:

$$
\operatorname{strength}
= \frac{\left|\operatorname{currentAttractivenessScore}-5\right|}{5}
$$

The value is clipped to `[0, 1]`.

## Current Attractiveness

Current attractiveness blends ensemble expected return, probability of positive return, probability of loss greater than 10 percent, current drawdown, and optional regime evidence. When ensemble reliability is below the confirmation threshold, the score is retained as a directional estimate with low confidence rather than being erased into neutral.

## Actionability

Actionability is separate from direction. Stale data, missing tactical timing, insufficient timing evidence, severe OOD, or a tentative strategic label can block a current action even when a historical or qualified direction is visible. Stale reports should be read as of the latest effective observation date.

## Required Warnings

Every directional signal carries warnings from the report and should be interpreted as research output. The system does not place trades, optimize portfolios, or override user risk constraints.
