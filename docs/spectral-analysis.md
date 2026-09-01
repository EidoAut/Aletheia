# Spectral Analysis

Aletheia separates spectral description from predictive evidence. A frequency component can be visible in historical data while still having no out-of-sample forecasting value.

## Existing Spectrum Pipeline

The spectrum pipeline works on ordered observations after the configured preprocessing step. It computes a one-sided frequency representation, dominant frequency, period in observations, peak power fraction, and phase. Rolling stability diagnostics measure whether the dominant period persists across windows.

Periods are expressed in observations, not calendar days. Calendar interpretation must go through the series cadence metadata.

## Component Evidence

`SpectralEvidenceAnalyzer` converts the full-sample spectrum and rolling stability into `SpectralComponentEvidence`:

- `PeriodObservations`;
- `FrequencyCyclesPerObservation`;
- `RelativePower`;
- `Stability`;
- `Phase`;
- `Reliability`;
- `Diagnostic`.

Reliability is conservative:

$$
\mathrm{reliability}
= \sqrt{\mathrm{relativePower}\cdot\mathrm{rollingStability}}
$$

This means a component needs both high relative power and repeated rolling persistence before it receives a strong evidence score.

## Interpretation Rules

- High spectral reliability means "historically visible and stable", not "tradable".
- Spectral evidence can support a research report but is not enough for a directional signal.
- Predictive use must be demonstrated separately through walk-forward validation and the Model Arena.
- Weak or unstable components are retained as diagnostics rather than upgraded into forecasts.

## Failure Modes

No component evidence is emitted when the spectrum has no dominant frequency or no bins. Rolling instability lowers reliability rather than throwing away the report.
