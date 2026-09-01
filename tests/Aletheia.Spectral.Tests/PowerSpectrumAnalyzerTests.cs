using Aletheia.Spectral;

namespace Aletheia.Spectral.Tests;

public sealed class PowerSpectrumAnalyzerTests
{
    [Fact]
    public void Analyze_WithSineWave_DetectsDominantPeriod()
    {
        const int period = 16;
        const double amplitude = 2.5d;
        var samples = Enumerable.Range(0, 256)
            .Select(index => amplitude * Math.Sin((2d * Math.PI * index) / period))
            .ToArray();
        var analyzer = new PowerSpectrumAnalyzer();

        var result = analyzer.Analyze(
            samples,
            new SpectralAnalysisOptions
            {
                DetrendingMode = SpectralDetrendingMode.None,
                Window = WindowFunction.None,
                ZeroPadToPowerOfTwo = false,
            });

        Assert.NotNull(result.DominantFrequency);
        Assert.Equal(period, result.DominantFrequency.PeriodObservations, 1);
        Assert.Equal(amplitude, result.DominantFrequency.Amplitude, 9);
        Assert.Equal(samples.Length, result.OriginalSampleCount);
        Assert.Equal(samples.Length, result.TransformLength);
        Assert.False(result.ZeroPaddingApplied);
        Assert.True(result.PeakPowerFraction > 0.40d);
    }

    [Fact]
    public void PrepareSignal_WithHannWindow_TapersEndpoints()
    {
        var analyzer = new PowerSpectrumAnalyzer();

        var prepared = analyzer.PrepareSignal(
            [1d, 1d, 1d, 1d],
            new SpectralAnalysisOptions
            {
                DetrendingMode = SpectralDetrendingMode.None,
                Window = WindowFunction.Hann,
                ZeroPadToPowerOfTwo = false,
            });

        Assert.Equal(0d, prepared[0], 9);
        Assert.Equal(0d, prepared[^1], 9);
        Assert.Equal(0.375d, analyzer.CalculateCoherentGain(4, WindowFunction.Hann), 9);
    }

    [Fact]
    public void Analyze_WithConstantSignal_ReturnsNoDominantComponent()
    {
        var analyzer = new PowerSpectrumAnalyzer();

        var result = analyzer.Analyze([4d, 4d, 4d, 4d, 4d, 4d, 4d, 4d]);

        Assert.Null(result.DominantFrequency);
        Assert.Equal(SpectralDiagnosticStrength.None, result.DiagnosticStrength);
    }

    [Fact]
    public void Analyze_WithZeroPadding_DoesNotChangeBinCenteredSinusoidAmplitude()
    {
        const int period = 16;
        const double amplitude = 1.75d;
        var samples = Enumerable.Range(0, 96)
            .Select(index => amplitude * Math.Sin((2d * Math.PI * index) / period))
            .ToArray();
        var analyzer = new PowerSpectrumAnalyzer();
        var optionsWithoutPadding = new SpectralAnalysisOptions
        {
            DetrendingMode = SpectralDetrendingMode.None,
            Window = WindowFunction.None,
            ZeroPadToPowerOfTwo = false,
        };
        var optionsWithPadding = optionsWithoutPadding with { ZeroPadToPowerOfTwo = true };

        var unpadded = analyzer.Analyze(samples, optionsWithoutPadding);
        var padded = analyzer.Analyze(samples, optionsWithPadding);

        Assert.NotNull(unpadded.DominantFrequency);
        Assert.NotNull(padded.DominantFrequency);
        Assert.False(unpadded.ZeroPaddingApplied);
        Assert.True(padded.ZeroPaddingApplied);
        Assert.Equal(samples.Length, padded.OriginalSampleCount);
        Assert.Equal(128, padded.TransformLength);
        Assert.Equal(unpadded.DominantFrequency.Amplitude, padded.DominantFrequency.Amplitude, 9);
    }

    [Fact]
    public void Analyze_WithHannWindow_CorrectsCoherentGainForAmplitude()
    {
        const int period = 16;
        const double amplitude = 1.25d;
        var samples = Enumerable.Range(0, 128)
            .Select(index => amplitude * Math.Sin((2d * Math.PI * index) / period))
            .ToArray();
        var analyzer = new PowerSpectrumAnalyzer();

        var result = analyzer.Analyze(
            samples,
            new SpectralAnalysisOptions
            {
                DetrendingMode = SpectralDetrendingMode.None,
                Window = WindowFunction.Hann,
                ZeroPadToPowerOfTwo = false,
            });

        Assert.NotNull(result.DominantFrequency);
        Assert.Equal(amplitude, result.DominantFrequency.Amplitude, 2);
        Assert.True(result.CoherentGain < 0.5d);
        Assert.Equal(WindowFunction.Hann, result.Options.Window);
    }
}
