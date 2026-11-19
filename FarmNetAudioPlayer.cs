using System;
using NAudio.Wave;

namespace FarmNet;

public class FarmNetAudioPlayer : IDisposable
{
    private WaveOutEvent? output;

    private BufferedWaveProvider? buffer;

    private int sampleRate;
    private int channels;

    private bool initialized;

    public bool IsPlaying =>
        output != null &&
        output.PlaybackState == PlaybackState.Playing;

    public double BufferedSeconds =>
        buffer == null
            ? 0
            : buffer.BufferedBytes / (double)buffer.WaveFormat.AverageBytesPerSecond;

    public void Start(
        int sampleRate,
        int channels)
    {
        if (output != null && output.PlaybackState == PlaybackState.Playing)
            return;

        if (initialized || output != null)
            Stop();

        if (sampleRate <= 0)
            return;

        if (channels <= 0)
            return;

        this.sampleRate =
            sampleRate;

        this.channels =
            channels;

        /*
         * Chromium is giving us 32-bit floating-point
         * PCM audio.
         */
        WaveFormat format =
            WaveFormat.CreateIeeeFloatWaveFormat(
                sampleRate,
                channels);

        buffer =
            new BufferedWaveProvider(
                format);

        /*
         * Give the browser some room to absorb
         * timing differences between Chromium and
         * the Windows audio device.
         */
        buffer.BufferDuration =
            TimeSpan.FromSeconds(2);

        buffer.ReadFully =
            true;

        output =
            new WaveOutEvent();

        output.Init(
            buffer);

        output.Play();

        initialized =
            true;

        Console.WriteLine(
            $"FarmNet AUDIO: OUTPUT STARTED " +
            $"{sampleRate}Hz / {channels} channels");
    }

    public void AddAudio(
        byte[] data)
    {
        if (!initialized)
            return;

        if (buffer == null)
            return;

        if (data.Length == 0)
            return;

        try
        {
            if (data.Length > buffer.BufferLength)
                return;

            // Discard old buffered audio rather than allowing
            // a backlog to permanently fill the output.
            if (buffer.BufferedBytes + data.Length > buffer.BufferLength)
                buffer.ClearBuffer();

            buffer.AddSamples(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet AUDIO: Output buffer error: {ex.Message}");
        }
    }

    public void ClearBuffer()
    {
        try
        {
            buffer?.ClearBuffer();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet AUDIO: Could not clear output buffer: {ex.Message}");
        }
    }
    public void Stop()
    {
        if (!initialized)
            return;

        try
        {
            output?.Stop();
        }
        catch
        {
        }

        output?.Dispose();

        output =
            null;

        buffer =
            null;

        initialized =
            false;

        Console.WriteLine(
            "FarmNet AUDIO: OUTPUT STOPPED");
    }

    public void Dispose()
    {
        Stop();
    }
}


