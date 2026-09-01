# Forecast Horizons

A forecast horizon says how far into the future a model is asked to look. Aletheia makes the unit
explicit.

## Horizon Types

| Type | Meaning |
| --- | --- |
| `CalendarDays` | Elapsed calendar days from the cutoff date. |
| `Observations` | Number of future NAV observations. |

The core domain defines standard calendar horizons of 7, 30, 90, 180, and 365 calendar days and
standard observation horizons of 5, 21, 63, 126, and 252 observations. The current application
surfaces run current forecasts at 30, 90, 180, and 365 calendar days. The desktop Model Arena header
lets the user select a primary calendar-day validation horizon.

## Resolution

Algorithms that need a loop count use a resolved horizon:

```text
requested horizon + observation frequency + observation calendar -> effective observation count
```

For business-daily data, calendar-day horizons are converted with the weekday observation calendar.
For irregular histories, calendar-day conversion requires an explicit effective cadence or actual
future dates.

## Why Horizon Integrity Matters

Validation evidence from one horizon must not weight another horizon. A model that performed well at
90 calendar days has not thereby earned trust at 365 calendar days.

## Related Pages

- [Temporal Semantics](../mathematics/temporal-semantics.md)
- [Forecasting](../user-guide/forecasting.md)
- [Common Support](../validation/common-support.md)
