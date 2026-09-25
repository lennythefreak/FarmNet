using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using CefSharp;
using CefSharp.Handler;
using CefSharp.Structs;

namespace FarmNet;

public class FarmNetAudioHandler : AudioHandler
{
    private readonly ConcurrentQueue<byte[]> audioQueue = new();

    private readonly object audioLock = new();

    private int sampleRate;
    private int channels;

    public int SampleRate =>
        sampleRate;

    public int Channels =>
        channels;

    public bool AudioStreamActive
    {
        get;
        private set;
    }

    public long PacketCount
    {
        get;
        private set;
    }

    protected override bool GetAudioParameters(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        ref AudioParameters parameters)
    {
        sampleRate =
            parameters.SampleRate;

        Console.WriteLine(
            $"FarmNet AUDIO: GetAudioParameters " +
            $"SampleRate={parameters.SampleRate}");

        return true;
    }

    protected override void OnAudioStreamStarted(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        AudioParameters parameters,
        int channels)
    {
        lock (audioLock)
        {
            ClearQueue();
        }

        sampleRate =
            parameters.SampleRate;

        this.channels =
            channels;

        PacketCount =
            0;

        AudioStreamActive =
            true;

        Console.WriteLine(
            "FarmNet AUDIO: STREAM STARTED");

        Console.WriteLine(
            $"FarmNet AUDIO: " +
            $"SampleRate={sampleRate}, " +
            $"Channels={channels}");
    }

    protected override void OnAudioStreamPacket(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IntPtr data,
        int noOfFrames,
        long pts)
    {
        if (data == IntPtr.Zero)
            return;

        if (noOfFrames <= 0)
            return;

        if (channels <= 0)
            return;

        try
        {
            /*
             * IMPORTANT:
             *
             * CefSharp gives us:
             *
             * Single**
             *
             * This means DATA is a pointer to an
             * array of pointers.
             *
             * Each pointer represents one audio
             * channel.
             *
             * Example for stereo:
             *
             * data
             *   |
             *   +--> channel 0 samples
             *   |
             *   +--> channel 1 samples
             *
             * The samples themselves are 32-bit floats.
             */

            float[][] channelSamples =
                new float[channels][];

            for (int channel = 0;
                 channel < channels;
                 channel++)
            {
                IntPtr channelPointer =
                    Marshal.ReadIntPtr(
                        data,
                        channel * IntPtr.Size);

                if (channelPointer == IntPtr.Zero)
                    return;

                float[] samples =
                    new float[noOfFrames];

                Marshal.Copy(
                    channelPointer,
                    samples,
                    0,
                    noOfFrames);

                channelSamples[channel] =
                    samples;
            }

            /*
             * Convert planar audio into interleaved audio.
             *
             * CefSharp:
             *
             * L L L L
             * R R R R
             *
             * NAudio:
             *
             * L R L R L R L R
             */

            float[] interleaved =
                new float[
                    noOfFrames * channels];

            int outputIndex =
                0;

            for (int frame = 0;
                 frame < noOfFrames;
                 frame++)
            {
                for (int channel = 0;
                     channel < channels;
                     channel++)
                {
                    interleaved[outputIndex++] =
                        channelSamples[channel][frame];
                }
            }

            /*
             * Convert the float array into raw bytes.
             */
            byte[] audioData =
                new byte[
                    interleaved.Length *
                    sizeof(float)];

            Buffer.BlockCopy(
                interleaved,
                0,
                audioData,
                0,
                audioData.Length);

            /*
             * Queue it.
             *
             * The Chromium callback NEVER talks
             * directly to NAudio.
             */
            audioQueue.Enqueue(
                audioData);

            PacketCount++;

            if (PacketCount % 100 == 0)
            {
                Console.WriteLine(
                    $"FarmNet AUDIO: " +
                    $"Queued packets={PacketCount}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet AUDIO: " +
                $"Packet processing error: {ex}");
        }
    }

    protected override void OnAudioStreamStopped(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser)
    {
        AudioStreamActive =
            false;

        Console.WriteLine(
            "FarmNet AUDIO: STREAM STOPPED");

        Console.WriteLine(
            $"FarmNet AUDIO: " +
            $"Total packets={PacketCount}");
    }

    protected override void OnAudioStreamError(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        string errorMessage)
    {
        AudioStreamActive =
            false;

        Console.WriteLine(
            "FarmNet AUDIO: STREAM ERROR");

        Console.WriteLine(
            $"FarmNet AUDIO ERROR: " +
            errorMessage);
    }

    public bool TryDequeue(
        out byte[]? audioData)
    {
        return audioQueue.TryDequeue(
            out audioData);
    }

    public int QueuedPacketCount =>
        audioQueue.Count;

    private void ClearQueue()
    {
        while (
            audioQueue.TryDequeue(
                out _))
        {
        }
    }

    public void ClearAudio()
    {
        lock (audioLock)
        {
            ClearQueue();
        }
    }
}