# Walk-Forward Validation

## Purpose

Walk-forward validation evaluates a model as if each historical forecast had been generated at the original cutoff date. The training set contains observations up to cutoff `t`; the target outcome is read only after the forecast is frozen.

## Formula

For cutoff `t` and horizon `h`:

\[
\begin{aligned}
\text{Train} &: x_1,\ldots,x_t,\\
\text{Predict} &: R_{t,t+h},\\
\text{Observe} &: \operatorname{realized}(R_{t,t+h}),\\
\text{Score} &: \operatorname{forecast}\ \text{versus realized return}
\end{aligned}
\]

## Interpretation

This measures out-of-sample behavior, not in-sample fit. A model can fit historical data well and still fail walk-forward validation.

## Assumptions

The data are ordered, cutoff dates are explicit, and target indices can be resolved from the forecast horizon.

## Limitations

Dense long-horizon forecasts overlap and should not be treated as independent experiments. Hyperparameter tuning and model-family selection should be performed inside a nested walk-forward loop, then evaluated on an untouched outer timeline.

## Implementation Notes

`WalkForwardEvaluator` creates expanding or rolling training windows, passes only the training prefix to each `IForecastModel`, freezes a `PredictionLedgerRecord`, then creates a separate `PredictionEvaluationRecord`.

`NestedWalkForwardValidator` supplies inner selection contexts that contain only the outer training prefix, so horizon or threshold choices cannot see the outer target. `FinalHoldoutSplitter` reserves a frozen tail segment for final evaluation after development decisions are made.
