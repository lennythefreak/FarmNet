using Microsoft.Xna.Framework.Graphics;
using System;

namespace FarmNet;

public class FarmNetBrowserTab : IDisposable
{
    private readonly object frameLock = new();

    private readonly Action<string> addressSubmittedHandler;

    private byte[]? latestFrame;

    private int browserWidth;
    private int browserHeight;

    private bool frameReady;
    private bool disposed;

    public ChromiumBrowser Browser { get; }

    public FarmNetKeyboardSubscriber KeyboardSubscriber { get; }

    public Texture2D? Texture { get; private set; }

    public int FrameWidth
    {
        get
        {
            lock (frameLock)
                return browserWidth;
        }
    }

    public int FrameHeight
    {
        get
        {
            lock (frameLock)
                return browserHeight;
        }
    }

    public string CurrentUrl =>
        Browser.CurrentUrl;

    public bool IsLoading =>
        Browser.IsLoading;

    public FarmNetBrowserTab(
        FarmNetBrowserAudioManager audioManager,
        Action<string> addressSubmitted,
        string sessionFileName = "browser-session.txt")
    {
        addressSubmittedHandler =
            addressSubmitted ??
            throw new ArgumentNullException(
                nameof(addressSubmitted));

        Browser =
            new ChromiumBrowser(
                audioManager,
                sessionFileName);

        KeyboardSubscriber =
            new FarmNetKeyboardSubscriber(
                Browser);

        KeyboardSubscriber.AddressSubmitted +=
            addressSubmittedHandler;

        Browser.FrameReady +=
            OnBrowserFrame;
    }

    private void OnBrowserFrame(
        byte[] pixels,
        int width,
        int height)
    {
        if (disposed ||
            pixels == null ||
            width <= 0 ||
            height <= 0)
        {
            return;
        }

        lock (frameLock)
        {
            latestFrame =
                pixels.ToArray();

            browserWidth =
                width;

            browserHeight =
                height;

            frameReady =
                true;
        }
    }

    public void UpdateTexture(
        GraphicsDevice graphicsDevice)
    {
        byte[]? pixels;
        int width;
        int height;

        lock (frameLock)
        {
            if (!frameReady ||
                latestFrame == null)
            {
                return;
            }

            pixels =
                latestFrame;

            width =
                browserWidth;

            height =
                browserHeight;

            frameReady =
                false;
        }

        if (width <= 0 ||
            height <= 0 ||
            pixels.Length != width * height * 4)
        {
            return;
        }

        if (Texture == null ||
            Texture.Width != width ||
            Texture.Height != height)
        {
            Texture?.Dispose();

            Texture =
                new Texture2D(
                    graphicsDevice,
                    width,
                    height,
                    false,
                    SurfaceFormat.Color);
        }

        byte[] correctedPixels =
            new byte[pixels.Length];

        for (int i = 0;
             i < pixels.Length;
             i += 4)
        {
            // CefSharp provides BGRA.
            // MonoGame expects RGBA.

            correctedPixels[i] =
                pixels[i + 2];

            correctedPixels[i + 1] =
                pixels[i + 1];

            correctedPixels[i + 2] =
                pixels[i];

            correctedPixels[i + 3] =
                pixels[i + 3];
        }

        Texture.SetData(
            correctedPixels);
    }

    public void ProcessAudio()
    {
        if (!disposed)
            Browser.ProcessAudio();
    }

    public void DeactivateAudio()
    {
        if (!disposed)
            Browser.DeactivateAudio();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        Browser.FrameReady -=
            OnBrowserFrame;

        KeyboardSubscriber.AddressSubmitted -=
            addressSubmittedHandler;

        Texture?.Dispose();

        Texture =
            null;

        Browser.Dispose();

        lock (frameLock)
        {
            latestFrame = null;
            frameReady = false;
        }
    }
}



