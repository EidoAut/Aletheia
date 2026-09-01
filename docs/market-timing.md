# Probabilistic Market Timing

Aletheia's market-timing engine estimates explicit future events instead of emitting raw buy/sell labels. It answers questions such as "what is the probability that an upside barrier is reached before a downside barrier over this horizon?" and then exposes the uncertainty, validation quality, model disagreement, and out-of-distribution diagnostics behind the answer.

The feature is a research and validation tool. A timing zone is not a brokerage instruction and can remain neutral when the evidence is weak.

## Event Definition

Each horizon is framed as a triple-barrier problem:

- an upper return barrier for favorable movement;
- a lower return barrier for adverse movement;
- a vertical time barrier expressed as a `ForecastHorizon`.

Labels are assigned by `TripleBarrierLabeler` as:

- `UpperHitFirst`;
- `LowerHitFirst`;
- `NoBarrierHit`.

Barriers can use fixed percentage thresholds or volatility-scaled thresholds. Volatility-scaled barriers use only causal volatility values available at the label start. If volatility is unavailable or non-finite at that start, the label is skipped rather than falling back to a fabricated threshold.

Each label stores both `StartIndex` and $\mathrm{EndIndex}=\mathrm{StartIndex}+\mathrm{TimeToEvent}$. A label is eligible for training only after its `EndIndex` has passed the prediction cutoff plus any purge/embargo requirement.

## Causal Feature Pipeline

`MarketTimingFeaturePipeline` builds one feature vector per eligible observation. Every historical feature is computed from the prefix that would have been available at that observation. For cutoff $T$, Aletheia uses observations up to $T$, updates causal volatility/Kalman/HMM/change-point state from that prefix, builds `FeatureVector(T)`, and only then evaluates outcomes after $T$.

The feature set includes:

- lagged and rolling returns;
- momentum, acceleration, drawdown, drawdown duration, distance from high, and recovery velocity;
- rolling volatility, causal EWMA volatility, and prefix-fitted GARCH conditional volatility when the full state profile is enabled; GARCH variance is advanced causally between refits;
- prefix-filtered Kalman level, trend, uncertainty, innovation, and normalized innovation when enabled;
- prefix-fitted HMM bull/bear probabilities and transition risk when enabled; HMM features use filtered online probabilities rather than smoothed full-sample posteriors;
- online adjacent-window change-point probability.

External spectral and ensemble evidence is treated differently from prefix-derived state features. If a historical cutoff does not have a causally reconstructed external evidence value, the feature key is absent. Aletheia does not replace unavailable spectral phase, spectral stability, ensemble expected return, ensemble probabilities, forecast dispersion, model disagreement, or ensemble reliability with `0`, `0.5`, or another neutral number. The current live point may use evidence computed from the full available history, but that evidence is not retropropagated into prior walk-forward samples.

Classifier fitting, historical analogue distance, and out-of-distribution detection use only feature columns that are actually available for the relevant training prefix and prediction cutoff. Missing feature keys therefore remove a column from that local comparison instead of silently adding an artificial observation.

The application uses a lightweight automatic preview profile when loading ordinary workspaces so UI and tests remain responsive. The explicit CLI `timing` command, report path, and desktop Model Arena path use the full scientific profile with GARCH, Kalman, and HMM state features.

## Model Arena

`MarketTimingModelArena` evaluates several timing candidates on the same temporal support:

- historical event-rate baseline;
- regime-transition timing model;
- historical analogue timing model;
- regularized multi-class event classifier;
- competing-risk hazard model;
- spectral timing candidate.

Each candidate must compete against a simple baseline through horizon-specific walk-forward evaluations. For a prediction cutoff $T$, training labels must satisfy $\mathrm{EndIndex} \le T-\mathrm{purge}-\mathrm{embargo}$; the out-of-sample label's realized duration is never used to decide its own training cutoff. The ensemble weights only eligible candidates with enough OOS samples for that horizon, acceptable calibration, positive baseline-relative Brier skill, and a non-negative lower bootstrap bound for that skill. Rejected models remain visible with sample counts, Brier, ECE, log loss, calibration status, eligibility, ensemble weight, and rejection reason.

Competing-risk hazards are reported as unconditional cumulative-incidence diagnostics, not as independent ensemble diversity. The spectral timing candidate is currently experimental and remains ineligible until causal historical spectral features are reconstructed for OOS validation.

## Probability And Calibration

The engine reports probability triples:

- upside event probability;
- downside event probability;
- no-barrier probability.

For each model Aletheia preserves raw probability, calibrated probability, and calibration status. Calibration uses one-vs-rest Platt scaling only from prior OOS predictions; if there are too few samples, probabilities remain raw and the reliability score is penalized. It also reports Brier score, log loss, expected calibration error, per-class calibration, reliability bins, balanced accuracy, Brier decomposition, Brier skill versus baseline, and block-bootstrap intervals for the baseline-relative improvement.

The ensemble output includes `ReliabilityIndex`, effective model count, and disagreement. `ReliabilityIndex` is a heuristic validation-quality index, not a probability that the timing call is correct. Low ReliabilityIndex or high disagreement naturally pulls the presentation layer toward neutral/watch zones.

The current ReliabilityIndex combines sample evidence, model effective count, model disagreement, calibration error, positive predictive skill, Brier-skill interval width, OOD distance, and weight concentration. It should be read as "how much validated support remains after penalties", not as "chance of success".

## Horizon Selection And Evidence

Each horizon keeps independent OOS evidence: sample count, Brier, log loss, ECE, directional/balanced accuracy, calibration status, eligibility, and weights. Evidence from one horizon is not reused for another horizon, and the application report builds a forecast ensemble from the arena that matches the forecast horizon being presented.

