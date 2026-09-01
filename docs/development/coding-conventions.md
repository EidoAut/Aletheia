# Coding Conventions

Aletheia favors explicit semantics, deterministic calculations, and narrow ownership boundaries.

## General Practices

- Use nullable reference types and finite-input validation.
- Keep mathematical routines deterministic and side-effect free where practical.
- Use domain types such as `ForecastHorizon`, `ObservationFrequency`, `DatasetIdentity`, and
  `StateSchemaDescriptor` instead of ambiguous primitive values.
- Return typed model failures for normal insufficient-data conditions.
- Keep provider parsing in `Aletheia.Data`, not in UI or model code.
- Keep presentation models in `Aletheia.Application`, not in core math projects.

## Scientific Naming

Prefer names that carry units:

- `CalendarDays`, not generic `Days`;
- `EffectiveObservationCount`, not generic `Periods`;
- `ExpectedSimpleReturn`, not generic `Forecast`;
- `LogNavVelocityPerObservation`, not generic `Velocity`.

## Tests

Add tests at the project boundary where the behavior belongs. Causality and validation tests should
mutate future data or compare support sets when that is the risk being protected.

## Documentation

When adding behavior, update the relevant user guide, model page, validation page, and source map.
Avoid line-number references because they become stale.
