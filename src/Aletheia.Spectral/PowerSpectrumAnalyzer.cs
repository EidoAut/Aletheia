using Aletheia.Mathematics;

namespace Aletheia.Spectral;

/// <summary>
/// Calculates power-spectrum diagnostics from a real-valued signal.
/// </summary>
public sealed class PowerSpectrumAnalyzer
{
    private readonly FftTransformer transformer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerSpectrumAnalyzer"/> class.
    /// </summary>
    /// <param name="transformer">The FFT transformer.</param>
    public PowerSpectrumAnalyzer(FftTransformer? transformer = null)
    {
        this.transformer = transformer ?? new FftTransformer();
    }

    /// <summary>
    /// Calculates a positive-frequency power spectrum over ordered observations.
    /// </summary>
    /// <param name="samples">The real-valued signal samples.</param>
    /// <param name="options">The spectral preprocessing options.</param>
    /// <returns>The spectral analysis result.</returns>
    public SpectralAnalysisResult Analyze(IReadOnlyList<double> samples, SpectralAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var effectiveOptions = options ?? new SpectralAnalysisOptions();

        if (samples.Count < 4)
        {
            return new SpectralAnalysisResult(
                Array.Empty<FrequencyBin>(),
                null,
                0d,
                0d,
                SpectralDiagnosticStrength.None,
                effectiveOptions,
                samples.Count,
                samples.Count,
                false,
                this.CalculateCoherentGain(samples.Count, effectiveOptions.Window));
        }

        ValidateFinite(samples);
        var preparedSignal = this.PrepareSignal(samples, effectiveOptions);
        var spectrum = this.transformer.Transform(preparedSignal, effectiveOptions.ZeroPadToPowerOfTwo);
        var coherentGain = this.CalculateCoherentGain(samples.Count, effectiveOptions.Window);
        var zeroPaddingApplied = spectrum.Length != samples.Count;
        var halfLength = spectrum.Length / 2;
        var bins = new List<FrequencyBin>(halfLength);

        for (var index = 1; index <= halfLength; index++)
        {
            var frequency = index / (double)spectrum.Length;
            var period = 1d / frequency;
            var isNyquist = spectrum.Length % 2 == 0 && index == halfLength;
            var oneSidedScale = isNyquist ? 1d : 2d;
            var amplitude = oneSidedScale * spectrum[index].Magnitude / (samples.Count * coherentGain);
            var power = (amplitude * amplitude) / 2d;
            bins.Add(new FrequencyBin(frequency, period, amplitude, power, spectrum[index].Phase));
        }

        if (bins.Count == 0)
        {
            return new SpectralAnalysisResult(
                bins,
                null,
                0d,
                0d,
                SpectralDiagnosticStrength.None,
                effectiveOptions,
                samples.Count,
                spectrum.Length,
                zeroPaddingApplied,
                coherentGain);
        }

        var totalPower = bins.Sum(bin => bin.Power);
        if (totalPower == 0d)
        {
            return new SpectralAnalysisResult(
                bins,
                null,
                0d,
                0d,
                SpectralDiagnosticStrength.None,
                effectiveOptions,
                samples.Count,
                spectrum.Length,
                zeroPaddingApplied,
                coherentGain);
        }

        var dominant = bins.OrderByDescending(bin => bin.Power).First();
        var peakPowerFraction = dominant.Power / totalPower;
        var nonDominantCount = Math.Max(1, bins.Count - 1);
        var backgroundPower = (totalPower - dominant.Power) / nonDominantCount;
        var peakToBackgroundRatio = backgroundPower == 0d ? double.PositiveInfinity : dominant.Power / backgroundPower;
        var diagnosticStrength = this.ClassifyStrength(samples.Count, peakPowerFraction);

        return new SpectralAnalysisResult(
            bins,
            dominant,
            peakPowerFraction,
            peakToBackgroundRatio,
            diagnosticStrength,
            effectiveOptions,
            samples.Count,
            spectrum.Length,
            zeroPaddingApplied,
            coherentGain);
    }

    /// <summary>
    /// Applies the configured deterministic signal preparation pipeline.
    /// </summary>
    /// <param name="samples">The input samples.</param>
    /// <param name="options">The preparation options.</param>
    /// <returns>The prepared signal.</returns>
    public double[] PrepareSignal(IReadOnlyList<double> samples, SpectralAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(options);
        this.ValidateFinite(samples);

        var prepared = samples.ToArray();
        if (options.DetrendingMode == SpectralDetrendingMode.MeanRemoval)
        {
            this.RemoveMeanInPlace(prepared);
        }
        else if (options.DetrendingMode == SpectralDetrendingMode.LinearDetrend)
        {
            this.RemoveLinearTrendInPlace(prepared);
        }

        if (options.Window == WindowFunction.Hann)
        {
            this.ApplyHannWindowInPlace(prepared);
        }

        return prepared;
    }

    /// <summary>
    /// Calculates the coherent gain of the configured window.
    /// </summary>
    /// <param name="sampleCount">The original signal sample count.</param>
    /// <param name="window">The applied window.</param>
    /// <returns>The mean window weight used for amplitude correction.</returns>
    public double CalculateCoherentGain(int sampleCount, WindowFunction window)
    {
        if (sampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "Sample count must be positive.");
        }

        if (window == WindowFunction.None || sampleCount == 1)
        {
            return 1d;
        }

        if (window != WindowFunction.Hann)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "Unsupported window function.");
        }

        var sum = 0d;
        for (var index = 0; index < sampleCount; index++)
        {
            sum += this.CalculateHannWeight(index, sampleCount);
        }

        return sum / sampleCount;
    }

    private SpectralDiagnosticStrength ClassifyStrength(int sampleCount, double peakPowerFraction)
    {
        if (sampleCount < 32 || peakPowerFraction <= 0d)
        {
            return SpectralDiagnosticStrength.Low;
        }

        if (peakPowerFraction >= 0.45d)
        {
            return SpectralDiagnosticStrength.High;
        }

        if (peakPowerFraction >= 0.25d)
        {
            return SpectralDiagnosticStrength.Medium;
        }

        return SpectralDiagnosticStrength.Low;
    }

    private void ValidateFinite(IReadOnlyList<double> samples)
    {
        for (var index = 0; index < samples.Count; index++)
        {
            if (double.IsNaN(samples[index]) || double.IsInfinity(samples[index]))
            {
                throw new ArgumentException("Spectral analysis requires finite samples.", nameof(samples));
            }
        }
    }

    private void RemoveMeanInPlace(double[] samples)
    {
        var mean = samples.Sum() / samples.Length;
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] -= mean;
        }
    }

    private void RemoveLinearTrendInPlace(double[] samples)
    {
        var x = Enumerable.Range(0, samples.Length).Select(value => (double)value).ToArray();
        var fit = LinearRegression.Fit(x, samples);
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] -= fit.Intercept + (fit.Slope * index);
        }
    }

    private void ApplyHannWindowInPlace(double[] samples)
    {
        if (samples.Length == 1)
        {
            return;
        }

        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] *= this.CalculateHannWeight(index, samples.Length);
        }
    }

    private double CalculateHannWeight(int index, int sampleCount)
    {
        return 0.5d * (1d - Math.Cos((2d * Math.PI * index) / (sampleCount - 1)));
    }
}
