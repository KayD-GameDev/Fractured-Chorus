using System.Diagnostics;
using System.Globalization;

namespace FracturedChorus.Tools.BeatAnalyzer;

internal static class Program
{
    private const int SampleRate = 22050;
    private const int FftSize = 1024;
    private const int HopSize = 256;

    private const double MinBpm = 70.0;
    private const double MaxBpm = 200.0;
    private const double PreferredMinBpm = 110.0;
    private const double PreferredMaxBpm = 190.0;

    private static int Main(string[] args)
    {
        var options = Options.Parse(args);

        var samples = string.IsNullOrEmpty(options.InputPath)
            ? ReadAllSamples(Console.OpenStandardInput())
            : ReadAllSamples(File.OpenRead(options.InputPath!));

        if (samples.Length == 0)
        {
            Console.Error.WriteLine("No PCM data. Pass --input <file.f32> or pipe f32le mono audio at 22050 Hz.");
            return 1;
        }

        var durationSec = options.DurationSec > 0
            ? options.DurationSec
            : samples.Length / (double)SampleRate;

        Console.Error.WriteLine(
            $"Decoded {samples.Length:N0} samples ({samples.Length / (double)SampleRate:F3}s @ {SampleRate} Hz)");

        var envelope = OnsetEnvelope.Compute(samples, FftSize, HopSize);
        var framesPerSecond = SampleRate / (double)HopSize;
        Console.Error.WriteLine($"Onset envelope: {envelope.Length:N0} frames @ {framesPerSecond:F3} fps");

        var coarse = TempoEstimator.EstimateCoarseBpm(envelope, framesPerSecond, MinBpm, MaxBpm);
        Console.Error.WriteLine(
            $"Coarse tempo: global best {coarse.GlobalBestBpm:F2} BPM, in-range pick {coarse.Bpm:F2} BPM");

        var grid = TempoEstimator.RefineGrid(
            envelope, framesPerSecond, coarse.Bpm, searchHalfWidthBpm: 2.0, stepBpm: 0.005);

        var beatSpanSec = 60.0 / grid.Bpm;
        var firstBeatSec = grid.FirstBeatSec;
        var totalBeats = (int)Math.Floor((durationSec - firstBeatSec) / beatSpanSec) + 1;
        var totalBars = totalBeats / 4;

        Console.WriteLine($"BPM={grid.Bpm.ToString("F4", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"FIRST_BEAT_SEC={firstBeatSec.ToString("F4", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"BEAT_SPAN_SEC={beatSpanSec.ToString("F6", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"TOTAL_BEATS={totalBeats}");
        Console.WriteLine($"TOTAL_BARS={totalBars}");
        Console.WriteLine($"DURATION_SEC={durationSec.ToString("F4", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"DOWNBEAT_PHASE={grid.DownbeatPhase}");
        Console.WriteLine($"GRID_SCORE={grid.Score.ToString("F4", CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrEmpty(options.ClickSourcePath))
        {
            ClickTrackWriter.Write(
                options.ClickSourcePath!,
                options.ClickOutputPath,
                firstBeatSec,
                beatSpanSec,
                totalBeats,
                durationSec);
        }

        return 0;
    }

    private static float[] ReadAllSamples(Stream source)
    {
        using var stream = source;
        using var buffered = new BufferedStream(stream, 1 << 20);
        using var memory = new MemoryStream();
        buffered.CopyTo(memory);

        var bytes = memory.GetBuffer();
        var count = (int)(memory.Length / sizeof(float));
        var samples = new float[count];
        Buffer.BlockCopy(bytes, 0, samples, 0, count * sizeof(float));
        return samples;
    }
}

internal sealed class Options
{
    public double DurationSec { get; private init; }
    public string? InputPath { get; private init; }
    public string? ClickSourcePath { get; private init; }
    public string ClickOutputPath { get; private init; } = "clicktrack.wav";

    public static Options Parse(string[] args)
    {
        double duration = 0;
        string? inputPath = null;
        string? clickSource = null;
        var clickOutput = "clicktrack.wav";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--duration" when i + 1 < args.Length:
                    duration = double.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                case "--input" when i + 1 < args.Length:
                    inputPath = args[++i];
                    break;
                case "--click" when i + 1 < args.Length:
                    clickSource = args[++i];
                    break;
                case "--click-out" when i + 1 < args.Length:
                    clickOutput = args[++i];
                    break;
            }
        }

        return new Options
        {
            DurationSec = duration,
            InputPath = inputPath,
            ClickSourcePath = clickSource,
            ClickOutputPath = clickOutput
        };
    }
}

internal static class OnsetEnvelope
{
    public static float[] Compute(float[] samples, int fftSize, int hopSize)
    {
        var window = new double[fftSize];
        for (var i = 0; i < fftSize; i++)
        {
            window[i] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (fftSize - 1));
        }

        var frameCount = Math.Max(0, (samples.Length - fftSize) / hopSize + 1);
        var flux = new float[frameCount];
        var bins = fftSize / 2;
        var previous = new double[bins];
        var real = new double[fftSize];
        var imag = new double[fftSize];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var start = frame * hopSize;
            for (var i = 0; i < fftSize; i++)
            {
                real[i] = samples[start + i] * window[i];
                imag[i] = 0.0;
            }

            Fft.Transform(real, imag);

            double sum = 0;
            for (var k = 0; k < bins; k++)
            {
                var magnitude = Math.Sqrt(real[k] * real[k] + imag[k] * imag[k]);
                var scaled = Math.Log(1.0 + 1000.0 * magnitude);
                var delta = scaled - previous[k];
                if (delta > 0)
                {
                    sum += delta;
                }

                previous[k] = scaled;
            }

            flux[frame] = (float)sum;
        }

        return Normalize(flux);
    }

    private static float[] Normalize(float[] flux)
    {
        const int movingAverageRadius = 8;
        var result = new float[flux.Length];

        for (var i = 0; i < flux.Length; i++)
        {
            var lo = Math.Max(0, i - movingAverageRadius);
            var hi = Math.Min(flux.Length - 1, i + movingAverageRadius);
            double sum = 0;
            for (var j = lo; j <= hi; j++)
            {
                sum += flux[j];
            }

            var mean = sum / (hi - lo + 1);
            var value = flux[i] - mean;
            result[i] = value > 0 ? (float)value : 0f;
        }

        double peak = 0;
        foreach (var value in result)
        {
            if (value > peak)
            {
                peak = value;
            }
        }

        if (peak > 0)
        {
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (float)(result[i] / peak);
            }
        }

        return result;
    }
}

