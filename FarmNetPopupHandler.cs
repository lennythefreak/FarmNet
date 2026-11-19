using System;
using CefSharp;
using CefSharp.Handler;

namespace FarmNet;

public sealed class FarmNetPopupHandler : LifeSpanHandler
{
    private readonly Action<string> popupRequested;

    public FarmNetPopupHandler(Action<string> popupRequested)
    {
        this.popupRequested =
            popupRequested ??
            throw new ArgumentNullException(nameof(popupRequested));
    }

    protected override bool OnBeforePopup(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        string targetUrl,
        string targetFrameName,
        WindowOpenDisposition targetDisposition,
        bool userGesture,
        IPopupFeatures popupFeatures,
        IWindowInfo windowInfo,
        IBrowserSettings browserSettings,
        ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        newBrowser = null!;

        if (!string.IsNullOrWhiteSpace(targetUrl))
        {
            popupRequested(targetUrl);
        }

        return true;
    }
}

