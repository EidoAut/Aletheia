# Returns

## Purpose

Returns convert NAV levels into scale-independent quantities suitable for statistical analysis.

## Simple Return

\[
R_t = \frac{P_t - P_{t-1}}{P_{t-1}}
\]

Simple returns are intuitive and are used for performance reporting.

## Log Return

\[
r_t = \ln\left(\frac{P_t}{P_{t-1}}\right)
\]

Log returns are additive across consecutive periods and are used by forecasting and simulation baselines.

## Assumptions

NAV values must be strictly positive. Non-positive values are data-quality failures, not valid financial observations for return calculations.

## Validation

Unit tests cover known simple-return, log-return, and cumulative-return examples.
