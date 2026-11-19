
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

    private string sessionFilePath = string.Empty;

    private const string DefaultUrl =
        "https://stardewvalleywiki.com";

    private const string ScrollPersistenceScript = @"
(function () {
    try {
        const key = 'farmnet-scroll:' + window.location.href;

        function saveScroll() {
            try {
                localStorage.setItem(key, JSON.stringify({
                    x: window.scrollX,
                    y: window.scrollY
                }));
            } catch (e) {}
        }

        const saved = localStorage.getItem(key);

        if (saved) {
            const position = JSON.parse(saved);

            window.requestAnimationFrame(function () {
                window.scrollTo(
                    position.x || 0,
                    position.y || 0
                );
            });
        }

        let saveTimer;

        window.addEventListener('scroll', function () {
            clearTimeout(saveTimer);

            saveTimer = setTimeout(saveScroll, 150);
        }, { passive: true });

        window.addEventListener('beforeunload', saveScroll);

    } catch (e) {}
})();
";

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
            string farmNetFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "FarmNet");

            Directory.CreateDirectory(farmNetFolder);

            sessionFilePath =
                Path.Combine(
                    farmNetFolder,
                    "browser-session.txt");

            if (!Cef.IsInitialized.GetValueOrDefault())
            {
                string cachePath =
                    Path.Combine(
                        farmNetFolder,
                        "CefCache");

                Directory.CreateDirectory(cachePath);

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

            musicDucker =
                new FarmNetMusicDucker();

            musicDucker.Initialize();

            /*
             * ----------------------------------------
             * BROWSER
             * ----------------------------------------
             */

            string startingUrl =
                LoadSavedUrl();

            browser =
                new ChromiumWebBrowser(startingUrl);

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
                $"FarmNet: Restoring session URL: {startingUrl}");

            Console.WriteLine(
                "FarmNet AUDIO: diagnostic audio handler attached.");

            Console.WriteLine(
                "FarmNet AUDIO: NAudio output prepared.");

            Console.WriteLine(
                "FarmNet AUDIO: Stardew music ducking enabled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet Chromium initialization error: {ex}");
        }
    }

    /*
     * ----------------------------------------
     * SESSION STORAGE
     * ----------------------------------------
     */

    private string LoadSavedUrl()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionFilePath) ||
                !File.Exists(sessionFilePath))
            {
                return DefaultUrl;
            }

            string savedUrl =
                File.ReadAllText(sessionFilePath).Trim();

            if (Uri.TryCreate(
                    savedUrl,
                    UriKind.Absolute,
                    out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                return savedUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet: Could not load browser session: {ex.Message}");
        }

        return DefaultUrl;
    }

    private void SaveCurrentUrl()
    {
        try
        {
            string url =
                CurrentUrl;

            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sessionFilePath))
                return;

            File.WriteAllText(
                sessionFilePath,
                url);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet: Could not save browser session: {ex.Message}");
        }
    }

    /*
     * ----------------------------------------
     * BROWSER EVENTS
     * ----------------------------------------
     */

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

        if (e.BufferHandle == IntPtr.Zero)
            return;

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

            /*
             * Restore the saved scroll position
             * and begin saving future scroll changes.
             */
            try
            {
                browser?.ExecuteScriptAsync(
                    ScrollPersistenceScript);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"FarmNet: Scroll restoration error: {ex.Message}");
            }
        }
    }

    private void OnAddressChanged(
        object? sender,
        AddressChangedEventArgs e)
    {
        Console.WriteLine(
            $"FarmNet URL changed: {e.Address}");

        /*
         * Keep the most recently visited page saved.
         */
        try
        {
            if (Uri.TryCreate(
                    e.Address,
                    UriKind.Absolute,
                    out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                if (!string.IsNullOrWhiteSpace(sessionFilePath))
                {
                    File.WriteAllText(
                        sessionFilePath,
                        e.Address);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"FarmNet: Could not save URL: {ex.Message}");
        }
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
     */

    public void ProcessAudio()
    {
        if (audioHandler == null ||
            audioPlayer == null ||
            musicDucker == null)
        {
            return;
        }

        bool browserAudioActive =
            audioHandler.AudioStreamActive;

        musicDucker.SetBrowserAudioActive(
            browserAudioActive);

        musicDucker.Update(
            1f / 60f);

        if (!audioHandler.AudioStreamActive &&
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

        int packetsProcessed =
            0;

        while (
            audioPlayer.BufferedSeconds < 0.5 &&
            audioHandler.TryDequeue(
                out byte[]? audioData))
        {
            if (audioData == null)
                continue;

            audioPlayer.AddAudio(
                audioData);

            packetsProcessed++;

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
            return;

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
            browser.Back();
    }

    public void GoForward()
    {
        if (!IsReady ||
            browser == null)
        {
            return;
        }

        if (browser.CanGoForward)
            browser.Forward();
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
         * Save the last visited URL before
         * destroying the browser.
         *
         * Scroll position is saved by Chromium's
         * JavaScript scroll listener.
         */
        SaveCurrentUrl();

        /*
         * Restore Stardew's music before
         * destroying the browser.
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

