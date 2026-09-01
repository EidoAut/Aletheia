# Numerical Derivatives

## Purpose

Numerical derivatives estimate local signal changes in the NAV trajectory.

## Units

Milestone 1.1 derivatives are computed over observation index, not elapsed calendar days. The dynamic state therefore names the features:

- `LogNavVelocityPerObservation`;
- `LogNavAccelerationPerObservationSquared`.

## Equations

Let:

\[
L_t = \ln(P_t)
\]

where `P_t` is NAV at observation index `t`.

First derivative per observation:

\[
v_t \approx L_t - L_{t-1}
\]

Second derivative per observation squared:

\[
a_t \approx L_t - 2L_{t-1} + L_{t-2}
\]

## Important Note

These are derived signal features, not literal physical velocity or acceleration. Using log NAV makes the local derivative scale invariant: prepending earlier history does not change the unit of the derivative by changing the base NAV level.

## Numerical Considerations

Differentiation amplifies noise, so Milestone 1.1 calculates derivatives from a smoothed log-NAV series. The smoothing method is explicit in `DynamicStateEstimatorOptions`.
