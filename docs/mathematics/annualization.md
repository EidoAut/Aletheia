# Annualization

## Purpose

Annualization converts a per-observation statistic into an annualized statistic. This conversion is only meaningful when the number of observations per year is defined.

## Default Convention

`StandardAnnualizationConvention` maps `ObservationFrequency` to periods per year:

- `Daily`: 365.25
- `BusinessDaily`: 252
- `Weekly`: 52
- `Monthly`: 12

For volatility:

$$
\sigma_{\text{annual}}
= \sigma_{\text{period}}\sqrt{\mathrm{periods\_per\_year}}
$$

Sharpe and Sortino use the same periods-per-year value to convert annual risk-free or target returns into per-period values and scale the resulting ratio.

## Irregular Data

`Irregular` observations are not silently annualized. A caller must provide an explicit periods-per-year value or use a future elapsed-time method that derives the convention from actual timestamps.

CAGR remains date-based because it uses actual elapsed calendar time between the first and last NAV observations.
