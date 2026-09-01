# Dynamic State

## Purpose

The dynamic state is a named vector of features describing the fund at a particular NAV observation date.

Milestone 1.2 makes the state schema explicit so models cannot accidentally mix incompatible dimensions or feature definitions.

## State Schema

`StateSchemaDescriptor` records:

- schema id;
- schema version;
- ordered dimensions;
- feature parameters;
- deterministic configuration fingerprint.

The default schema is `AletheiaStateSchema` version `v1.2`.

## Standard Dimensions

The default pipeline emits:

- `SimpleReturn`;
- `LogReturn`;
- `Trend`;
- `Momentum`;
- `Volatility`;
- `Drawdown`;
- `LogNavVelocityPerObservation`;
- `LogNavAccelerationPerObservationSquared`.

`SimpleReturn` is useful for intuitive reporting. `LogReturn` is used by log-return models such as AR(1). The names are intentionally separate.

## No Future Data

`IStateFeaturePipeline.Build(history, targetIndex)` constructs a state from the prefix ending at `targetIndex`. For historical state `i`, only observations `0..i` are visible.

This rule applies to:

- returns;
- rolling volatility;
- trend and momentum;
- drawdown;
- smoothed log-NAV derivatives.

The leakage test appends extreme future observations and verifies that the previously reconstructed historical state is unchanged.

## Data Adequacy

The state exposes `DataAdequacy`, not confidence. It is a heuristic sample-availability diagnostic, not a calibrated probability that the state is correct.

## Derivatives

Derivatives are calculated from smoothed log NAV over observation index. This avoids the arbitrary scale dependence that appears when using NAV normalized to the first available observation.

The derivative features are not calendar-time quantities. They are explicitly per observation.
