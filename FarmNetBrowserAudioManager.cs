using System;

namespace FarmNet;

public class FarmNetBrowserAudioManager : IDisposable
{
    private readonly FarmNetAudioPlayer audioPlayer = new();
    private readonly FarmNetMusicDucker musicDucker = new();

    private bool disposed;

    public FarmNetBrowserAudioManager()
    {
        musicDucker.Initialize();

        Console.WriteLine(
            "FarmNet AUDIO: Shared audio manager initialized.");
    }

    public void ProcessAudio(FarmNetAudioHandler? audioHandler)
    {
        if (disposed)
            return;

        bool browserAudioActive =
            audioHandler?.AudioStreamActive == true;

        musicDucker.SetBrowserAudioActive(
            browserAudioActive);

        musicDucker.Update(1f / 60f);

        if (audioHandler == null)
            return;

        if (!browserAudioActive &&
            audioHandler.QueuedPacketCount == 0)
        {
            return;
        }

        if (!audioPlayer.IsPlaying)
        {
            audioPlayer.Start(
                audioHandler.SampleRate,
                audioHandler.Channels);
        }

        int packetsProcessed = 0;

        while (
            audioPlayer.BufferedSeconds < 0.5 &&
            audioHandler.TryDequeue(out byte[]? audioData))
        {
            if (audioData == null)
                continue;

            audioPlayer.AddAudio(audioData);

            packetsProcessed++;

            if (packetsProcessed >= 20)
                break;
        }
    }

    public void Deactivate(FarmNetAudioHandler? audioHandler)
    {
        if (disposed)
            return;

        // Discard audio belonging to the tab we're leaving.
        audioHandler?.ClearAudio();

        // Do not let old audio play after switching tabs.
        audioPlayer.ClearBuffer();

        musicDucker.SetBrowserAudioActive(false);
        musicDucker.Update(1f / 60f);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        musicDucker.Restore();
        audioPlayer.Dispose();

        Console.WriteLine(
            "FarmNet AUDIO: Shared audio manager disposed.");
    }
}
