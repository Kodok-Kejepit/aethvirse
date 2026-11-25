using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>Player settings (§12/§13), saved in PlayerPrefs and applied at once.</summary>
    public static class GameSettings
    {
        public static float Sensitivity { get => PlayerPrefs.GetFloat("sens", 1f); set { PlayerPrefs.SetFloat("sens", value); Changed?.Invoke(); } }
        public static bool InvertY { get => PlayerPrefs.GetInt("invert", 0) == 1; set { PlayerPrefs.SetInt("invert", value ? 1 : 0); Changed?.Invoke(); } }
        public static float CameraDistance { get => PlayerPrefs.GetFloat("camdist", 4.5f); set { PlayerPrefs.SetFloat("camdist", value); Changed?.Invoke(); } }
        public static float Volume { get => PlayerPrefs.GetFloat("volume", 0.8f); set { PlayerPrefs.SetFloat("volume", value); Apply(); } }
        /// <summary>0 Low · 1 Medium · 2 High · 3 Ultra (PC full version, §13).</summary>
        public static int Quality { get => PlayerPrefs.GetInt("quality", Application.isMobilePlatform ? 0 : 2); set { PlayerPrefs.SetInt("quality", value); Apply(); } }
        /// <summary>0 = monitor (VSync) · 30 · 60 · 120.</summary>
        public static int FpsCap { get => PlayerPrefs.GetInt("fps", Application.isMobilePlatform ? 60 : 0); set { PlayerPrefs.SetInt("fps", value); Apply(); } }
        /// <summary>PC: windowed instead of fullscreen (the screenshot tool uses it too).</summary>
        public static bool Windowed { get => PlayerPrefs.GetInt("windowed", 0) == 1 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-windowed") >= 0; set { PlayerPrefs.SetInt("windowed", value ? 1 : 0); Changed?.Invoke(); } }
        public static string Language { get => PlayerPrefs.GetString("lang", Application.systemLanguage == SystemLanguage.Indonesian ? "id" : "en"); set { PlayerPrefs.SetString("lang", value); Changed?.Invoke(); } }

        public static event System.Action Changed;
        public static readonly string[] QualityNames = { "Low", "Medium", "High", "Ultra" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Apply()
        {
            AudioListener.volume = Volume;
            int q = Mathf.Clamp(Quality, 0, 3);
            int levels = QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(Mathf.Clamp(Mathf.RoundToInt(q / 3f * (levels - 1)), 0, levels - 1), true);
            QualitySettings.shadowDistance = q switch { 0 => 25f, 1 => 50f, 2 => 90f, _ => 150f };
            QualitySettings.lodBias = q switch { 0 => 0.6f, 1 => 1f, 2 => 1.5f, _ => 2f };
            if (FpsCap == 0 && !Application.isMobilePlatform) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
            else { QualitySettings.vSyncCount = 0; Application.targetFrameRate = FpsCap == 0 ? 60 : FpsCap; }
            Changed?.Invoke();
        }
    }
}
