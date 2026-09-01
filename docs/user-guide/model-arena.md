# Model Arena

Model Arena is the validation workbench. It runs default forecast models through walk-forward
evaluation under the same horizon, dataset, and metric rules.

## How To Run

=== "Desktop"
    Choose the calendar-day horizon in the header and click `RUN <days>D`.

=== "CLI"
    ```powershell
    dotnet run --project src/Aletheia.Cli -- arena sample
    dotnet run --project src/Aletheia.Cli -- arena examples/sample-fund.csv
    dotnet run --project src/Aletheia.Cli -- arena --provider cnmv-iic --fund ES0000000000
    ```

The CLI Arena uses the application default horizon of 90 calendar days. The desktop exposes a
user-selectable primary horizon.

## What It Displays

- model coverage and failure counts;
- point metrics such as MAE, RMSE, and directional accuracy;
- probability metrics such as Brier score and calibration;
- quantile and interval diagnostics when supported;
- common-support and non-overlapping subsets;
- baseline-relative skill;
- ranking eligibility and exclusion reasons.

## Interpretation

Arena ranking is not a universal truth score. It is a horizon-specific, dataset-specific comparison
under explicit evaluation rules. Models with too little evidence should remain visible but ineligible.

## Related Pages

- [Walk-Forward Validation](../validation/walk-forward-validation.md)
- [Common Support](../validation/common-support.md)
- [Model Baselines](../mathematics/validation/model-baselines.md)
