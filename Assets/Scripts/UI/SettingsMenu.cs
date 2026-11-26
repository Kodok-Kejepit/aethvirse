using UnityEngine.InputSystem;
using Aethvirse.Core;
using UnityEngine;

namespace Aethvirse.UI
{
    /// <summary>Settings page (from the pause menu or the title): camera, volume, graphics preset, FPS cap, language.</summary>
    public static class SettingsMenu
    {
        static GUIStyle _label, _button;

        static bool _controls;
        static InputActionRebindingExtensions.RebindingOperation _rebind;

        /// <summary>Draws the page; returns true when Back was pressed.</summary>
        public static bool Draw(float s, float x, float y, float w)
        {
            if (_controls) return DrawControls(s, x, y, w);
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
            if (GUI.Button(new Rect(x, y, w, h), $"{Loc.T("difficulty")}: {GameSettings.DifficultyNames[GameSettings.Difficulty]}", _button)) GameSettings.Difficulty = (GameSettings.Difficulty + 1) % 4;
            y += h + gap;
            if (!Application.isMobilePlatform && GUI.Button(new Rect(x, y, w, h), Loc.T("controls") + " …", _button)) _controls = true;
            if (!Application.isMobilePlatform) y += h + gap;
            return GUI.Button(new Rect(x, y, w, h), Loc.T("back"), _button);
        }

        /// <summary>Keyboard rebinding: click a key, press the new one (Esc cancels). Saved at once.</summary>
        static bool DrawControls(float s, float x, float y, float w)
        {
            var input = Input.GameInput.Instance;
            var actions = new (string name, InputAction a)[]
            {
                ("Jump", input.Jump), ("Dodge", input.Dodge), ("Sprint", input.Sprint), ("Interact", input.Interact),
                ("Skill 1", input.Skills[0]), ("Skill 2", input.Skills[1]), ("Skill 3", input.Skills[2]), ("Skill 4", input.Skills[3]),
                ("Ultimate", input.Ultimate), ("Lock on", input.LockOn), ("Status", input.StatusPanel), ("Bag", input.InventoryPanel),
                ("Map", input.Map), ("Potion", input.QuickPotion), ("Pause", input.Pause),
            };
            float h = 40 * s, gap = 8 * s, half = w * 0.5f;
            for (int i = 0; i < actions.Length; i++)
            {
                var (name, a) = actions[i];
                int b = Input.GameInput.KeyboardBinding(a);
                if (b < 0) continue;
                float cx = x + (i % 2) * (half + 8 * s), cy = y + (i / 2) * (h + gap);
                GUI.Label(new Rect(cx, cy, half * 0.45f, h), name, _label);
                bool waiting = _rebind != null && _rebind.action == a;
                if (GUI.Button(new Rect(cx + half * 0.45f, cy, half * 0.5f, h), waiting ? "…" : a.GetBindingDisplayString(b), _button) && _rebind == null)
                {
                    input.Player.Disable();
                    _rebind = a.PerformInteractiveRebinding(b)
                        .WithControlsHavingToMatchPath("<Keyboard>")
                        .WithCancelingThrough("<Keyboard>/escape")
                        .OnComplete(op => { op.Dispose(); _rebind = null; input.Player.Enable(); input.SaveBindings(); })
                        .OnCancel(op => { op.Dispose(); _rebind = null; input.Player.Enable(); })
                        .Start();
                }
            }
            y += ((actions.Length + 1) / 2) * (h + gap) + gap;
            if (GUI.Button(new Rect(x, y, w * 0.48f, h), Loc.T("reset_keys"), _button)) input.ResetBindings();
            if (GUI.Button(new Rect(x + w * 0.52f, y, w * 0.48f, h), Loc.T("back"), _button)) _controls = false;
            return false;
        }
    }
}
