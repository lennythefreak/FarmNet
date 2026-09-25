
using CefSharp;
using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using StardewValley;
using StardewValley.Menus;

using System;
using System.Windows.Forms;

namespace FarmNet;

public class FarmNetMenu : IClickableMenu
{
    private readonly ChromiumBrowser chromiumBrowser;
    private readonly FarmNetKeyboardSubscriber keyboardSubscriber;

    private Texture2D? browserTexture;
    private byte[]? latestFrame;

    private int browserWidth;
    private int browserHeight;

    private bool frameReady;

    private Rectangle browserArea;

    private bool chromiumLeftMouseDown;

    // =========================================================
    // TOOLBAR
    // =========================================================

    private Rectangle backButton;
    private Rectangle forwardButton;
    private Rectangle reloadButton;
    private Rectangle favoriteButton;
    private Rectangle themeButton;
    private Rectangle addressBar;
    private Rectangle closeButton;

    private bool addressBarFocused;

    private string addressText =
        "https://stardewvalleywiki.com";

    // =========================================================
    // THEME
    // =========================================================

    // false = Stardew / Parchment
    // true  = Original Dark

    private bool darkTheme = false;

    // =========================================================
    // FAVORITE
    // =========================================================

    private readonly string favoriteUrl =
        "https://stardewvalleywiki.com";

    // =========================================================
    // WINDOW
    // =========================================================

    private int windowX;
    private int windowY;
    private int windowWidth;
    private int windowHeight;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public FarmNetMenu()
        : base(
            0,
            0,
            Game1.viewport.Width,
            Game1.viewport.Height,
            true)
    {
        chromiumBrowser =
            new ChromiumBrowser();

        keyboardSubscriber =
            new FarmNetKeyboardSubscriber(
                chromiumBrowser);

        keyboardSubscriber.AddressSubmitted +=
            OnAddressSubmitted;

        Game1.keyboardDispatcher.Subscriber =
            keyboardSubscriber;

        chromiumBrowser.FrameReady +=
            OnBrowserFrame;
    }

    // =========================================================
    // BROWSER FRAME
    // =========================================================

    private void OnBrowserFrame(
        byte[] pixels,
        int width,
        int height)
    {
        latestFrame = pixels;

        browserWidth = width;
        browserHeight = height;

        frameReady = true;
    }

    private void UpdateBrowserTexture()
    {
        if (!frameReady ||
            latestFrame == null)
        {
            return;
        }

        frameReady = false;

        if (browserTexture == null ||
            browserTexture.Width != browserWidth ||
            browserTexture.Height != browserHeight)
        {
            browserTexture?.Dispose();

            browserTexture =
                new Texture2D(
                    Game1.graphics.GraphicsDevice,
                    browserWidth,
                    browserHeight,
                    false,
                    SurfaceFormat.Color);
        }

        /*
         * CefSharp gives us BGRA.
         *
         * MonoGame expects RGBA.
         */
        byte[] correctedPixels =
            new byte[latestFrame.Length];

        for (int i = 0;
             i < latestFrame.Length;
             i += 4)
        {
            correctedPixels[i] =
                latestFrame[i + 2];

            correctedPixels[i + 1] =
                latestFrame[i + 1];

            correctedPixels[i + 2] =
                latestFrame[i];

            correctedPixels[i + 3] =
                latestFrame[i + 3];
        }

        browserTexture.SetData(
            correctedPixels);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public override void update(
    GameTime time)
    {
        UpdateBrowserTexture();

        chromiumBrowser.ProcessAudio();

        SendMouseMove();

        UpdateAddressBar();

        if (addressBarFocused)
        {
            addressText =
                keyboardSubscriber.AddressText;
        }

        base.update(time);
    }

    // =========================================================
    // ADDRESS BAR
    // =========================================================

    private void UpdateAddressBar()
    {
        if (addressBarFocused)
            return;

        string currentUrl =
            chromiumBrowser.CurrentUrl;

        if (!string.IsNullOrWhiteSpace(
                currentUrl))
        {
            addressText =
                currentUrl;
        }
    }

    private void OnAddressSubmitted(
        string url)
    {
        addressText =
            url;

        addressBarFocused =
            false;

        keyboardSubscriber.EndAddressBar();

        chromiumBrowser.Navigate(
            url);
    }

    // =========================================================
    // KEYBOARD
    // =========================================================

    public override void receiveKeyPress(
    Microsoft.Xna.Framework.Input.Keys key)
    {
        if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            if (addressBarFocused)
            {
                addressBarFocused = false;

                keyboardSubscriber.EndAddressBar();

                addressText =
                    chromiumBrowser.CurrentUrl;

                return;
            }

            exitThisMenu();
            return;
        }

        /*
         * Do NOT call base.receiveKeyPress().
         * FarmNetKeyboardSubscriber handles browser keyboard input.
         */
    }

