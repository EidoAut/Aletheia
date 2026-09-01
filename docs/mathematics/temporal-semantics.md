# Temporal Semantics

## Principle

Aletheia treats calendar time, observation index time, and fund valuation time as different units.

Ambiguous names such as `Days`, `Period`, or `Velocity` are avoided when the unit matters. Public types and state dimensions carry explicit semantics.

## Forecast Horizons

`ForecastHorizon` contains:

- `Value`;
- `Unit`.

The supported units are:

- `CalendarDays`: elapsed calendar days from a cutoff date;
- `Observations`: future NAV observations.

Standard calendar horizons are 7, 30, 90, 180, and 365 calendar days. Standard observation horizons are 5, 21, 63, 126, and 252 observations.

## Horizon Resolution

Algorithms that need a loop count use `ForecastHorizonResolution`, not the raw request. It records:

- requested horizon;
- observation frequency;
- effective observation count;
- target date, when known;
- resolution policy name;
- approximation flag.

The resolver combines `ForecastHorizon`, `ObservationFrequency`, and `IObservationCalendar`.

- `Daily`: calendar days and observations can align.
- `BusinessDaily`: the default weekday calendar converts calendar days to observation counts.
- `Weekly`: calendar horizons are mapped to weekly observation steps.
- `Monthly`: calendar horizons are mapped to monthly observation steps.
- `Irregular`: calendar horizons are rejected unless actual future observation dates are supplied by a future component.

The default `WeekdayObservationCalendar` treats Monday through Friday as possible observation dates. It is intentionally small and documented as an approximation. Future fund-specific calendars can replace it through `IObservationCalendar`.

## Observation Frequency

`NavSeries` and `TimeSeries<T>` carry `ObservationFrequency` metadata:

- `Irregular`;
- `Daily`;
- `BusinessDaily`;
- `Weekly`;
- `Monthly`.

This metadata does not resample data by itself. It tells algorithms and users how the observations should be interpreted.

Transformations that preserve the observation cadence preserve this metadata. Simple returns, log returns, rolling returns, moving averages, rolling volatility, smoothing, and numerical derivatives therefore keep the source frequency unless an operation explicitly changes sampling semantics.

## Spectral Time

FFT analysis currently operates in ordered observation-index space. A spectral period is reported as observations, not calendar days. Calendar conversion should only be added when the input sampling interval is known and valid.
