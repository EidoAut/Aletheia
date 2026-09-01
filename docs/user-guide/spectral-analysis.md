# Spectral Analysis

The Spectral page shows FFT-based diagnostics over ordered log-return observations.

## What It Shows

- dominant observation-index frequencies and periods;
- one-sided amplitude and power spectrum diagnostics;
- rolling spectral stability;
- transform metadata used for reproducibility.

## Interpretation

Spectral analysis can reveal periodic structure in a finite historical signal. A peak is only a
diagnostic. It is not automatically a tradable cycle, and it is not a validated timing model unless
causal out-of-sample evidence supports that use.

!!! danger "Do not calendar-convert blindly"
    Current spectral periods are measured in observations. Calendar-day interpretation requires a
    valid sampling cadence and should remain explicit.

## Related Pages

- [Spectral Methods](../models/spectral-methods.md)
- [Spectral Analysis Notes](../mathematics/spectral-analysis.md)
- [Causality and Look-Ahead](../concepts/causality-and-look-ahead.md)
