# Periodic-investment simulation

Aletheia 2.4 adds a transparent Monte Carlo baseline for studying an initial investment followed by fixed end-of-month contributions. The result is a scenario distribution, not a validated forecast and not an investment recommendation.

## Inputs

The scenario is defined by:

- initial capital `B0`;
- fixed monthly contribution `c`;
- horizon in calendar months `T`;
- number of Monte Carlo paths `N`;
- deterministic random seed.

The active fund supplies historical per-observation log returns and an explicit observation-frequency classification.

## Moment scaling

Let the historical log returns be \(r_1,\ldots,r_n\), with sample mean \(\mu\) and sample standard deviation \(\sigma\). Aletheia maps the declared observation frequency to periods per year \(f\):

- calendar daily: `365.25`;
- business daily: `252`;
- weekly: `52`;
- monthly: `12`.

The number of historical observation periods represented by one month is

\[
m = \frac{f}{12}
\]

The Gaussian baseline then uses

\[
\mu_{\text{month}} = m\mu
\]

and

\[
\sigma_{\text{month}} = \sqrt{m}\sigma
\]

This scaling assumes independent increments with stable historical moments. It is a modeling convention, not evidence that the fund actually follows a Gaussian process.

Irregular observations are deliberately rejected because they do not imply a unique periods-per-year convention. A later model may support irregular timestamps through elapsed-time estimation or an explicit user-supplied calendar.

## Portfolio recursion

For path `j` and month `t`, Aletheia draws `Z(j,t)` from a standard normal distribution and applies

\[
B_{j,t}
= B_{j,t-1}\exp\left(\mu_{\text{month}} + \sigma_{\text{month}}Z_{j,t}\right) + c
\]

The contribution is added at the end of each simulated month. Therefore, the final contribution has no simulated return before the terminal measurement.

At each month, Aletheia records:

- total capital contributed;
- mean simulated value;
- P10, P25, median, P75 and P90 values.

The terminal diagnostics also include the fraction of paths whose final value is below total contributed capital.

## Determinism and workload guard

The same dataset, options and seed produce the same simulation trajectory. This supports regression tests and reproducible research.

Exact monthly percentiles require repeated cross-path ordering. To prevent accidental UI stalls, the simulator rejects workloads above 12,000,000 path-months. Users can trade horizon against path count while preserving a bounded calculation.

## Exclusions and interpretation

The baseline excludes taxes, inflation, subscription or redemption charges, custody costs and any product-level fee not already reflected in the historical NAV. Applying a management fee again to a NAV series that is already net of that fee would double count costs.

The model also excludes volatility clustering, fat tails, serial dependence, regime changes, changing contribution schedules and parameter uncertainty. The historical mean is especially unstable as an estimator of future drift.

Aletheia therefore labels the output `NO CALL`. The simulation answers a conditional question—what the value distribution looks like under stated assumptions—not whether the fund should be bought or sold.
