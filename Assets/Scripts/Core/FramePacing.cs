using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Even frame pacing, which matters more for a smooth camera than raw FPS: uneven frame times
    /// read as stutter even at high frame rates. PC syncs to the monitor (no tearing, steady steps);
    /// phones are capped at 60. A Settings option comes with the Settings menu.
    /// </summary>
    public static class FramePacing
    {
        public const int MobileTargetFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = MobileTargetFps;
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
        }
    }
}
