
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
    private Rectangle saveBookmarkButton;
    private Rectangle listBookmarksButton;
    private Rectangle addressBar;

    private readonly FarmNetBookmarks bookmarks;

    private bool bookmarkPanelOpen;
    private int bookmarkScrollOffset;

    private Rectangle bookmarkPanelBounds;
    private Rectangle bookmarkCloseButton;
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
        bookmarks =
            new FarmNetBookmarks();

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

        int saveBookmarkWidth = 140;
        int listBookmarksWidth = 170;
        int bookmarkButtonGap = 8;

        listBookmarksButton =
            new Rectangle(
                windowX + windowWidth - 20 - listBookmarksWidth,
                navY,
                listBookmarksWidth,
                navButtonHeight);

        saveBookmarkButton =
            new Rectangle(
                listBookmarksButton.X - bookmarkButtonGap - saveBookmarkWidth,
                navY,
                saveBookmarkWidth,
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

        DrawNavigationButton(
            b,
            saveBookmarkButton);

        DrawNavigationButton(
            b,
            listBookmarksButton);

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

        string saveLabel = "SAVE PAGE";
        Vector2 saveLabelSize =
            Game1.smallFont.MeasureString(saveLabel);

        b.DrawString(
            Game1.smallFont,
            saveLabel,
            new Vector2(
                saveBookmarkButton.X +
                (saveBookmarkButton.Width - saveLabelSize.X) / 2,
                saveBookmarkButton.Y +
                (saveBookmarkButton.Height - saveLabelSize.Y) / 2),
            darkTheme ? Color.White : new Color(74, 57, 40));

        string listLabel = "BOOKMARKS";
        Vector2 listLabelSize =
            Game1.smallFont.MeasureString(listLabel);

        b.DrawString(
            Game1.smallFont,
            listLabel,
            new Vector2(
                listBookmarksButton.X +
                (listBookmarksButton.Width - listLabelSize.X) / 2,
                listBookmarksButton.Y +
                (listBookmarksButton.Height - listLabelSize.Y) / 2),
            darkTheme ? Color.White : new Color(74, 57, 40));


        // -----------------------------------------------------
        // BROWSER
        // -----------------------------------------------------

        int browserTop =
            navY +
            navButtonHeight +
            10;

        int statusBarHeight = 32;

        Rectangle statusBar =
            new Rectangle(
                windowX + 20,
                windowY + windowHeight - 48,
                windowWidth - 40,
                statusBarHeight);

        browserArea =
            new Rectangle(
                windowX + 20,
                browserTop,
                windowWidth - 40,
                statusBar.Y - browserTop - 8);

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

        // -----------------------------------------------------
        // STATUS BAR
        // -----------------------------------------------------

        Color statusBackground =
            darkTheme
                ? new Color(32, 32, 35)
                : new Color(218, 202, 172);

        Color statusText =
            darkTheme
                ? new Color(220, 220, 220)
                : new Color(74, 57, 40);

        b.Draw(
            Game1.staminaRect,
            statusBar,
            statusBackground);

        b.Draw(
            Game1.staminaRect,
            new Rectangle(
                statusBar.X,
                statusBar.Y,
                statusBar.Width,
                2),
            darkTheme
                ? new Color(65, 65, 70)
                : new Color(190, 170, 137));

        string clockText =
            DateTime.Now.ToString("dddd, MMMM d yyyy  h:mm:ss tt");

        Vector2 clockSize =
            Game1.smallFont.MeasureString(clockText);

        b.DrawString(
            Game1.smallFont,
            clockText,
            new Vector2(
                statusBar.X + 12,
                statusBar.Y +
                (statusBar.Height - clockSize.Y) / 2),
            statusText);

        string statusTextValue =
            chromiumBrowser.IsLoading
                ? "LOADING..."
                : "READY";

        Vector2 statusTextSize =
            Game1.smallFont.MeasureString(statusTextValue);

        b.DrawString(
            Game1.smallFont,
            statusTextValue,
            new Vector2(
                statusBar.Right - statusTextSize.X - 12,
                statusBar.Y +
                (statusBar.Height - statusTextSize.Y) / 2),
            statusText);

        if (bookmarkPanelOpen)
        {
            DrawBookmarkPanel(b);
        }

        drawMouse(b);
    }

    // =========================================================
    // BOOKMARK PANEL
    // =========================================================

    private void DrawBookmarkPanel(SpriteBatch b)
    {
        int panelWidth = Math.Min(560, browserArea.Width - 40);
        int panelHeight = Math.Min(460, browserArea.Height - 40);

        panelWidth = Math.Max(260, panelWidth);
        panelHeight = Math.Max(180, panelHeight);

        bookmarkPanelBounds = new Rectangle(
            browserArea.X + (browserArea.Width - panelWidth) / 2,
            browserArea.Y + (browserArea.Height - panelHeight) / 2,
            panelWidth,
            panelHeight);

        b.Draw(
            Game1.staminaRect,
            bookmarkPanelBounds,
            darkTheme
                ? new Color(30, 30, 33)
                : new Color(238, 222, 190));

        b.DrawString(
            Game1.smallFont,
            "FARMNET BOOKMARKS",
            new Vector2(
                bookmarkPanelBounds.X + 16,
                bookmarkPanelBounds.Y + 14),
            darkTheme ? Color.White : new Color(74, 57, 40));

        bookmarkCloseButton = new Rectangle(
            bookmarkPanelBounds.Right - 36,
            bookmarkPanelBounds.Y + 8,
            28,
            28);

        b.Draw(
            Game1.staminaRect,
            bookmarkCloseButton,
            darkTheme
                ? new Color(75, 45, 45)
                : new Color(190, 130, 105));

        b.DrawString(
            Game1.smallFont,
            "X",
            new Vector2(
                bookmarkCloseButton.X + 9,
                bookmarkCloseButton.Y + 5),
            Color.White);

        int rowHeight = 42;
        int firstRowY = bookmarkPanelBounds.Y + 52;
        int visibleRows = Math.Max(
            0,
            (bookmarkPanelBounds.Height - 64) / rowHeight);

        if (bookmarks.All.Count == 0)
        {
            Color messageColor =
                darkTheme ? Color.White : new Color(74, 57, 40);

            b.DrawString(
                Game1.smallFont,
                "No bookmarks yet.",
                new Vector2(
                    bookmarkPanelBounds.X + 16,
                    firstRowY + 8),
                messageColor);

            b.DrawString(
                Game1.smallFont,
                "Press SAVE PAGE on a",
                new Vector2(
                    bookmarkPanelBounds.X + 16,
                    firstRowY + 30),
                messageColor);

            b.DrawString(
                Game1.smallFont,
                "page to keep it.",
                new Vector2(
                    bookmarkPanelBounds.X + 16,
                    firstRowY + 52),
                messageColor);

            return;
        }

        for (int i = 0; i < visibleRows; i++)
        {
            int index = bookmarkScrollOffset + i;

            if (index >= bookmarks.All.Count)
                break;

            string url = bookmarks.All[index];

            Rectangle row = new Rectangle(
                bookmarkPanelBounds.X + 12,
                firstRowY + i * rowHeight,
                bookmarkPanelBounds.Width - 24,
                rowHeight - 4);

            b.Draw(
                Game1.staminaRect,
                row,
                darkTheme
                    ? new Color(48, 48, 52)
                    : new Color(220, 203, 169));

            Rectangle removeButton = new Rectangle(
                row.Right - 38,
                row.Y + 4,
                32,
                row.Height - 8);

            b.Draw(
                Game1.staminaRect,
                removeButton,
                darkTheme
                    ? new Color(85, 45, 45)
                    : new Color(180, 115, 95));

            b.DrawString(
                Game1.smallFont,
                "X",
                new Vector2(
                    removeButton.X + 10,
                    removeButton.Y + 3),
                Color.White);

            string displayUrl = url;

            while (
                displayUrl.Length > 0 &&
                Game1.smallFont.MeasureString(displayUrl).X >
                row.Width - 60)
            {
                displayUrl = displayUrl.Substring(
                    0,
                    displayUrl.Length - 1);
            }

            if (displayUrl.Length < url.Length)
                displayUrl += "...";

            b.DrawString(
                Game1.smallFont,
                displayUrl,
                new Vector2(row.X + 10, row.Y + 7),
                darkTheme ? Color.White : new Color(74, 57, 40));
        }

        if (bookmarks.All.Count > visibleRows)
        {
            b.DrawString(
                Game1.smallFont,
                "Use mouse wheel to scroll",
                new Vector2(
                    bookmarkPanelBounds.X + 16,
                    bookmarkPanelBounds.Bottom - 24),
                darkTheme
                    ? new Color(190, 190, 190)
                    : new Color(100, 85, 65));
        }
    }

    private bool HandleBookmarkPanelClick(int x, int y)
    {
        if (!bookmarkPanelBounds.Contains(x, y))
        {
            bookmarkPanelOpen = false;
            return false;
        }

        if (bookmarkCloseButton.Contains(x, y))
        {
            bookmarkPanelOpen = false;
            return true;
        }

        int rowHeight = 42;
        int firstRowY = bookmarkPanelBounds.Y + 52;
        int visibleRows = Math.Max(
            0,
            (bookmarkPanelBounds.Height - 64) / rowHeight);

        for (int i = 0; i < visibleRows; i++)
        {
            int index = bookmarkScrollOffset + i;

            if (index >= bookmarks.All.Count)
                break;

            Rectangle row = new Rectangle(
                bookmarkPanelBounds.X + 12,
                firstRowY + i * rowHeight,
                bookmarkPanelBounds.Width - 24,
                rowHeight - 4);

            if (!row.Contains(x, y))
                continue;

            string url = bookmarks.All[index];

            Rectangle removeButton = new Rectangle(
                row.Right - 38,
                row.Y + 4,
                32,
                row.Height - 8);

            if (removeButton.Contains(x, y))
            {
                bookmarks.Remove(url);

                int maxOffset = Math.Max(
                    0,
                    bookmarks.All.Count - visibleRows);

                bookmarkScrollOffset = Math.Min(
                    bookmarkScrollOffset,
                    maxOffset);

                return true;
            }

            addressBarFocused = false;
            keyboardSubscriber.EndAddressBar();
            addressText = url;
            chromiumBrowser.Navigate(url);
            bookmarkPanelOpen = false;

            return true;
        }

        return true;
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

        if (bookmarkPanelOpen &&
            HandleBookmarkPanelClick(x, y))
        {
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

        // SAVE BOOKMARK

        if (saveBookmarkButton.Contains(x, y))
        {
            bookmarks.Add(chromiumBrowser.CurrentUrl ?? string.Empty);
            return;
        }

        // BOOKMARK LIST

        if (listBookmarksButton.Contains(x, y))
        {
            bookmarkPanelOpen = !bookmarkPanelOpen;
            bookmarkScrollOffset = 0;
            addressBarFocused = false;
            keyboardSubscriber.EndAddressBar();
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
        int mouseX =
            Game1.getMouseX();

        int mouseY =
            Game1.getMouseY();

        if (bookmarkPanelOpen &&
            bookmarkPanelBounds.Contains(mouseX, mouseY))
        {
            int rowHeight = 42;
            int visibleRows = Math.Max(
                0,
                (bookmarkPanelBounds.Height - 64) / rowHeight);

            int maxOffset = Math.Max(
                0,
                bookmarks.All.Count - visibleRows);

            bookmarkScrollOffset = Math.Max(
                0,
                Math.Min(
                    maxOffset,
                    bookmarkScrollOffset - Math.Sign(delta)));

            return;
        }

        if (!chromiumBrowser.IsReady)
            return;

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