    // =========================================================
    // WINDOW SIZE
    // =========================================================

    private void CalculateWindow()
    {
        windowWidth =
            Math.Min(
                1300,
                Game1.viewport.Width - 50);

        windowHeight =
            Math.Min(
                820,
                Game1.viewport.Height - 35);

        windowX =
            (Game1.viewport.Width -
             windowWidth) / 2;

        windowY =
            (Game1.viewport.Height -
             windowHeight) / 2;
    }

    // =========================================================
    // DRAW
    // =========================================================

    public override void draw(
        SpriteBatch b)
    {
        CalculateWindow();

        // -----------------------------------------------------
        // MAIN WINDOW
        // -----------------------------------------------------

        Rectangle window =
            new Rectangle(
                windowX,
                windowY,
                windowWidth,
                windowHeight);

        b.Draw(
            Game1.staminaRect,
            window,
            darkTheme
                ? new Color(
                    25,
                    25,
                    27)
                : new Color(
                    236,
                    220,
                    184));

        // -----------------------------------------------------
        // TITLE BAR
        // -----------------------------------------------------

        Rectangle titleBar =
            new Rectangle(
                windowX,
                windowY,
                windowWidth,
                50);

        b.Draw(
            Game1.staminaRect,
            titleBar,
            darkTheme
                ? new Color(
                    18,
                    18,
                    20)
                : new Color(
                    177,
                    151,
                    111));

        b.DrawString(
            Game1.smallFont,
            "FARMNET",
            new Vector2(
                windowX + 20,
                windowY + 15),
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40));

        // -----------------------------------------------------
        // CLOSE BUTTON
        // -----------------------------------------------------

        closeButton =
            new Rectangle(
                windowX +
                windowWidth -
                45,
                windowY + 10,
                30,
                30);

        b.Draw(
            Game1.staminaRect,
            closeButton,
            darkTheme
                ? new Color(
                    105,
                    45,
                    45)
                : new Color(
                    171,
                    92,
                    78));

        DrawX(
            b,
            closeButton);

        // -----------------------------------------------------
        // ADDRESS BAR
        // -----------------------------------------------------

        int addressRowY =
            windowY + 62;

        int addressLeft =
            windowX + 20;

        int addressRight =
            closeButton.Left - 20;

        addressBar =
            new Rectangle(
                addressLeft,
                addressRowY,
                addressRight -
                addressLeft,
                44);

        b.Draw(
            Game1.staminaRect,
            addressBar,
            darkTheme
                ? addressBarFocused
                    ? new Color(
                        55,
                        55,
                        58)
                    : new Color(
                        40,
                        40,
                        43)
                : addressBarFocused
                    ? new Color(
                        255,
                        247,
                        218)
                    : new Color(
                        247,
                        235,
                        204));

        string displayAddress =
            addressBarFocused
                ? keyboardSubscriber.AddressText
                : addressText;

        if (string.IsNullOrEmpty(
                displayAddress))
        {
            displayAddress =
                "Enter a web address...";
        }

        while (
            displayAddress.Length > 0 &&
            Game1.smallFont.MeasureString(
                displayAddress).X >
            addressBar.Width - 24)
        {
            displayAddress =
                displayAddress.Substring(1);
        }

