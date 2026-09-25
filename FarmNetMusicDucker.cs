using System;
using StardewValley;

namespace FarmNet;

public class FarmNetMusicDucker
{
    private const float DuckVolume = 0.0f;

    private const float FadeSpeed = 2.5f;

    private float normalVolume;

    private float currentVolume;

    private bool initialized;

    private bool browserAudioActive;

    public void Initialize()
    {
        normalVolume =
            Game1.options.musicVolumeLevel;

        currentVolume =
            normalVolume;

        initialized =
            true;

        Game1.musicCategory.SetVolume(
            normalVolume);
    }

    public void SetBrowserAudioActive(
        bool active)
    {
        if (!initialized)
            Initialize();

        if (browserAudioActive == active)
            return;

        browserAudioActive =
            active;

        if (!active)
        {
            /*
             * Refresh this in case the player
             * changed their music volume while
             * FarmNet was open.
             */
            normalVolume =
                Game1.options.musicVolumeLevel;
        }
    }

    public void Update(
        float deltaTime)
    {
        if (!initialized)
            return;

        float targetVolume;

        if (browserAudioActive)
        {
            targetVolume =
                normalVolume *
                DuckVolume;
        }
        else
        {
            targetVolume =
                normalVolume;
        }

        currentVolume =
            MoveTowards(
                currentVolume,
                targetVolume,
                FadeSpeed * deltaTime);

        Game1.musicCategory.SetVolume(
            currentVolume);
    }

    private static float MoveTowards(
        float current,
        float target,
        float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
            return target;

        if (current < target)
            return current + maxDelta;

        return current - maxDelta;
    }

    public void Restore()
    {
        if (!initialized)
            return;

        float volume =
            Game1.options.musicVolumeLevel;

        currentVolume =
            volume;

        browserAudioActive =
            false;

        Game1.musicCategory.SetVolume(
            volume);
    }
}