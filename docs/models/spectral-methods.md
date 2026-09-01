# Spectral Methods

Spectral methods inspect frequency structure in the ordered return signal.

## Intuition

Fourier analysis decomposes a signal into sinusoidal components. Aletheia uses this to detect
dominant observation-index periodicities and rolling spectral stability.

## Mathematical Definition

For observations \(x_0,\ldots,x_{n-1}\), the discrete Fourier transform is:

\[
X_k = \sum_{t=0}^{n-1} x_t e^{-2\pi i k t/n}
\]

Power is based on the squared magnitude of \(X_k\), with Aletheia's implementation documenting
normalization metadata.

## Implementation in Aletheia

`Aletheia.Spectral` owns FFT, inverse FFT, one-sided amplitude normalization, power spectrum,
dominant-frequency diagnostics, and rolling spectral stability. The timing engine treats spectral
timing as experimental unless causal out-of-sample feature reconstruction exists.

## Interpretation

A spectral peak can describe periodic structure in the sample. It is not automatically predictive and
should not be read as a trading cycle.

## Assumptions

- The ordered observations are meaningful in index time.
- Detrending/windowing choices are appropriate for the diagnostic.
- Sampling cadence is understood before calendar interpretation.

## Limitations

The implementation does not currently claim STFT, wavelets, or complete causal spectral timing
validation.

## Source and Tests

- Source: `src/Aletheia.Spectral`
- Tests: `tests/Aletheia.Spectral.Tests`

Related math: [Spectral Analysis](../mathematics/spectral-analysis.md).
