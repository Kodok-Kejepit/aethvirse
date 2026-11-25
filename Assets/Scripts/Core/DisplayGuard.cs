using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Keeps the desktop game at the monitor's own resolution in fullscreen. Tiling window managers (Hyprland) first
    /// give a new window a tile size (e.g. 860×836); Unity would take that, stretch it over a 16:9 screen and save it
    /// — everything looked squashed, even the splash. This puts it back and keeps it right.
    /// Pure check: <see cref="NeedsFix"/>.
    /// </summary>
    public sealed class DisplayGuard : MonoBehaviour
    {
        float _timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Application.isMobilePlatform || Application.isEditor) return;
            var go = new GameObject("DisplayGuard");
            DontDestroyOnLoad(go);
            go.AddComponent<DisplayGuard>();
            Fix();
        }

        /// <summary>True when a fullscreen game is not at the display's size (a stretched picture).</summary>
        public static bool NeedsFix(int screenW, int screenH, int displayW, int displayH, bool fullscreen) =>
            fullscreen && displayW > 0 && displayH > 0 && (screenW != displayW || screenH != displayH);

        static void Fix()
        {
            DisplayInfo d = Screen.mainWindowDisplayInfo;
            int w = d.width > 0 ? d.width : Display.main.systemWidth, h = d.height > 0 ? d.height : Display.main.systemHeight;
            if (GameSettings.Windowed) return;
            if (NeedsFix(Screen.width, Screen.height, w, h, true) || Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
                Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
        }

        void Update()
        {
            // Check a few times a second: the window manager may resize us again after start or alt-tab.
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            if (!GameSettings.Windowed) Fix();
        }
    }
}
