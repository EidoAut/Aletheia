# Returns and Annualization

Aletheia separates simple returns, logarithmic returns, and annualized quantities because confusing
these units can make results look more precise than they are.

## Simple Return

For NAV values $P_{t-1}$ and $P_t$, simple return is:

$$
r_t = \frac{P_t}{P_{t-1}} - 1
$$

Simple returns are intuitive for investor-facing percentages.

## Log Return

Log return is:

$$
g_t = \log(P_t) - \log(P_{t-1})
$$

Log returns add naturally over time and are used in several modeling paths.

## Annualization

Annualization requires an observation-frequency convention. Aletheia detects daily, business-daily,
weekly, monthly, or irregular cadence and applies frequency-aware scaling.

For irregular histories, Aletheia uses elapsed-time effective cadence derived from actual timestamps
instead of creating synthetic missing observations.

!!! warning "Do not compare annualized values blindly"
    Annualized volatility from regular business-daily data and annualized volatility from irregular
    elapsed-time cadence are both explicit conventions. They are not interchangeable without checking
    the underlying observation pattern.

## Related Pages

- [Returns](../mathematics/returns.md)
- [Annualization](../mathematics/annualization.md)
- [Observation Frequency](observation-frequency.md)
