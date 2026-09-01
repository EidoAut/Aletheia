# Frozen Holdout

A frozen holdout is a final untouched segment reserved for evaluation after model design and
threshold selection are complete.

## Purpose

The holdout protects against repeatedly peeking at the same final sample while tuning research
choices.

## Implementation

`FinalHoldoutSplitter` and related options live in `Aletheia.Validation`. The helper separates a
development region from a final holdout region and records the boundary.

## Correct Interpretation

Passing a frozen holdout is stronger than passing the development period, but it is still historical
evidence. It does not guarantee future profitability.

## Common Mistakes

- changing thresholds after seeing holdout results;
- comparing many final candidates on the holdout and reporting only the winner;
- treating one holdout period as a universal market proof.

## Source and Tests

- Source: `src/Aletheia.Validation/FinalHoldoutProtocol.cs`
- Tests: `tests/Aletheia.Validation.Tests/ScientificProtocolTests.cs`
