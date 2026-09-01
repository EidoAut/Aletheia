# FAQ

## Is Aletheia financial advice?

No. It is a quantitative research and validation tool.

## Can Aletheia say buy or sell?

It can display `BUY`, `BUY?`, `SELL`, or `SELL?` labels when the decision layer has enough evidence
for that label. A question mark means the direction is tentative. Every label remains research output,
not advice.

## Why does Aletheia sometimes say `NO CALL`?

Because missing, contradictory, stale, out-of-distribution, or insufficiently validated evidence is a
valid scientific result.

## Does a high fund score mean buy now?

No. Fund score summarizes long-run quality. Current actionability depends on freshness, current
attractiveness, tactical timing, validation evidence, and warnings.

## Does `ReliabilityIndex` mean chance of success?

No. It is a validation-support index after penalties for sample scarcity, disagreement, calibration,
OOD, and weight concentration.

## Does a calibrated model guarantee profit?

No. Calibration is probability quality. Economic value requires delayed backtesting and independent
validation under realistic costs and constraints.

## Can the user configure forecast windows?

The desktop exposes a configurable primary calendar-day Model Arena horizon. Current application
forecasts are produced for 30, 90, 180, and 365 calendar days. Simulation and backtest options are
configurable through the CLI options documented in [CLI Reference](cli-reference.md).
