using System;
using System.IO;
using System.Runtime.InteropServices;
using CefSharp;
using CefSharp.OffScreen;
using StardewValley;

namespace FarmNet;

public class ChromiumBrowser : IDisposable
{
    private ChromiumWebBrowser? browser;

    private FarmNetAudioHandler? audioHandler;

    private FarmNetAudioPlayer? audioPlayer;

    private FarmNetMusicDucker? musicDucker;

    public event Action<byte[], int, int>? FrameReady;

    public bool IsReady =>
        browser != null &&
        browser.IsBrowserInitialized;

    public bool IsLoading =>
        browser != null &&
        browser.IsLoading;

    public string CurrentUrl =>
        browser?.Address ?? string.Empty;

    public ChromiumWebBrowser? Browser =>
        browser;

    public ChromiumBrowser()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            if (!Cef.IsInitialized.GetValueOrDefault())
            {
                string cachePath =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        "FarmNet",
                        "CefCache");

                Directory.CreateDirectory(
                    cachePath);

                CefSettings settings =
                    new CefSettings
                    {
                        RootCachePath =
                            cachePath,

                        CachePath =
                            Path.Combine(
                                cachePath,
                                "BrowserCache"),

                        WindowlessRenderingEnabled =
                            true
                    };

                /*
                 * Keep Chromium audio enabled.
                 *
                 * This is what allows YouTube audio
                 * to reach FarmNetAudioHandler.
                 */
                settings.EnableAudio();

                settings.SetOffScreenRenderingBestPerformanceArgs();

                bool initialized =
                    Cef.Initialize(
                        settings,
                        performDependencyCheck: true,
                        browserProcessHandler: null);

