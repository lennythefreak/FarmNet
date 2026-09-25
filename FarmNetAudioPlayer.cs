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
        output != null;

    public void Start(
        int sampleRate,
        int channels)
    {
        if (initialized)
            return;

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
            false;

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
            buffer.AddSamples(
                data,
                0,
                data.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet AUDIO: " +
                $"Output buffer error: {ex}");
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