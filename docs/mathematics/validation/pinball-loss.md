# Pinball Loss

## Purpose

Pinball loss evaluates forecast quantiles.

## Formula

For quantile level $\tau$ and forecast quantile $q$:

$$
L_{\tau}(y,q)=
\begin{cases}
\tau(y-q), & y \ge q,\\
(1-\tau)(q-y), & y < q
\end{cases}
$$

## Interpretation

Lower is better. The metric is asymmetric so under-forecasting and over-forecasting are penalized according to the quantile level.

## Assumptions

Forecast quantiles must be finite and monotonic.

## Limitations

Pinball loss evaluates each quantile separately and does not summarize a full distribution by itself.

## Implementation Notes

`PinballLossCalculator` evaluates percentiles available on the prediction record, currently the standard 10, 25, 50, 75, and 90 percentiles.
