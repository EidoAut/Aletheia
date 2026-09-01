# Sample Workflow

This workflow gives reviewers and new users a repeatable path through the application without
requiring CNMV access or external data files.

## 1. Confirm the Toolchain

```powershell
dotnet --version
dotnet restore Aletheia.sln
```

## 2. Run the Standard Analysis

```powershell
dotnet run --project src/Aletheia.Cli -- sample
```

Read the output in this order:

| Section | Question it answers |
| --- | --- |
| Dataset and provenance | What history is being analyzed? |
| Data quality | Is the data deep and regular enough for interpretation? |
| Performance and risk | What happened historically? |
| Dynamics and spectrum | What current state descriptors are visible? |
| Forecasts | What model outputs are available now? |
| Signal | What, if anything, can be defensibly summarized? |

## 3. Run Model Arena

```powershell
dotnet run --project src/Aletheia.Cli -- arena sample
```

The Arena evaluates the default model set under walk-forward rules and writes historical prediction
records to the configured SQLite ledger path. By default the path is:

```text
data/aletheia.db
```

Set `ALETHEIA_LEDGER_PATH` to place the ledger elsewhere.

## 4. Compare Timing Evidence and Economics

```powershell
dotnet run --project src/Aletheia.Cli -- timing sample
dotnet run --project src/Aletheia.Cli -- backtest sample --cost 0.001 --slippage 0.0005 --delay 1
```

The timing command evaluates probabilities. The backtest command evaluates whether historical
out-of-sample timing decisions survive delayed execution and costs.

## 5. Open the Desktop Shell

```powershell
dotnet run --project src/Aletheia.Desktop
```

Use the left sidebar as the analysis map. The sample dataset is enough to inspect the full UI,
including empty states that appear before Model Arena or prediction-ledger data are available.

## 6. Build the Documentation

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs build --strict
```
