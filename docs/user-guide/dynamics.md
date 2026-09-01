# Dynamics

The Dynamics page displays state variables derived from the fund NAV history. It helps users inspect
the current condition of the time series before reading forecasts or timing labels.

## What It Consumes

- ordered NAV observations;
- simple and log returns;
- the shared dynamic-state feature pipeline;
- observation-frequency metadata.

## What It Displays

The current state includes named dimensions such as return, log-return velocity, acceleration,
volatility, momentum, drawdown, and model-derived state descriptors when available.

## Interpretation

Dynamic state is descriptive context. It can explain why analogue, forecast, or timing models see
the current point as ordinary or unusual. It is not itself a validated predictive claim.

!!! warning "Schema compatibility"
    Historical analogue searches require state vectors with compatible schema fingerprints. This
    prevents comparing a current state built with one feature set against historical states built
    with another.

## Related Pages

- [Dynamic State](../mathematics/dynamic-state.md)
- [State Schema Identity](../mathematics/state-schema.md)
- [GARCH(1,1)](../models/garch.md)
- [Hidden Markov Model](../models/hidden-markov-model.md)