        b.DrawString(
            Game1.smallFont,
            displayAddress,
            new Vector2(
                addressBar.X + 12,
                addressBar.Y + 11),
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40));

        // -----------------------------------------------------
        // ADDRESS BAR CARET
        // -----------------------------------------------------

        if (addressBarFocused)
        {
            double milliseconds =
                Game1.currentGameTime
                    .TotalGameTime
                    .TotalMilliseconds;

            bool showCaret =
                ((int)(
                    milliseconds / 500) % 2) == 0;

            if (showCaret)
            {
                Vector2 textSize =
                    Game1.smallFont.MeasureString(
                        displayAddress);

                int caretX =
                    addressBar.X +
                    12 +
                    (int)textSize.X +
                    2;

                caretX =
                    Math.Min(
                        caretX,
                        addressBar.Right - 5);

                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(
                        caretX,
                        addressBar.Y + 8,
                        2,
                        28),
                    darkTheme
                        ? Color.White
                        : new Color(
                            74,
                            57,
                            40));
            }
        }

        // -----------------------------------------------------
        // NAVIGATION ROW
        // -----------------------------------------------------

        int navY =
            addressBar.Bottom + 8;

        int navButtonWidth =
            58;

        int navButtonHeight =
            40;

        int navGap =
            8;

        int navX =
            windowX + 20;

        backButton =
            new Rectangle(
                navX,
                navY,
                navButtonWidth,
                navButtonHeight);

        forwardButton =
            new Rectangle(
                navX +
                navButtonWidth +
                navGap,
                navY,
                navButtonWidth,
                navButtonHeight);

        reloadButton =
            new Rectangle(
                navX +
                (navButtonWidth +
                 navGap) * 2,
                navY,
                navButtonWidth,
                navButtonHeight);

        favoriteButton =
            new Rectangle(
                navX +
                (navButtonWidth +
                 navGap) * 3,
                navY,
                navButtonWidth,
                navButtonHeight);

        themeButton =
            new Rectangle(
                navX +
                (navButtonWidth +
                 navGap) * 4,
                navY,
                navButtonWidth,
                navButtonHeight);

        DrawNavigationButton(
            b,
            backButton);

        DrawNavigationButton(
            b,
            forwardButton);

        DrawNavigationButton(
            b,
            reloadButton);

        DrawNavigationButton(
            b,
            favoriteButton);

        DrawNavigationButton(
            b,
            themeButton);

        DrawBackIcon(
            b,
            backButton);

        DrawForwardIcon(
            b,
            forwardButton);

        DrawReloadIcon(
            b,
            reloadButton);

        DrawHeartIcon(
            b,
            favoriteButton);

        DrawThemeIcon(
            b,
            themeButton);

        if (chromiumBrowser.IsLoading)
        {
            b.DrawString(
                Game1.smallFont,
                "LOADING...",
                new Vector2(
                    windowX +
                    windowWidth -
                    140,
                    navY + 10),
                darkTheme
                    ? Color.White
                    : new Color(
                        74,
                        57,
                        40));
        }

        // -----------------------------------------------------
        // BROWSER
        // -----------------------------------------------------

        int browserTop =
            navY +
            navButtonHeight +
            10;

        browserArea =
            new Rectangle(
                windowX + 20,
                browserTop,
                windowWidth - 40,
                windowY +
                windowHeight -
                browserTop -
                20);

        b.Draw(
            Game1.staminaRect,
            browserArea,
            darkTheme
                ? Color.Black
                : new Color(
                    91,
                    73,
                    55));

        if (browserTexture != null)
        {
            b.Draw(
                browserTexture,
                browserArea,
                Color.White);
        }
        else
        {
            b.DrawString(
                Game1.smallFont,
                "LOADING WEB...",
                new Vector2(
                    browserArea.X + 25,
                    browserArea.Y + 30),
                darkTheme
                    ? Color.White
                    : new Color(
                        247,
                        235,
                        204));

            b.DrawString(
                Game1.smallFont,
                "Connecting to the internet...",
                new Vector2(
                    browserArea.X + 25,
                    browserArea.Y + 70),
                darkTheme
                    ? new Color(
                        200,
                        200,
                        200)
                    : new Color(
                        218,
                        199,
                        165));
        }

        drawMouse(b);
    }

    // =========================================================
    // BUTTON BACKGROUND
    // =========================================================

    private void DrawNavigationButton(
        SpriteBatch b,
        Rectangle rectangle)
    {
        b.Draw(
            Game1.staminaRect,
            rectangle,
            darkTheme
                ? new Color(
                    45,
                    45,
                    48)
                : new Color(
                    205,
                    183,
                    143));
    }

    // =========================================================
    // X ICON
    // =========================================================

    private void DrawX(
        SpriteBatch b,
        Rectangle r)
    {
        int thickness = 3;

        Color iconColor =
            darkTheme
                ? Color.White
                : new Color(
                    255,
                    235,
                    205);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                r.X + 8,
                r.Y + 8,
                thickness,
                14),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                r.X + 19,
                r.Y + 8,
                thickness,
                14),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                r.X + 11,
                r.Y + 11,
                8,
                thickness),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                r.X + 11,
                r.Y + 18,
                8,
                thickness),
            iconColor);
    }

    // =========================================================
    // BACK ICON
    // =========================================================

    private void DrawBackIcon(
        SpriteBatch b,
        Rectangle r)
    {
        int cx = r.Center.X;
        int cy = r.Center.Y;

        Color iconColor =
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 9,
                cy - 2,
                18,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 11,
                cy - 7,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 7,
                cy - 11,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 11,
                cy + 3,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 7,
                cy + 7,
                4,
                4),
            iconColor);
    }

    // =========================================================
    // FORWARD ICON
    // =========================================================

    private void DrawForwardIcon(
        SpriteBatch b,
        Rectangle r)
    {
        int cx = r.Center.X;
        int cy = r.Center.Y;

        Color iconColor =
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 9,
                cy - 2,
                18,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 7,
                cy - 7,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 3,
                cy - 11,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 7,
                cy + 3,
                4,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 3,
                cy + 7,
                4,
                4),
            iconColor);
    }

    // =========================================================
    // RELOAD ICON
    // =========================================================

    private void DrawReloadIcon(
        SpriteBatch b,
        Rectangle r)
    {
        int cx = r.Center.X;
        int cy = r.Center.Y;

        int t = 3;

        Color iconColor =
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 10,
                cy - 10,
                20,
                t),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 10,
                cy - 7,
                t,
                14),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 7,
                cy + 7,
                14,
                t),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 7,
                cy - 7,
                t,
                14),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 7,
                cy - 11,
                8,
                t),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 12,
                cy - 11,
                3,
                8),
            iconColor);
    }

    // =========================================================
    // HEART ICON
    // =========================================================

    private void DrawHeartIcon(
        SpriteBatch b,
        Rectangle r)
    {
        int cx = r.Center.X;
        int cy = r.Center.Y;

        int s = 4;

        Color iconColor =
            darkTheme
                ? Color.White
                : new Color(
                    74,
                    57,
                    40);

        // Top-left

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 8,
                cy - 8,
                s,
                s),
            iconColor);

        // Top-right

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 4,
                cy - 8,
                s,
                s),
            iconColor);

        // Left middle

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 12,
                cy - 4,
                8,
                8),
            iconColor);

        // Right middle

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 4,
                cy - 4,
                8,
                8),
            iconColor);

        // Bottom

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 8,
                cy,
                16,
                8),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 4,
                cy + 8,
                8,
                4),
            iconColor);
    }

    // =========================================================
    // THEME ICON
    // =========================================================

    private void DrawThemeIcon(
        SpriteBatch b,
        Rectangle r)
    {
        int cx = r.Center.X;
        int cy = r.Center.Y;

        Color iconColor =
            darkTheme
                ? new Color(
                    255,
                    225,
                    120)
                : new Color(
                    74,
                    57,
                    40);

        // Simple pixel sun / theme icon

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 6,
                cy - 6,
                12,
                12),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 2,
                cy - 13,
                4,
                5),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 2,
                cy + 8,
                4,
                5),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx - 13,
                cy - 2,
                5,
                4),
            iconColor);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                cx + 8,
                cy - 2,
                5,
                4),
            iconColor);
    }

    // =========================================================
    // MOUSE MOVEMENT
    // =========================================================

    private void SendMouseMove()
    {
        if (!chromiumBrowser.IsReady)
            return;

        int mouseX =
            Game1.getMouseX();

        int mouseY =
            Game1.getMouseY();

        if (!browserArea.Contains(
                mouseX,
                mouseY))
        {
            return;
        }

        GetChromiumMousePosition(
            mouseX,
            mouseY,
            out int chromiumX,
            out int chromiumY);

        var mouseEvent =
            new CefSharp.MouseEvent(
                chromiumX,
                chromiumY,
                GetMouseEventFlags());

        chromiumBrowser.Browser?
            .GetBrowserHost()?
            .SendMouseMoveEvent(
                mouseEvent,
                false);
    }

    private CefSharp.CefEventFlags GetMouseEventFlags()
    {
        if (chromiumLeftMouseDown)
        {
            return CefSharp.CefEventFlags.LeftMouseButton;
        }

        return CefSharp.CefEventFlags.None;
    }

    // =========================================================
    // CONVERT MOUSE COORDINATES
    // =========================================================

    private void GetChromiumMousePosition(
        int screenX,
        int screenY,
        out int chromiumX,
        out int chromiumY)
    {
        int localX =
            screenX -
            browserArea.X;

        int localY =
            screenY -
            browserArea.Y;

        if (browserArea.Width <= 0 ||
            browserArea.Height <= 0 ||
            browserWidth <= 0 ||
            browserHeight <= 0)
        {
            chromiumX = 0;
            chromiumY = 0;
            return;
        }

        chromiumX =
            (int)(
                (float)localX /
                browserArea.Width *
                browserWidth);

        chromiumY =
            (int)(
                (float)localY /
                browserArea.Height *
                browserHeight);

        chromiumX =
            Math.Clamp(
                chromiumX,
                0,
                Math.Max(
                    0,
                    browserWidth - 1));

        chromiumY =
            Math.Clamp(
                chromiumY,
                0,
                Math.Max(
                    0,
                    browserHeight - 1));
    }

    // =========================================================
    // LEFT CLICK
    // =========================================================

    public override void receiveLeftClick(
        int x,
        int y,
        bool playSound = true)
    {
        // CLOSE

        if (closeButton.Contains(
                x,
                y))
        {
            exitThisMenu();
            return;
        }

        // BACK

        if (backButton.Contains(
                x,
                y))
        {
            addressBarFocused =
                false;

            keyboardSubscriber.EndAddressBar();

            chromiumBrowser.GoBack();

            return;
        }

        // FORWARD

        if (forwardButton.Contains(
                x,
                y))
        {
            addressBarFocused =
                false;

            keyboardSubscriber.EndAddressBar();

            chromiumBrowser.GoForward();

            return;
        }

        // RELOAD

        if (reloadButton.Contains(
                x,
                y))
        {
            addressBarFocused =
                false;

            keyboardSubscriber.EndAddressBar();

            chromiumBrowser.Reload();

            return;
        }

        // FAVORITE / HOME

        if (favoriteButton.Contains(
                x,
                y))
        {
            addressBarFocused =
                false;

            keyboardSubscriber.EndAddressBar();

            addressText =
                favoriteUrl;

            chromiumBrowser.Navigate(
                favoriteUrl);

            return;
        }

        // THEME

        if (themeButton.Contains(
                x,
                y))
        {
            darkTheme =
                !darkTheme;

            return;
        }

        // ADDRESS BAR

        if (addressBar.Contains(
                x,
                y))
        {
            addressBarFocused =
                true;

            keyboardSubscriber.BeginAddressBar(
                addressText);

            return;
        }

        // WEB PAGE

        if (browserArea.Contains(
                x,
                y))
        {
            addressBarFocused =
                false;

            keyboardSubscriber.EndAddressBar();

            SendMouseDown(
                x,
                y);

            return;
        }

        base.receiveLeftClick(
            x,
            y,
            playSound);
    }

    // =========================================================
    // MOUSE DOWN
    // =========================================================

    private void SendMouseDown(
        int screenX,
        int screenY)
    {
        if (!chromiumBrowser.IsReady)
            return;

        GetChromiumMousePosition(
            screenX,
            screenY,
            out int chromiumX,
            out int chromiumY);

        var mouseEvent =
            new CefSharp.MouseEvent(
                chromiumX,
                chromiumY,
                CefSharp.CefEventFlags.LeftMouseButton);

        var host =
            chromiumBrowser.Browser?
                .GetBrowserHost();

        if (host == null)
            return;

        host.SendMouseClickEvent(
            mouseEvent,
            CefSharp.MouseButtonType.Left,
            false,
            1);

        chromiumLeftMouseDown =
            true;
    }

    // =========================================================
    // MOUSE UP
    // =========================================================

    public override void releaseLeftClick(
        int x,
        int y)
    {
        if (chromiumLeftMouseDown)
        {
            SendMouseUp(
                x,
                y);
        }

        base.releaseLeftClick(
            x,
            y);
    }

    private void SendMouseUp(
        int screenX,
        int screenY)
    {
        if (!chromiumBrowser.IsReady)
        {
            chromiumLeftMouseDown =
                false;

            return;
        }

        GetChromiumMousePosition(
            screenX,
            screenY,
            out int chromiumX,
            out int chromiumY);

        var mouseEvent =
            new CefSharp.MouseEvent(
                chromiumX,
                chromiumY,
                CefSharp.CefEventFlags.None);

        var host =
            chromiumBrowser.Browser?
                .GetBrowserHost();

        if (host == null)
        {
            chromiumLeftMouseDown =
                false;

            return;
        }

        host.SendMouseClickEvent(
            mouseEvent,
            CefSharp.MouseButtonType.Left,
            true,
            1);

        chromiumLeftMouseDown =
            false;
    }

    // =========================================================
    // MOUSE WHEEL
    // =========================================================

    public void HandleMouseWheel(
        int delta)
    {
        if (!chromiumBrowser.IsReady)
            return;

        int mouseX =
            Game1.getMouseX();

        int mouseY =
            Game1.getMouseY();

        if (!browserArea.Contains(
                mouseX,
                mouseY))
        {
            return;
        }

        GetChromiumMousePosition(
            mouseX,
            mouseY,
            out int chromiumX,
            out int chromiumY);

        var mouseEvent =
            new CefSharp.MouseEvent(
                chromiumX,
                chromiumY,
                GetMouseEventFlags());

        chromiumBrowser.Browser?
            .GetBrowserHost()?
            .SendMouseWheelEvent(
                mouseEvent,
                0,
                delta);
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    protected override void cleanupBeforeExit()
    {
        /*
         * Remove keyboard subscriber.
         */

        if (Game1.keyboardDispatcher.Subscriber ==
            keyboardSubscriber)
        {
            Game1.keyboardDispatcher.Subscriber =
                null;
        }

        /*
         * Remove FarmNet events.
         */

        keyboardSubscriber.AddressSubmitted -=
            OnAddressSubmitted;

        chromiumBrowser.FrameReady -=
            OnBrowserFrame;

        /*
         * Dispose Chromium.
         */

        chromiumBrowser.Dispose();

        /*
         * Dispose browser texture.
         */

        browserTexture?.Dispose();

        browserTexture =
            null;

        chromiumLeftMouseDown =
            false;

        base.cleanupBeforeExit();
    }
}

