using Aethvirse.Combat;
using Aethvirse.Input;
using Aethvirse.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.UI
{
    /// <summary>
    /// Pause menu (P / Start / touch ⏸, or Esc when the cursor is already free): resume, save to slots 1–3,
    /// load (autosave + slots), back to title, quit. Freezes time while open. Graybox IMGUI.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        static PauseMenu _instance;
        bool _open;
        bool _cursorWasLocked;
        CursorLockMode _cursorBefore;
        enum Page { Main, Save, Load }
        Page _page;
        GUIStyle _title, _button, _text;

        public static bool IsOpen => _instance != null && _instance._open;
        public static void Toggle() { if (_instance != null) _instance.SetOpen(!_instance._open); }

        void Awake() => _instance = this;

        void Update()
        {
            GameInput input = GameInput.Instance;
            bool esc = input.ReleaseCursor.WasPressedThisFrame();
            if (input.Pause.WasPressedThisFrame()) SetOpen(!_open);
            else if (esc && _open) SetOpen(false);
            // Esc first frees a captured cursor (camera); with a free cursor it pauses.
            else if (esc && !_cursorWasLocked && !Core.StatusPanel.IsOpen) SetOpen(true);
            _cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        void OnDestroy()
        {
            if (_open) TimeFx.Paused = false;
        }

        void SetOpen(bool open)
        {
            if (open == _open) return;
            _open = open;
            _page = Page.Main;
            TimeFx.Paused = open;
            if (open)
            {
                _cursorBefore = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (_cursorBefore == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnGUI()
        {
            if (!_open) return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _button ??= new GUIStyle(GUI.skin.button);
            _text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _title.fontSize = Mathf.RoundToInt(44 * s);
            _button.fontSize = Mathf.RoundToInt(26 * s);
            _text.fontSize = Mathf.RoundToInt(20 * s);

            GUI.color = new Color(0f, 0.02f, 0.06f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = 560 * s, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.18f, h = 64 * s, gap = 14 * s;
            GUI.Label(new Rect(0, y, Screen.width, 70 * s), _page == Page.Main ? "PAUSED" : _page == Page.Save ? "SAVE" : "LOAD", _title);
            y += 100 * s;

            switch (_page)
            {
                case Page.Main:
                    if (GUI.Button(new Rect(x, y, w, h), "Resume", _button)) SetOpen(false);
                    y += h + gap;
                    if (GUI.Button(new Rect(x, y, w, h), "Save", _button)) _page = Page.Save;
                    y += h + gap;
                    if (GUI.Button(new Rect(x, y, w, h), "Load", _button)) _page = Page.Load;
                    y += h + gap;
                    if (GUI.Button(new Rect(x, y, w, h), "Return to title", _button))
                    {
                        SetOpen(false);
                        SceneManager.LoadScene(TitleMenu.SceneName);
                    }
                    y += h + gap;
                    if (!Application.isMobilePlatform && GUI.Button(new Rect(x, y, w, h), "Quit", _button)) Application.Quit();
                    break;
                case Page.Save:
                    for (int slot = 1; slot <= SaveManager.ManualSlots; slot++)
                    {
                        if (GUI.Button(new Rect(x, y, w, h), SlotLabel(slot), _button) && SaveManager.Save(slot))
                        {
                            Codex.Notify($"Saved to slot {slot}.", CodexTone.System);
                            _page = Page.Main;
                        }
                        y += h + gap;
                    }
                    if (GUI.Button(new Rect(x, y, w, h), "Back", _button)) _page = Page.Main;
                    break;
                case Page.Load:
                    for (int slot = 0; slot <= SaveManager.ManualSlots; slot++)
                    {
                        bool exists = SaveManager.Info(slot) != null;
                        GUI.enabled = exists;
                        if (GUI.Button(new Rect(x, y, w, h), SlotLabel(slot), _button))
                        {
                            SetOpen(false);
                            SaveManager.Load(slot);
                        }
                        GUI.enabled = true;
                        y += h + gap;
                    }
                    if (GUI.Button(new Rect(x, y, w, h), "Back", _button)) _page = Page.Main;
                    break;
            }
        }

        public static string SlotLabel(int slot)
        {
            string name = slot == SaveManager.AutosaveSlot ? "Autosave" : $"Slot {slot}";
            SaveManager.SlotInfo i = SaveManager.Info(slot);
            if (i == null) return $"{name} — empty";
            int minutes = Mathf.FloorToInt(i.playtime / 60f);
            return $"{name} — Lv {i.level} · {minutes / 60}h {minutes % 60:00}m · {i.savedAt:dd MMM HH:mm}";
        }
    }
}