internal readonly record struct CoarseTempo(double Bpm, double GlobalBestBpm);

internal readonly record struct BeatGrid(double Bpm, double FirstBeatSec, int DownbeatPhase, double Score);

internal static class TempoEstimator
{
    private static readonly double[] HarmonicWeights = { 1.0, 0.6, 0.4, 0.3 };

    public static CoarseTempo EstimateCoarseBpm(
        float[] envelope, double framesPerSecond, double minBpm, double maxBpm)
    {
        var maxLag = (int)Math.Ceiling(60.0 / minBpm * framesPerSecond * HarmonicWeights.Length) + 2;
        var autocorrelation = Autocorrelation(envelope, maxLag);

        var globalBestBpm = minBpm;
        double globalBestScore = double.NegativeInfinity;
        var rangeBestBpm = PreferredFallback(minBpm, maxBpm);
        double rangeBestScore = double.NegativeInfinity;

        for (var bpm = minBpm; bpm <= maxBpm; bpm += 0.02)
        {
            var period = 60.0 / bpm * framesPerSecond;
            double score = 0;
            for (var h = 0; h < HarmonicWeights.Length; h++)
            {
                var lag = (int)Math.Round(period * (h + 1));
                if (lag < autocorrelation.Length)
                {
                    score += HarmonicWeights[h] * autocorrelation[lag];
                }
            }

            if (score > globalBestScore)
            {
                globalBestScore = score;
                globalBestBpm = bpm;
            }

            if (bpm >= 110.0 && bpm <= 190.0 && score > rangeBestScore)
            {
                rangeBestScore = score;
                rangeBestBpm = bpm;
            }
        }

        return new CoarseTempo(rangeBestScore > double.NegativeInfinity ? rangeBestBpm : globalBestBpm, globalBestBpm);
    }

    public static BeatGrid RefineGrid(
        float[] envelope, double framesPerSecond, double centerBpm, double searchHalfWidthBpm, double stepBpm)
    {
        var best = new BeatGrid(centerBpm, 0, 0, double.NegativeInfinity);
        var lowBpm = Math.Max(MinimumBpm, centerBpm - searchHalfWidthBpm);
        var highBpm = centerBpm + searchHalfWidthBpm;

        for (var bpm = lowBpm; bpm <= highBpm; bpm += stepBpm)
        {
            var period = 60.0 / bpm * framesPerSecond;
            var beatCount = (int)((envelope.Length - 1) / period);
            if (beatCount < 8)
            {
                continue;
            }

            for (var offset = 0.0; offset < period; offset += 0.25)
            {
                double score = 0;
                for (var k = 0; k < beatCount; k++)
                {
                    score += SampleEnvelope(envelope, offset + k * period);
                }

                score /= beatCount;
                if (score <= best.Score)
                {
                    continue;
                }

                var phase = BestDownbeatPhase(envelope, offset, period, beatCount);
                var firstBeatFrame = offset + phase * period;
                best = new BeatGrid(bpm, firstBeatFrame / framesPerSecond, phase, score);
            }
        }

        return best;
    }

    private static int BestDownbeatPhase(float[] envelope, double offset, double period, int beatCount)
    {
        var bestPhase = 0;
        double bestScore = double.NegativeInfinity;

        for (var phase = 0; phase < 4; phase++)
        {
            double score = 0;
            var count = 0;
            for (var bar = 0; phase + 4 * bar < beatCount; bar++)
            {
                score += SampleEnvelope(envelope, offset + (phase + 4 * bar) * period);
                count++;
            }

            if (count == 0)
            {
                continue;
            }

            score /= count;
            if (score > bestScore)
            {
                bestScore = score;
                bestPhase = phase;
            }
        }

        return bestPhase;
    }

