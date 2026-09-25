using CefSharp;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;

namespace FarmNet;

public class FarmNetKeyboardSubscriber : IKeyboardSubscriber
{
    private readonly ChromiumBrowser chromiumBrowser;

    public bool Selected { get; set; }

    // True when the FarmNet address bar has focus.
    public bool AddressBarActive { get; set; }

    // The text currently inside the FarmNet address bar.
    public string AddressText =>
     addressText;

    private string addressText =
        string.Empty;

    // Called when Enter is pressed in the address bar.
    public Action<string>? AddressSubmitted;

    public FarmNetKeyboardSubscriber(
        ChromiumBrowser chromiumBrowser)
    {
        this.chromiumBrowser =
            chromiumBrowser;

        Selected = true;
    }

    // ---------------------------------------------------------
    // ADDRESS BAR
    // ---------------------------------------------------------

    public void BeginAddressBar(
        string currentUrl)
    {
        AddressBarActive =
            true;

        addressText =
            currentUrl ?? string.Empty;

        Selected =
            true;
    }

    public void EndAddressBar()
    {
        AddressBarActive =
            false;

        addressText =
            string.Empty;

        Selected =
            true;
    }

    // ---------------------------------------------------------
    // NORMAL TEXT INPUT
    // ---------------------------------------------------------

    public void RecieveTextInput(
        char inputChar)
    {
        if (!Selected)
            return;

        if (AddressBarActive)
        {
            AddAddressCharacter(
                inputChar);

            return;
        }

        SendCharacter(
            inputChar);
    }

    public void RecieveTextInput(
        string text)
    {
        if (!Selected)
            return;

        if (string.IsNullOrEmpty(text))
            return;

        foreach (char c in text)
        {
            if (AddressBarActive)
            {
                AddAddressCharacter(c);
            }
            else
            {
                SendCharacter(c);
            }
        }
    }

    // ---------------------------------------------------------
    // COMMAND INPUT
    // ---------------------------------------------------------

    public void RecieveCommandInput(
        char command)
    {
        if (!Selected)
            return;

        if (AddressBarActive)
        {
            switch (command)
            {
                case '\b':
                    RemoveAddressCharacter();
                    break;

                case '\r':
                case '\n':
                    SubmitAddress();
                    break;

                case '\t':
                    EndAddressBar();
                    break;
            }

            return;
        }

        var host =
            chromiumBrowser.Browser?
                .GetBrowserHost();

        if (host == null)
            return;

        switch (command)
        {
            case '\b':
                SendSpecialKey(
                    host,
                    0x08);
                break;

            case '\r':
            case '\n':
                SubmitSearch();
                break;

            case '\t':
                SendSpecialKey(
                    host,
                    0x09);
                break;
        }
    }

    // ---------------------------------------------------------
    // SPECIAL KEYS
    // ---------------------------------------------------------

    public void RecieveSpecialInput(
        Keys key)
    {
        if (!Selected)
            return;

        // -----------------------------------------------------
        // ADDRESS BAR
        // -----------------------------------------------------

        if (AddressBarActive)
        {
            if (key == Keys.Back)
            {
                RemoveAddressCharacter();
                return;
            }

            if (key == Keys.Enter)
            {
                SubmitAddress();
                return;
            }

            if (key == Keys.Escape)
            {
                EndAddressBar();
                return;
            }

            // Ctrl+A
            if (key == Keys.A &&
                IsControlPressed())
            {
                addressText =
                    string.Empty;

                return;
            }

            return;
        }

        // -----------------------------------------------------
        // NORMAL WEBPAGE
        // -----------------------------------------------------

        var host =
            chromiumBrowser.Browser?
                .GetBrowserHost();

        if (host == null)
            return;

        int keyCode =
            GetWindowsKeyCode(key);

        if (keyCode == 0)
            return;

        if (key == Keys.Enter)
        {
            SubmitSearch();
            return;
        }

        SendSpecialKey(
            host,
            keyCode);
    }

    // ---------------------------------------------------------
    // ADDRESS TEXT
    // ---------------------------------------------------------

    private void AddAddressCharacter(
        char character)
    {
        addressText +=
            character;
    }

    private void RemoveAddressCharacter()
    {
        if (AddressText.Length <= 0)
            return;

        addressText =
            AddressText.Substring(
                0,
                AddressText.Length - 1);
    }

    private void SubmitAddress()
    {
        if (string.IsNullOrWhiteSpace(
                AddressText))
        {
            return;
        }

        string url =
            AddressText.Trim();

        AddressBarActive =
            false;

        Selected =
            true;

        AddressSubmitted?.Invoke(
            url);
    }

    // ---------------------------------------------------------
    // CTRL KEY CHECK
    // ---------------------------------------------------------

    private bool IsControlPressed()
    {
        return
            Game1.input.GetKeyboardState()
                .IsKeyDown(Keys.LeftControl)
            ||
            Game1.input.GetKeyboardState()
                .IsKeyDown(Keys.RightControl);
    }

    // ---------------------------------------------------------
    // SEND CHARACTER TO CHROMIUM
    // ---------------------------------------------------------