                if (!initialized)
                {
                    throw new Exception(
                        "FarmNet: Cef.Initialize() failed.");
                }
            }

            /*
             * ----------------------------------------
             * AUDIO
             * ----------------------------------------
             */

            audioHandler =
                new FarmNetAudioHandler();

            audioPlayer =
                new FarmNetAudioPlayer();

            /*
             * Stardew music ducking.
             */
            musicDucker =
                new FarmNetMusicDucker();

            /*
             * Store the player's current Stardew
             * music volume as our normal volume.
             */
            musicDucker.Initialize();

            /*
             * ----------------------------------------
             * BROWSER
             * ----------------------------------------
             */

            browser =
                new ChromiumWebBrowser(
                    "https://stardewvalleywiki.com");

            browser.AudioHandler =
                audioHandler;

            browser.BrowserInitialized +=
                OnBrowserInitialized;

            browser.LoadError +=
                OnLoadError;

            browser.LoadingStateChanged +=
                OnLoadingStateChanged;

            browser.Paint +=
                OnPaint;

            browser.AddressChanged +=
                OnAddressChanged;

            browser.Size =
                new System.Drawing.Size(
                    1000,
                    600);

            Console.WriteLine(
                "FarmNet: Chromium browser created.");

            Console.WriteLine(
                "FarmNet AUDIO: " +
                "diagnostic audio handler attached.");

            Console.WriteLine(
                "FarmNet AUDIO: " +
                "NAudio output prepared.");

            Console.WriteLine(
                "FarmNet AUDIO: " +
                "Stardew music ducking enabled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet Chromium initialization error: {ex}");
        }
    }

    private void OnBrowserInitialized(
        object? sender,
        EventArgs e)
    {
        Console.WriteLine(
            "FarmNet: Chromium browser initialized.");

        browser?
            .GetBrowserHost()?
            .WasResized();
    }

    private void OnPaint(
        object? sender,
        OnPaintEventArgs e)
    {
        if (e.IsPopup)
            return;

        if (e.Width <= 0 ||
            e.Height <= 0)
        {
            return;
        }

        if (e.BufferHandle ==
            IntPtr.Zero)
        {
            return;
        }

        try
        {
            int byteCount =
                e.Width *
                e.Height *
                4;

            byte[] pixels =
                new byte[byteCount];

            Marshal.Copy(
                e.BufferHandle,
                pixels,
                0,
                byteCount);

            FrameReady?.Invoke(
                pixels,
                e.Width,
                e.Height);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet Chromium paint error: {ex}");
        }
    }

    private void OnLoadingStateChanged(
        object? sender,
        LoadingStateChangedEventArgs e)
    {
        if (!e.IsLoading)
        {
            Console.WriteLine(
                "FarmNet: page finished loading.");
        }
    }

    private void OnAddressChanged(
        object? sender,
        AddressChangedEventArgs e)
    {
        Console.WriteLine(
            $"FarmNet URL changed: {e.Address}");
    }

    private void OnLoadError(
        object? sender,
        LoadErrorEventArgs e)
    {
        Console.WriteLine(
            $"FarmNet: Chromium load error: " +
            $"{e.ErrorCode} - {e.ErrorText}");
    }

    /*
     * ----------------------------------------
     * AUDIO PROCESSING
     * ----------------------------------------
     *
     * This runs from Stardew's normal update
     * loop.
     *
     * Chromium's audio callback NEVER talks
     * directly to NAudio.
     */
    public void ProcessAudio()
    {
        if (audioHandler == null ||
            audioPlayer == null ||
            musicDucker == null)
        {
            return;
        }

        /*
         * ----------------------------------------
         * MUSIC DUCKING
         * ----------------------------------------
         *
         * Active browser audio:
         *
         * Stardew music → 15%
         *
         * No browser audio:
         *
         * Stardew music → normal volume
         */
        bool browserAudioActive =
            audioHandler.AudioStreamActive;

        musicDucker.SetBrowserAudioActive(
            browserAudioActive);

        /*
         * Stardew normally updates around 60 FPS,
         * so this gives us a smooth fade.
         */
        musicDucker.Update(
            1f / 60f);

        /*
         * Nothing left to send to NAudio.
         */
        if (!audioHandler.AudioStreamActive &&
            audioHandler.QueuedPacketCount == 0)
        {
            return;
        }

        /*
         * Start the Windows audio output when
         * Chromium starts sending audio.
         */
        if (!audioPlayer.IsPlaying)
        {
            audioPlayer.Start(
                audioHandler.SampleRate,
                audioHandler.Channels);
        }

        /*
         * Move a limited number of packets from
         * the Chromium queue into NAudio each
         * Stardew frame.
         */
        int packetsProcessed =
            0;

        while (
            audioHandler.TryDequeue(
                out byte[]? audioData))
        {
            if (audioData == null)
                continue;

            audioPlayer.AddAudio(
                audioData);

            packetsProcessed++;

            /*
             * Prevent an enormous backlog from
             * consuming one Stardew frame.
             */
            if (packetsProcessed >= 20)
                break;
        }
    }

    /*
     * ----------------------------------------
     * NAVIGATION
     * ----------------------------------------
     */

    public void Navigate(
        string url)
    {
        if (!IsReady ||
            browser == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        url =
            url.Trim();

        if (!url.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            url =
                "https://" + url;
        }

        browser.Load(url);
    }

    public void GoBack()
    {
        if (!IsReady ||
            browser == null)
        {
            return;
        }

        if (browser.CanGoBack)
        {
            browser.Back();
        }
    }

    public void GoForward()
    {
        if (!IsReady ||
            browser == null)
        {
            return;
        }

        if (browser.CanGoForward)
        {
            browser.Forward();
        }
    }

    public void Reload()
    {
        if (!IsReady ||
            browser == null)
        {
            return;
        }

        browser.Reload();
    }

    /*
     * ----------------------------------------
     * CLEANUP
     * ----------------------------------------
     */

    public void Dispose()
    {
        /*
         * Immediately restore Stardew's music
         * before destroying the browser.
         */
        musicDucker?.Restore();

        if (browser != null)
        {
            browser.BrowserInitialized -=
                OnBrowserInitialized;

            browser.LoadError -=
                OnLoadError;

            browser.LoadingStateChanged -=
                OnLoadingStateChanged;

            browser.Paint -=
                OnPaint;

            browser.AddressChanged -=
                OnAddressChanged;

            browser.AudioHandler =
                null;

            browser.Dispose();

            browser =
                null;
        }

        audioHandler?.ClearAudio();

        audioPlayer?.Dispose();

        audioPlayer =
            null;

        audioHandler =
            null;

        musicDucker =
            null;

        FrameReady =
            null;
    }
}