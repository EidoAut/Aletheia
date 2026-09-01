# Nested Model Selection

Nested model selection protects against tuning a model on the same outcomes later used to claim
performance.

## Intuition

If hyperparameters are chosen because they look best on a validation period, that validation period
has influenced model design. Nested validation keeps the selection step inside an outer training
prefix so the outer target remains unseen.

## Implementation

`NestedWalkForwardValidator` supplies inner selection contexts that contain only the outer training
prefix. The current repository includes helpers for this protocol rather than a universal automated
optimizer for every model family.

## Correct Use

1. Split the historical timeline into outer walk-forward contexts.
2. Inside each outer training prefix, compare candidate settings.
3. Select a setting without seeing the outer target.
4. Evaluate the selected setting on the outer target.
5. Keep final holdout data untouched until development decisions are complete.

## Limitations

Nested validation reduces tuning leakage but does not remove all model-selection risk. A large number
of manual research attempts can still overfit the development data.

## Source and Tests

- Source: `src/Aletheia.Validation/NestedWalkForwardSelection.cs`
- Tests: `tests/Aletheia.Validation.Tests/MarketTimingEngineTests.cs`,
  `tests/Aletheia.Validation.Tests/ScientificProtocolTests.cs`
