using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FarmNet;

public class ModEntry : Mod
{
    private SmartphoneIntegration? phone;

    public override void Entry(IModHelper helper)
    {
        Monitor.Log(
            "FarmNet has loaded!",
            LogLevel.Info);

        helper.Events.GameLoop.GameLaunched +=
            OnGameLaunched;

        helper.Events.Input.ButtonPressed +=
            OnButtonPressed;

        helper.Events.Input.MouseWheelScrolled +=
            OnMouseWheelScrolled;
    }

    private void OnGameLaunched(
        object? sender,
        GameLaunchedEventArgs e)
    {
        Monitor.Log(
            "FarmNet is ready.",
            LogLevel.Info);

        // Optional: show up as an app on the Smartphone mod.
        phone = new SmartphoneIntegration(
            Helper,
            Monitor,
            OpenFarmNet);

        phone.TryRegister();
    }

    private void OnButtonPressed(
        object? sender,
        ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button == SButton.F2)
            OpenFarmNet();
    }

    /// <summary>
    /// Single entry point used by both the F2 hotkey and the phone app.
    /// </summary>
    private void OpenFarmNet()
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is FarmNetMenu)
            return;

        Game1.activeClickableMenu =
            new FarmNetMenu();

        Monitor.Log(
            "FarmNet browser opened.",
            LogLevel.Info);
    }

    private void OnMouseWheelScrolled(
     object? sender,
     MouseWheelScrolledEventArgs e)
    {
        if (Game1.activeClickableMenu
            is FarmNetMenu farmNetMenu)
        {
            farmNetMenu.HandleMouseWheel(
                e.Delta);
        }
    }
}