    private static double SampleEnvelope(float[] envelope, double position)
    {
        if (position < 0 || position >= envelope.Length - 1)
        {
            return 0;
        }

        var index = (int)position;
        var fraction = position - index;
        return envelope[index] * (1.0 - fraction) + envelope[index + 1] * fraction;
    }

    private static double[] Autocorrelation(float[] envelope, int maxLag)
    {
        var result = new double[maxLag + 1];
        for (var lag = 1; lag <= maxLag; lag++)
        {
            double sum = 0;
            var limit = envelope.Length - lag;
            for (var i = 0; i < limit; i++)
            {
                sum += envelope[i] * envelope[i + lag];
            }

            result[lag] = limit > 0 ? sum / limit : 0;
        }

        return result;
    }

    private const double MinimumBpm = 40.0;

    private static double PreferredFallback(double minBpm, double maxBpm)
    {
        return Math.Clamp(148.0, minBpm, maxBpm);
    }
}

internal static class Fft
{
    public static void Transform(double[] real, double[] imag)
    {
        var n = real.Length;
        if (n <= 1)
        {
            return;
        }

        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2.0 * Math.PI / length;
            var wReal = Math.Cos(angle);
            var wImag = Math.Sin(angle);

            for (var start = 0; start < n; start += length)
            {
                double curReal = 1.0;
                double curImag = 0.0;
                var half = length >> 1;

                for (var k = 0; k < half; k++)
                {
                    var evenReal = real[start + k];
                    var evenImag = imag[start + k];
                    var oddReal = real[start + k + half] * curReal - imag[start + k + half] * curImag;
                    var oddImag = real[start + k + half] * curImag + imag[start + k + half] * curReal;

                    real[start + k] = evenReal + oddReal;
                    imag[start + k] = evenImag + oddImag;
                    real[start + k + half] = evenReal - oddReal;
                    imag[start + k + half] = evenImag - oddImag;

                    var nextReal = curReal * wReal - curImag * wImag;
                    curImag = curReal * wImag + curImag * wReal;
                    curReal = nextReal;
                }
            }
        }
    }
}

internal static class ClickTrackWriter
{
    private const int OutputSampleRate = 44100;
    private const int Channels = 2;
    private const double ClickDurationSec = 0.015;

    public static void Write(
        string sourcePath,
        string outputPath,
        double firstBeatSec,
        double beatSpanSec,
        int totalBeats,
        double durationSec)
    {
        var music = DecodeStereo(sourcePath);
        if (music.Length == 0)
        {
            Console.Error.WriteLine("Click track skipped: could not decode source audio.");
            return;
        }

        var clickSamples = (int)(ClickDurationSec * OutputSampleRate);

        for (var beat = 0; beat < totalBeats; beat++)
        {
            var timeSec = firstBeatSec + beat * beatSpanSec;
            if (timeSec >= durationSec)
            {
                break;
            }

            var isDownbeat = beat % 4 == 0;
            var frequency = isDownbeat ? 1600.0 : 1000.0;
            var amplitude = isDownbeat ? 0.55 : 0.32;
            var startFrame = (int)(timeSec * OutputSampleRate);

            for (var i = 0; i < clickSamples; i++)
            {
                var frame = startFrame + i;
                var index = frame * Channels;
                if (index + 1 >= music.Length)
                {
                    break;
                }

                var envelope = 1.0 - i / (double)clickSamples;
                var value = Math.Sin(2.0 * Math.PI * frequency * i / OutputSampleRate) * envelope * amplitude;
                music[index] = ClampToShort(music[index] * 0.55 + value * short.MaxValue);
                music[index + 1] = ClampToShort(music[index + 1] * 0.55 + value * short.MaxValue);
            }
        }

        WriteWav(outputPath, music);
        Console.Error.WriteLine($"Click track written: {Path.GetFullPath(outputPath)}");
    }

    private static short ClampToShort(double value)
    {
        return (short)Math.Clamp(value, short.MinValue, short.MaxValue);
    }

    private static short[] DecodeStereo(string sourcePath)
    {
        var startInfo = new ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-ac");
        startInfo.ArgumentList.Add(Channels.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-ar");
        startInfo.ArgumentList.Add(OutputSampleRate.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("s16le");
        startInfo.ArgumentList.Add("-");

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return Array.Empty<short>();
        }

        using var memory = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(memory);
        process.WaitForExit();

        var bytes = memory.GetBuffer();
        var count = (int)(memory.Length / sizeof(short));
        var samples = new short[count];
        Buffer.BlockCopy(bytes, 0, samples, 0, count * sizeof(short));
        return samples;
    }

    private static void WriteWav(string path, short[] samples)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        var dataBytes = samples.Length * sizeof(short);
        var byteRate = OutputSampleRate * Channels * sizeof(short);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)Channels);
        writer.Write(OutputSampleRate);
        writer.Write(byteRate);
        writer.Write((short)(Channels * sizeof(short)));
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataBytes);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }
}
