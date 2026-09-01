# Spectral Analysis

## Purpose

Spectral analysis searches for periodic structure in a sampled signal.

Milestone 1.1 applies FFT over ordered observations, not continuous calendar time. Therefore the primary units are:

- `FrequencyCyclesPerObservation`;
- `PeriodObservations`.

A period of 20 observations must not be read as 20 calendar days unless the input series has a justified calendar sampling interval.

## FFT

The discrete Fourier transform is:

$$
X_k = \sum_{n=0}^{N-1}x_n\exp\left(-i\frac{2\pi kn}{N}\right)
$$

Milestone 1.2 implements a radix-2 Cooley-Tukey FFT, optional zero-padding to a power of two, and a direct DFT fallback when zero-padding is disabled for non-power-of-two inputs.

## Signal Preparation

Before the FFT, the analyzer validates finite input values and applies the configured preparation pipeline:

- no detrending, mean removal, or linear detrending;
- no window or Hann window;
- optional zero-padding to a power of two.

The Hann window is:

$$
w_n = \frac{1}{2}\left(1-\cos\left(\frac{2\pi n}{N-1}\right)\right)
$$

Windowing reduces spectral leakage by tapering the endpoints before the signal is interpreted as periodic by the discrete Fourier transform. Linear detrending is explicit because removing a trend changes the signal being analyzed.

The result records:

- original sample count;
- FFT transform length;
- zero-padding flag;
- detrending mode;
- window;
- coherent gain.

Zero-padding changes the displayed DFT grid. It does not add new physical information or improve the resolving power of the original observation window.

## Power Spectrum

Amplitude is reported as a one-sided real-signal amplitude spectrum. Non-DC and non-Nyquist bins use the factor-of-two one-sided scaling. Normalization uses the original sample count, not the padded FFT length. When a Hann window is applied, amplitude is corrected by the actual discrete coherent gain:

$$
\operatorname{CG} = \frac{1}{N}\sum_{n=0}^{N-1}w_n
$$

Power is defined from normalized amplitude as:

$$
\operatorname{Power} = \frac{\operatorname{Amplitude}^2}{2}
$$

This is a power spectrum, not a calibrated power spectral density. The dominant non-zero frequency is the positive-frequency bin with the largest power.
The result exposes diagnostic quantities such as peak power fraction and peak-to-background ratio. These are not statistical confidence probabilities.

## Rolling Stability

The rolling stability analyzer repeats the same spectral analysis over overlapping windows and reports:

- window detection rate;
- dominant-period persistence relative to the full-history dominant period;
- dominant-frequency variance across detected windows.

The purpose is to identify whether a dominant peak is persistent through time or appears only in the full-history spectrum.

## Scientific Caution

A spectral peak is not automatically a tradeable cycle. Peaks require persistence, out-of-sample validation, and comparison against noise and simple baselines.
