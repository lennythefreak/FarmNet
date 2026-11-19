
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
    private readonly Action<string>? popupRequested;
    private FarmNetPopupHandler? popupHandler;

    private FarmNetBrowserAudioManager? audioManager;

    private bool ownsAudioManager;

    private readonly object navigationLock = new();
    private string? pendingNavigationUrl;

    private string sessionFilePath = string.Empty;

    private readonly string sessionFileName;

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
        sessionFileName =
            "browser-session.txt";

        audioManager =
            new FarmNetBrowserAudioManager();

        ownsAudioManager =
            true;

        Initialize();
    }

    public ChromiumBrowser(
        FarmNetBrowserAudioManager sharedAudioManager,
        string sessionFileName = "browser-session.txt",
        Action<string>? popupRequested = null)
    {
        this.sessionFileName =
            string.IsNullOrWhiteSpace(sessionFileName)
                ? "browser-session.txt"
                : Path.GetFileName(sessionFileName);

        audioManager =
            sharedAudioManager ??
            throw new ArgumentNullException(
                nameof(sharedAudioManager));

        ownsAudioManager =
            false;

        this.popupRequested =
            popupRequested;

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
                    sessionFileName);

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

            /*
             * ----------------------------------------
             * BROWSER
             * ----------------------------------------
             */

            string startingUrl =
                LoadSavedUrl();

            browser =
                new ChromiumWebBrowser(startingUrl);

            if (popupRequested != null)
            {
                popupHandler =
                    new FarmNetPopupHandler(popupRequested);

                browser.LifeSpanHandler =
                    popupHandler;
            }

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

        string? pendingUrl;

        ChromiumWebBrowser? initializedBrowser;

        lock (navigationLock)
        {
            initializedBrowser = browser;

            pendingUrl =
                pendingNavigationUrl;

            pendingNavigationUrl = null;
        }

        initializedBrowser?
            .GetBrowserHost()?
            .WasResized();

        if (!string.IsNullOrWhiteSpace(pendingUrl))
        {
            Console.WriteLine(
                $"FarmNet: Loading queued navigation: {pendingUrl}");

            initializedBrowser?.Load(pendingUrl);
        }
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

    public void Resize(
        int width,
        int height)
    {
        if (browser == null ||
            width <= 0 ||
            height <= 0)
        {
            return;
        }

        var newSize =
            new System.Drawing.Size(width, height);

        if (browser.Size == newSize)
            return;

        browser.Size = newSize;

        if (browser.IsBrowserInitialized)
        {
            browser.GetBrowserHost()?.WasResized();
        }
    }

    public void ProcessAudio()
    {
        if (audioHandler == null ||
            audioManager == null)
        {
            return;
        }

        audioManager.ProcessAudio(audioHandler);
    }

    public void DeactivateAudio()
    {
        if (audioManager == null)
            return;

        audioManager.Deactivate(audioHandler);
    }

    /*
     * ----------------------------------------
     * NAVIGATION
     * ----------------------------------------
     */

    public void Navigate(
        string url)
    {
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

        ChromiumWebBrowser? browserToLoad;

        lock (navigationLock)
        {
            if (browser == null)
                return;

            if (!browser.IsBrowserInitialized)
            {
                pendingNavigationUrl = url;
                return;
            }

            browserToLoad = browser;
        }

        browserToLoad.Load(url);
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

            browser.LifeSpanHandler = null;
            popupHandler = null;

            browser.Dispose();

            browser =
                null;
        }

        audioHandler?.ClearAudio();

        if (ownsAudioManager)
        {
            audioManager?.Dispose();
        }

        audioManager =
            null;

        audioHandler =
            null;

        FrameReady =
            null;
    }
}






