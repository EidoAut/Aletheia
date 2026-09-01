# Historical Analogues

Historical analogues compare the current state with previous state vectors and use realized outcomes
from the nearest historical matches.

## Intuition

The question is: "When this fund looked similar in the past, what happened over the same horizon?"

## Mathematical Definition

Aletheia compares state vectors over compatible dimensions. A standardized distance has the form:

\[
d(x, y) = \sqrt{\frac{1}{k}\sum_{j=1}^{k}\left(\frac{x_j - y_j}{s_j}\right)^2}
\]

where \(x\) is the current state, \(y\) is a candidate historical state, \(k\) is the number of
comparable features, and \(s_j\) is a feature scale. Timing analogues use robust median/MAD scaling.

## Implementation in Aletheia

`HistoricalAnalogueForecastModel`:

- builds states with the shared dynamic-state pipeline;
- requires schema compatibility;
- excludes candidates too close to the query cutoff;
- requires completed future outcomes for the requested horizon;
- returns an empirical distribution from analogue outcomes.

## Interpretation

Analogues are empirical evidence, not a guarantee. A small or clustered analogue set should reduce
confidence.

## Assumptions

- The chosen state features describe relevant similarity.
- Historical neighbors have enough completed same-horizon outcomes.
- Similarity is meaningful after schema and scale checks.

## Limitations

Analogues can fail when the current state is out of distribution, history is too short, or the state
schema changes.

## Source and Tests

- Source: `src/Aletheia.Validation/HistoricalAnalogueForecastModel.cs`,
  `src/Aletheia.Dynamics/HistoricalAnalogueFinder.cs`,
  `src/Aletheia.Dynamics/HistoricalAnalogueFeatureBuilder.cs`
- Tests: `tests/Aletheia.Dynamics.Tests`, `tests/Aletheia.Validation.Tests`