The primary horizon is selected by a conservative score combining ReliabilityIndex, evidence strength, model agreement, calibration/OOD penalties, ensemble activation, and a soft preference for the configured useful horizon. A short horizon is not selected merely because it is short.

If the ensemble lacks enough validated evidence, the technical timing zone remains `InsufficientEvidence`. This is deliberately distinct from `Neutral`, which means the evidence exists but upside and downside are balanced.

The investor-facing timing label is separate from the technical zone. Weak but directional timing probabilities can surface as `BUY?` or `SELL?`; balanced weak evidence can surface as `HOLD?`; severe out-of-distribution states force `NO CALL`.

## Expected Return, Barriers, And Quantiles

Barrier events and terminal return distributions are separate:

- `ProbabilityUp`, `ProbabilityDown`, and `ProbabilityNoEvent` describe first-hit triple-barrier outcomes.
- `ExpectedBarrierPayoff` is the payoff implied by those first-hit probabilities and the effective barriers.
- `ForecastExpectedReturn` is computed from terminal horizon returns when enough samples exist.
- P10/P25/P50/P75/P90 are shown only when they are true terminal-return quantiles. If the sample count is insufficient, Aletheia does not label barriers as percentiles.

The UI displays the effective upside/downside barriers used by the labeler. If volatility-scaled barriers are enabled, diagnostics say so.

Calendar-day horizons are complete only when the dataset contains an observation on or after the requested target date. The requested target date and the effective valuation date are stored separately; if the target falls on a weekend, holiday, or missing valuation day, the first later observation is marked as a calendar valuation approximation. An incomplete calendar horizon cannot be recorded as `NoBarrierHit` and does not enter calibration, Brier scoring, bootstrap intervals, terminal returns, or historical evaluation ledgers.

## Out-Of-Distribution

OOD uses a robust standardized Euclidean distance: each feature is centered by historical median, scaled by MAD, squared, averaged across features, and square-rooted. The default OOD threshold is in the real range of that metric. Results are surfaced as:

- `InDistribution`;
- `SlightlyUnusual`;
- `OutOfDistribution`.

OOD does not change probabilities directly; it lowers reliability and can force abstention.

Historical analogue timing uses the same robust median/MAD feature scaling idea before nearest-neighbor distance is computed, so a feature measured in large units cannot dominate merely because of its unit scale.

## Timing Zones

Application-level presentation converts validated probabilities into interpretable zones:

- `StrongAccumulation`;
- `Accumulation`;
- `WatchPositive`;
- `Neutral`;
- `WatchNegative`;
- `Reduction`;
- `StrongReduction`.

Zones are based on probability edge, evidence strength, calibration, model reliability, and out-of-distribution diagnostics. They are deliberately conservative: insufficient history, weak validation, or unusual current states should reduce confidence rather than create a strong signal.

Timing decisions expose direction, qualification, directional strength, validation strength, reasons, evidence, and counter-evidence. A question mark marks a qualified view, not an actionable instruction.

## Surfaces

CLI:

```powershell
dotnet run --project src/Aletheia.Cli -- timing sample
dotnet run --project src/Aletheia.Cli -- timing examples/sample-fund.csv
dotnet run --project src/Aletheia.Cli -- timing --provider cnmv-iic --fund ES0000000000
```

Desktop:

- the sidebar includes a `Market Timing` page under `MODELS`;
- ordinary workspace loads show the lightweight automatic timing preview;
- running Model Arena refreshes the timing page with the full validated profile.

Application:

- `FundAnalysisResult.MarketTiming` stores the automatic preview;
- `AletheiaApplicationService.BuildMarketTimingAssessment` builds a full timing assessment for an existing workspace;
- `AletheiaApplicationService.RunTimingEconomicBacktest` converts only historical OOS timing predictions into delayed target-exposure paths.

The economic backtest is displayed separately from probability validation. It preserves calculation, decision and execution dates, compares Aletheia timing with buy-and-hold and neutral/no-action, and reports `NO RELIABLE ECONOMIC BACKTEST` when usable OOS timing decisions are insufficient. A good Brier score or ReliabilityIndex remains validation evidence, not profitability evidence.

## Scientific Guardrails

Market timing follows the same scientific rules as the rest of Aletheia:

- no future observations may affect historical features;
- no training label may be used before its event or vertical barrier end index is known;
- all model claims must survive out-of-sample walk-forward checks;
- complex models must beat simple baselines before they enter the ensemble;
- probability calibration is trained only on prior OOS predictions and reported separately from direction;
- out-of-distribution current states reduce ReliabilityIndex and can force abstention;
- no reliable signal is a valid result.
- nested walk-forward selection should be used for design choices, and a final frozen holdout should remain untouched until the end.

## Pipeline Summary

```text
Historical data
      ↓
Causal Feature Pipeline
      ↓
Walk-forward validation
      ↓
Candidate models
      ↓
Probability calibration
      ↓
Model eligibility
      ↓
Horizon-specific ensemble
      ↓
OOD + disagreement + reliability
      ↓
Decision engine
      ↓
Human-readable explanation
```

## What Aletheia DOES NOT claim

- It does not guarantee profits.
- It does not know the future.
- Probabilities are not certainties.
- High event probability is not the same as high reliability.
- Low evidence produces a qualified label or abstention rather than a forced validated BUY/SELL.
- Turning-point research remains experimental unless separately validated OOS.

## Limits

The current engine is univariate on fund NAV history plus validation-gated internal evidence. It does not include macro factors, benchmark-relative factors, transaction costs, tax treatment, liquidity constraints, or execution logic. It estimates event probabilities under historical evidence; it does not prove a profitable trading strategy.
