using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FarmNet;

public class ModEntry : Mod
{
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
    }

    private void OnButtonPressed(
        object? sender,
        ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button == SButton.F2)
        {
            Game1.activeClickableMenu =
                new FarmNetMenu();

            Monitor.Log(
                "FarmNet browser opened.",
                LogLevel.Info);
        }
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