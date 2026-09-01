# Observation Frequency

Observation frequency describes how often NAV observations are reported. It is metadata, not a
resampling instruction.

## Supported Frequencies

| Frequency | Meaning |
| --- | --- |
| `Daily` | Calendar-daily observations. |
| `BusinessDaily` | Weekday-like observations with tolerance for isolated missing weekdays. |
| `Weekly` | Weekly cadence. |
| `Monthly` | Monthly cadence. |
| `Irregular` | No defensible regular cadence was detected. |

## Why It Matters

Frequency affects:

- return aggregation;
- annualization;
- forecast horizon resolution;
- dynamic-state interpretation;
- simulation scaling;
- validation target dates.

## Data Integrity

The detector classifies the series. It does not repair it. Missing provider values stay missing, and
the source observation count remains distinct from any effective analytical count.

## Related Pages

- [Temporal Semantics](../mathematics/temporal-semantics.md)
- [Data Provenance](data-provenance.md)
- [Data Layer](../architecture/data-layer.md)
