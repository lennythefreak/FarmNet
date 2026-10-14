using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Reflection;

namespace FarmNet;

/// <summary>
/// Registers FarmNet as an app on d5a1lamdtd's "Smartphone" mod.
///
/// Smartphone exposes ISmartPhoneApi.RegisterPhoneApp(...). We call it by
/// reflection so FarmNet needs no compile-time reference to Smartphone.dll
/// and still loads fine when the phone mod is not installed.
/// </summary>
internal sealed class SmartphoneIntegration
{
    public const string SmartphoneModId = "d5a1lamdtd.Smartphone";
    public const string AppId = "farmnet_browser";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Action openFarmNet;

    private bool openQueued;

    public SmartphoneIntegration(
        IModHelper helper,
        IMonitor monitor,
        Action openFarmNet)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.openFarmNet = openFarmNet;
    }

    public bool TryRegister()
    {
        if (!helper.ModRegistry.IsLoaded(SmartphoneModId))
        {
            monitor.Log(
                "Smartphone mod not found; FarmNet stays F2-only.",
                LogLevel.Info);
            return false;
        }

        try
        {
            // Raw (unproxied) API object. Reflection sidesteps the
            // Smartphone.AppSize enum, which we can't reference directly.
            object? api = helper.ModRegistry.GetApi(SmartphoneModId);

            MethodInfo? register =
                api?.GetType().GetMethod("RegisterPhoneApp");

            if (api == null || register == null)
            {
                monitor.Log(
                    "Smartphone API / RegisterPhoneApp not found.",
                    LogLevel.Warn);
                return false;
            }

            Texture2D icon =
                helper.ModContent.Load<Texture2D>("assets/icon.png");

            var themedIcons =
                new Dictionary<string, Texture2D>
                {
                    ["default"] = icon
                };

            // Order matches ISmartPhoneApi.RegisterPhoneApp(
            //   ownerModId, appId, displayName, onClick,
            //   closePhoneOnLaunch, sourceRect, getBadgeCount,
            //   supportedSizes, onDrawWidget, themedIconTextures)
            object?[] args =
            {
                ModManifestId(),
                AppId,
                "FarmNet",
                (Action)OnPhoneAppClicked,
                true,   // close the phone UI when launching
                null,   // sourceRect: use whole icon texture
                null,   // getBadgeCount: no badge
                null,   // supportedSizes: default 1x1 icon
                null,   // onDrawWidget: no widget
                themedIcons
            };

            object? result = register.Invoke(api, args);

            monitor.Log(
                result is bool ok && !ok
                    ? "Smartphone refused FarmNet registration."
                    : "FarmNet registered as a Smartphone app.",
                result is bool ok2 && !ok2
                    ? LogLevel.Warn
                    : LogLevel.Info);

            return result is not bool b || b;
        }
        catch (Exception ex)
        {
            monitor.Log(
                $"Smartphone integration failed: {ex}",
                LogLevel.Error);
            return false;
        }
    }

    private string ModManifestId() => "LennyTheFreak.FarmNet";

    /// <summary>
    /// Called by the phone when the icon is tapped. The phone menu is
    /// still the active menu at this point (and closes itself because
    /// closePhoneOnLaunch = true), so we wait one tick before opening
    /// FarmNet so our menu isn't clobbered.
    /// </summary>
    private void OnPhoneAppClicked()
    {
        if (openQueued)
            return;

        openQueued = true;
        helper.Events.GameLoop.UpdateTicked += OpenNextTick;
    }

    private void OpenNextTick(object? sender, UpdateTickedEventArgs e)
    {
        helper.Events.GameLoop.UpdateTicked -= OpenNextTick;
        openQueued = false;

        openFarmNet();
    }
}
