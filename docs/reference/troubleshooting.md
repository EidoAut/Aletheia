# Troubleshooting

## `Calendar-day horizons cannot be converted for irregular observations`

The history was detected as irregular and no explicit effective cadence was available. Use
observation-count workflows where possible or inspect the source data cadence.

## `NO CALL`

This means Aletheia cannot defend a directional conclusion from available evidence. Check data
quality, forecast status, validation availability, OOD state, and timing diagnostics.

## `NO RELIABLE ECONOMIC BACKTEST`

There were too few usable historical out-of-sample timing decisions, or the validated timing ensemble
was inactive. This is an evidence limit, not a crash.

## Provider Search Fails

CNMV is remote and can be affected by network, provider-site, or payload-format changes. Check the
diagnostic message for requested URI, final URI, content type, payload size, cache/network origin, and
rejection reason.

## Desktop Looks Empty

Load a dataset first. Some pages also require Model Arena or prediction-ledger rows before advanced
tables become populated.

## Docs Build Fails

Run:

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs build --strict
```

Strict mode reports missing nav items, broken internal links, and Markdown issues that must be fixed
before publishing.
