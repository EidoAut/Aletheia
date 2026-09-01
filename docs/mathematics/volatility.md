# Volatility

## Purpose

Volatility estimates dispersion of periodic returns.

## Equation

Milestone 1 uses sample standard deviation:

$$
s = \sqrt{\frac{\sum_{i=1}^{n}(r_i-\bar{r})^2}{n-1}}
$$

Annualized volatility is:

$$
s_{\text{annual}} = s_{\text{period}}\sqrt{\mathrm{periods\_per\_year}}
$$

`periods_per_year` is resolved from `ObservationFrequency` by the annualization convention:

- `Daily`: 365.25
- `BusinessDaily`: 252
- `Weekly`: 52
- `Monthly`: 12

`Irregular` series are not annualized without an explicit convention.

## Numerical Considerations

The implementation returns zero for insufficient data rather than implying a statistically meaningful estimate. For sufficient irregular data, annualized calculations fail unless the caller supplies an explicit periods-per-year convention.

## Limitations

Volatility is backward-looking and does not capture regime-dependent or asymmetric risk by itself.