    private void SendCharacter(
        char character)
    {
        if (!chromiumBrowser.IsReady)
            return;

        var host =
            chromiumBrowser.Browser?
                .GetBrowserHost();

        if (host == null)
            return;

        int characterCode =
            (int)character;

        var keyEvent =
            new CefSharp.KeyEvent
            {
                Type =
                    CefSharp.KeyEventType.Char,

                WindowsKeyCode =
                    characterCode,

                NativeKeyCode =
                    characterCode,

                FocusOnEditableField =
                    true,

                IsSystemKey =
                    false
            };

        host.SendKeyEvent(
            keyEvent);
    }

    // ---------------------------------------------------------
    // SEARCH SUBMIT
    // ---------------------------------------------------------

    private void SubmitSearch()
    {
        var browser =
            chromiumBrowser.Browser;

        if (browser == null ||
            !browser.IsBrowserInitialized)
        {
            return;
        }

        const string script = @"
            (function () {

                var searchInput =
                    document.querySelector(
                        '#searchInput'
                    );

                if (!searchInput) {

                    searchInput =
                        document.querySelector(
                            'input[name=""search""]'
                        );
                }

                if (!searchInput) {

                    searchInput =
                        document.querySelector(
                            'input[type=""search""]'
                        );
                }

                if (!searchInput) {
                    return;
                }

                searchInput.focus();

                var form =
                    searchInput.closest('form');

                if (form) {

                    if (
                        typeof form.requestSubmit ===
                        'function'
                    ) {
                        form.requestSubmit();
                    }
                    else {
                        form.submit();
                    }

                    return;
                }

                var button =
                    document.querySelector(
                        'button[type=""submit""]'
                    );

                if (button) {
                    button.click();
                    return;
                }

                var searchButton =
                    document.querySelector(
                        '#searchButton'
                    );

                if (searchButton) {
                    searchButton.click();
                }

            })();
        ";

        browser.ExecuteScriptAsync(
            script);
    }

    // ---------------------------------------------------------
    // SPECIAL KEY SENDER
    // ---------------------------------------------------------

    private void SendSpecialKey(
        CefSharp.IBrowserHost host,
        int keyCode)
    {
        var keyDown =
            new CefSharp.KeyEvent
            {
                Type =
                    CefSharp.KeyEventType.KeyDown,

                WindowsKeyCode =
                    keyCode,

                NativeKeyCode =
                    keyCode,

                FocusOnEditableField =
                    true,

                IsSystemKey =
                    false
            };

        host.SendKeyEvent(
            keyDown);

        var keyUp =
            new CefSharp.KeyEvent
            {
                Type =
                    CefSharp.KeyEventType.KeyUp,

                WindowsKeyCode =
                    keyCode,

                NativeKeyCode =
                    keyCode,

                FocusOnEditableField =
                    true,

                IsSystemKey =
                    false
            };

        host.SendKeyEvent(
            keyUp);
    }

    // ---------------------------------------------------------
    // KEY MAPPING
    // ---------------------------------------------------------

    private int GetWindowsKeyCode(
        Keys key)
    {
        if (key >= Keys.A &&
            key <= Keys.Z)
        {
            return (int)key;
        }

        if (key >= Keys.D0 &&
            key <= Keys.D9)
        {
            return (int)key;
        }

        if (key >= Keys.NumPad0 &&
            key <= Keys.NumPad9)
        {
            return (int)key;
        }

        return key switch
        {
            Keys.Back => 0x08,
            Keys.Tab => 0x09,
            Keys.Enter => 0x0D,

            Keys.LeftShift => 0x10,
            Keys.RightShift => 0x10,

            Keys.LeftControl => 0x11,
            Keys.RightControl => 0x11,

            Keys.LeftAlt => 0x12,
            Keys.RightAlt => 0x12,

            Keys.CapsLock => 0x14,

            Keys.Escape => 0x1B,
            Keys.Space => 0x20,

            Keys.PageUp => 0x21,
            Keys.PageDown => 0x22,

            Keys.End => 0x23,
            Keys.Home => 0x24,

            Keys.Left => 0x25,
            Keys.Up => 0x26,
            Keys.Right => 0x27,
            Keys.Down => 0x28,

            Keys.Insert => 0x2D,
            Keys.Delete => 0x2E,

            Keys.F1 => 0x70,
            Keys.F2 => 0x71,
            Keys.F3 => 0x72,
            Keys.F4 => 0x73,
            Keys.F5 => 0x74,
            Keys.F6 => 0x75,
            Keys.F7 => 0x76,
            Keys.F8 => 0x77,
            Keys.F9 => 0x78,
            Keys.F10 => 0x79,
            Keys.F11 => 0x7A,
            Keys.F12 => 0x7B,

            Keys.OemSemicolon => 0xBA,
            Keys.OemPlus => 0xBB,
            Keys.OemComma => 0xBC,
            Keys.OemMinus => 0xBD,
            Keys.OemPeriod => 0xBE,
            Keys.OemQuestion => 0xBF,

            Keys.OemTilde => 0xC0,
            Keys.OemOpenBrackets => 0xDB,
            Keys.OemPipe => 0xDC,
            Keys.OemCloseBrackets => 0xDD,
            Keys.OemQuotes => 0xDE,

            _ => 0
        };
    }

    // ---------------------------------------------------------
    // KEYBOARD FOCUS
    // ---------------------------------------------------------

    public void OnKeyboardFocusChange(
        bool hasFocus)
    {
        Selected =
            hasFocus;
    }
}