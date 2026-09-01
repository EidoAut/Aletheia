# Drawdown

## Purpose

Drawdown measures loss from a running high-water mark.

## Equation

\[
D_t = \frac{P_t}{\max(P_0,\ldots,P_t)} - 1
\]

Maximum drawdown is the minimum value of `D_t`.

## Interpretation

Drawdown is reported as a negative return. A value of `-0.25` means a 25% peak-to-trough loss.

## Validation

Tests verify a known path where the worst loss is from `120` to `90`, producing `-25%`.
