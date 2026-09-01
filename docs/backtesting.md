# Economic Timing Backtesting

Aletheia includes a separate economic backtesting module in `Aletheia.Simulation` for converting historical out-of-sample timing signals into simulated portfolio decisions. It is intentionally separate from probability validation: a good Brier score or calibration summary is not automatically an investment strategy.

## Execution Semantics

`TimingDecisionBacktester` consumes:

- a `NavSeries`;
- historical `TimingBacktestSignal` records with calculation date, decision date, signal date and target exposure;
- `TimingBacktestOptions` with transaction costs, slippage, execution delay, maximum gross exposure, and annualization cadence.

Execution delay defaults to one observation. A signal generated from the NAV of date `T` cannot be applied to the return ending at `T`, and if it executes on the next valuation date it cannot benefit from the `T -> T+1` return. The implementation first realizes the return into the execution NAV using the previous exposure, then charges costs and changes exposure for later intervals. This avoids same-NAV look-ahead execution and accidental extra delay.

Buy-and-hold starts with full exposure at the first NAV and therefore captures the first observed return interval. Its initial purchase cost is explicit through `ChargeInitialFixedExposureCost`; the default charges the inception fixed-exposure trade so costed comparisons do not get a free initial position. `Neutral/no-action` starts with zero exposure, never trades, and stays flat.

For regular frequencies, annualized return, volatility, Sharpe, Sortino and Calmar use the repository's standard annualization convention. For irregular histories, annualized return uses effective elapsed calendar time between the first and last observation, and scaled risk metrics use the elapsed-time cadence estimator. No missing observations are interpolated or invented.

## Comparisons

The module reports comparable paths for:

- `Aletheia timing`, driven by the supplied historical signals;
- `Buy-and-hold`, fixed full exposure from inception;
- `Neutral/no-action`, fixed zero exposure.

The application layer supplies signals through the causal chain:

```text
historical OOS timing predictions
    -> decisions available on each historical date
    -> target exposure
    -> delayed execution
    -> economic backtest
    -> comparison with buy-and-hold and neutral/no-action
```

If there are not enough usable historical OOS timing decisions, Aletheia reports `NO RELIABLE ECONOMIC BACKTEST`. It does not reconstruct trading curves from in-sample labels, current probabilities, or future information.

Periodic contribution baselines remain in the investment-plan simulator. They should be compared separately from trading-signal backtests unless contribution timing and exposure rules are made identical.

## Costs And Metrics

Costs are charged when exposure changes:

$$
\mathrm{cost}_t
= V_t\left|\mathrm{targetExposure}_t-\mathrm{exposure}_{t-1}\right|
  \left(\mathrm{transactionCost}+\mathrm{slippage}\right)
$$

The result includes cumulative and annualized return, annualized volatility, Sharpe, Sortino, maximum drawdown, Calmar, total turnover, time in market, trade count, and the full normalized value path.

CLI usage:

```powershell
dotnet run --project src/Aletheia.Cli -- backtest sample
dotnet run --project src/Aletheia.Cli -- backtest examples/sample-fund.csv --cost 0.001 --slippage 0.0005 --delay 1
dotnet run --project src/Aletheia.Cli -- backtest --provider cnmv-iic --fund ES0000000000 --from 2024-01-01 --to 2024-12-31
```

The Desktop `Market Timing` page shows the economic backtest status, diagnostic, timing horizon, signal count, delay, costs, signal trace context, and strategy metrics when reliable results exist.

## Limitations

The module does not optimize thresholds, does not use holdout data to tune signals, and does not infer signals from probabilities by itself. The caller must supply historical signals that were produced out of sample. A statistically attractive model can still fail this economic test after costs, slippage, turnover, delayed execution, or drawdown.
