using System.Numerics;

namespace Aletheia.Spectral;

/// <summary>
/// Calculates the radix-2 Cooley-Tukey fast Fourier transform.
/// </summary>
/// <remarks>
/// Non-power-of-two inputs are zero-padded to the next power of two. Padding
/// changes frequency resolution but preserves the original samples.
/// </remarks>
public sealed class FftTransformer
{
    /// <summary>
    /// Calculates the forward FFT for real samples.
    /// </summary>
    /// <param name="samples">The real-valued samples.</param>
    /// <param name="zeroPadToPowerOfTwo">Whether non-power-of-two inputs should be zero-padded before FFT.</param>
    /// <returns>The complex spectrum.</returns>
    public Complex[] Transform(IReadOnlyList<double> samples, bool zeroPadToPowerOfTwo = true)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required for FFT.", nameof(samples));
        }

        if (!zeroPadToPowerOfTwo && !IsPowerOfTwo(samples.Count))
        {
            return TransformDirect(samples);
        }

        var length = zeroPadToPowerOfTwo ? NextPowerOfTwo(samples.Count) : samples.Count;
        var buffer = new Complex[length];
        for (var index = 0; index < samples.Count; index++)
        {
            buffer[index] = new Complex(samples[index], 0d);
        }

        TransformInPlace(buffer, inverse: false);
        return buffer;
    }

    /// <summary>
    /// Calculates the inverse FFT.
    /// </summary>
    /// <param name="spectrum">The complex spectrum.</param>
    /// <returns>The reconstructed complex samples.</returns>
    public Complex[] Inverse(IReadOnlyList<Complex> spectrum)
    {
        ArgumentNullException.ThrowIfNull(spectrum);

        if (spectrum.Count == 0)
        {
            throw new ArgumentException("At least one spectral value is required for inverse FFT.", nameof(spectrum));
        }

        var buffer = spectrum.ToArray();
        TransformInPlace(buffer, inverse: true);
        return buffer;
    }

    private static void TransformInPlace(Complex[] buffer, bool inverse)
    {
        BitReverseInPlace(buffer);

        for (var length = 2; length <= buffer.Length; length <<= 1)
        {
            var angle = 2d * Math.PI / length * (inverse ? 1d : -1d);
            var root = Complex.FromPolarCoordinates(1d, angle);

            for (var start = 0; start < buffer.Length; start += length)
            {
                var factor = Complex.One;
                var halfLength = length / 2;

                for (var offset = 0; offset < halfLength; offset++)
                {
                    var even = buffer[start + offset];
                    var odd = factor * buffer[start + offset + halfLength];
                    buffer[start + offset] = even + odd;
                    buffer[start + offset + halfLength] = even - odd;
                    factor *= root;
                }
            }
        }

        if (inverse)
        {
            for (var index = 0; index < buffer.Length; index++)
            {
                buffer[index] /= buffer.Length;
            }
        }
    }

    private static void BitReverseInPlace(Complex[] buffer)
    {
        var j = 0;
        for (var i = 1; i < buffer.Length; i++)
        {
            var bit = buffer.Length >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;

            if (i < j)
            {
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
        }
    }

    private static int NextPowerOfTwo(int value)
    {
        var power = 1;
        while (power < value)
        {
            power <<= 1;
        }

        return power;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private static Complex[] TransformDirect(IReadOnlyList<double> samples)
    {
        var result = new Complex[samples.Count];
        for (var k = 0; k < samples.Count; k++)
        {
            var sum = Complex.Zero;
            for (var n = 0; n < samples.Count; n++)
            {
                var angle = -2d * Math.PI * k * n / samples.Count;
                sum += samples[n] * Complex.FromPolarCoordinates(1d, angle);
            }

            result[k] = sum;
        }

        return result;
    }
}
