using Aethvirse.Core;
using UnityEngine;

namespace Aethvirse.UI
{
    /// <summary>Settings page (from the pause menu or the title): camera, volume, graphics preset, FPS cap, language.</summary>
    public static class SettingsMenu
    {
        static GUIStyle _label, _button;

        /// <summary>Draws the page; returns true when Back was pressed.</summary>
        public static bool Draw(float s, float x, float y, float w)
        {
            _label ??= new GUIStyle(GUI.skin.label);
            _button ??= new GUIStyle(GUI.skin.button);
            _label.fontSize = Mathf.RoundToInt(24 * s);
            _button.fontSize = Mathf.RoundToInt(22 * s);
            float h = 48 * s, gap = 16 * s, half = w * 0.45f;

            GUI.Label(new Rect(x, y, half, h), $"{Loc.T("sensitivity")}  {GameSettings.Sensitivity:0.00}", _label);
            GameSettings.Sensitivity = GUI.HorizontalSlider(new Rect(x + half, y + h * 0.4f, w - half, h), GameSettings.Sensitivity, 0.2f, 3f);
            y += h + gap;
            GUI.Label(new Rect(x, y, half, h), $"{Loc.T("distance")}  {GameSettings.CameraDistance:0.0}", _label);
            GameSettings.CameraDistance = GUI.HorizontalSlider(new Rect(x + half, y + h * 0.4f, w - half, h), GameSettings.CameraDistance, 2.5f, 8f);
            y += h + gap;
            GUI.Label(new Rect(x, y, half, h), $"{Loc.T("volume")}  {Mathf.RoundToInt(GameSettings.Volume * 100f)}%", _label);
            float v = GUI.HorizontalSlider(new Rect(x + half, y + h * 0.4f, w - half, h), GameSettings.Volume, 0f, 1f);
            if (!Mathf.Approximately(v, GameSettings.Volume)) GameSettings.Volume = v;
            y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), $"{Loc.T("invert")}: {(GameSettings.InvertY ? Loc.T("on") : Loc.T("off"))}", _button)) GameSettings.InvertY = !GameSettings.InvertY;
            y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), $"{Loc.T("quality")}: {GameSettings.QualityNames[GameSettings.Quality]}", _button)) GameSettings.Quality = (GameSettings.Quality + 1) % 4;
            y += h + gap;
            int[] caps = { 0, 30, 60, 120 };
            int ci = System.Array.IndexOf(caps, GameSettings.FpsCap);
            if (GUI.Button(new Rect(x, y, w, h), $"{Loc.T("fps")}: {(GameSettings.FpsCap == 0 ? Loc.T("monitor") : GameSettings.FpsCap.ToString())}", _button)) GameSettings.FpsCap = caps[(ci + 1) % caps.Length];
            y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), $"{Loc.T("language")}: {(GameSettings.Language == "id" ? "Bahasa Indonesia" : "English")}", _button)) GameSettings.Language = GameSettings.Language == "id" ? "en" : "id";
            y += h + gap;
            return GUI.Button(new Rect(x, y, w, h), Loc.T("back"), _button);
        }
    }
}
