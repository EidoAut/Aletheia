# Limitations

Aletheia is rigorous about many internal boundaries, but it is not a complete investment-science
proof machine.

## Current Scientific Limits

- The implemented fund-analysis workflow is primarily univariate on fund NAV history.
- CNMV IIC is the first official provider, not a global fund universe.
- Provider coverage and survivorship properties are not solved by the application.
- Spectral peaks remain descriptive unless causally validated as timing predictors.
- Market-timing economics depend on costs, delay, turnover, and sample support.
- Calibration and `ReliabilityIndex` do not prove profitability.
- Historical walk-forward predictions are not a live track record.
- Final holdout helpers exist, but research discipline still depends on how they are used.

## Not Implemented as of Product 2.7.3

- broker integration;
- portfolio optimization;
- tax-aware execution;
- liquidity-aware execution;
- multivariate factor models;
- STFT and wavelet spectral analysis;
- a global fund universe with survivorship-bias controls.

## Statements Requiring Independent Validation

Any claim that Aletheia can generate economically profitable investment decisions requires
independent scientific validation beyond this repository: out-of-sample or live-forward evidence,
realistic costs and slippage, holdout discipline, robustness checks, and review of fund-universe
selection effects.
