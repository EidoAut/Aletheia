# Forecasting

## Purpose

Aletheia forecasts distributions, not exact future NAV values.

## Target

Conceptually, forecasting estimates:

$$
P(r_{t,t+h}\mid x_t)
$$

where `x_t` is the current reconstructed state and `h` is the horizon.

## Horizon Semantics

A forecast request carries an explicit unit:

- `CalendarDays`: elapsed calendar time from the data cutoff date.
- `Observations`: a count of future fund valuation observations.

Internal simulation and observation-index models operate on an effective observation count. Calendar-day horizons are resolved through an observation calendar before simulation. The resolution metadata records:

- requested horizon;
- effective observation count;
- target date when known;
- calendar or sampling assumption used for the conversion.

For example, a request for `30 calendar days` under the default weekday calendar may resolve to about 21 fund observations. The code does not treat these as interchangeable units.

## Milestone 1.1 Baseline

The naive baseline uses realized historical horizon returns when enough samples exist. For short series, it falls back to a deterministic Gaussian approximation fitted to per-observation log-return moments.

For observation horizons, historical outcomes are selected by index. For calendar-day horizons, outcomes are selected by the first observation on or after the target calendar date.

## Output

Forecast distributions expose:

- requested horizon and horizon-resolution metadata;
- expected return;
- median return;
- selected percentiles;
- probability of positive return;
- probability of return above 5%;
- probability of loss worse than 10%.

## Limitations

The baseline is not calibrated, regime-aware, or validated for signal generation. The default calendar is a weekday approximation, not a fund-specific holiday calendar.
