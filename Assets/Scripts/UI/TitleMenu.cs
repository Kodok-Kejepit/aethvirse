using Aethvirse.Combat;
using Aethvirse.Save;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.UI
{
    /// <summary>Title screen: New Game, Continue (latest save), Load, Quit. Graybox IMGUI.</summary>
    public sealed class TitleMenu : MonoBehaviour
    {
        public const string SceneName = "Title";

        bool _loadPage;
        int _latest = -1;
        GUIStyle _title, _sub, _button;

        void Start()
        {
            TimeFx.ResetAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (GameSession.Exists) GameSession.Instance.WorldRunning = false;
            _latest = SaveManager.LatestSlot();
        }

        public static void NewGame()
        {
            GameSession.Instance.ResetForNewGame();
            Quests.QuestSystem.Instance.ResetForNewGame();
            SaveManager.TakePending();
            SceneManager.LoadScene(Core.BootLoader.NewGameScene);
        }

        void OnGUI()
        {
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _sub ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _button ??= new GUIStyle(GUI.skin.button);
            _title.fontSize = Mathf.RoundToInt(84 * s);
            _sub.fontSize = Mathf.RoundToInt(28 * s);
            _button.fontSize = Mathf.RoundToInt(28 * s);
            _title.normal.textColor = new Color(0.85f, 0.75f, 0.45f);
            _sub.normal.textColor = new Color(0.6f, 0.8f, 1f);

            GUI.Label(new Rect(0, Screen.height * 0.14f, Screen.width, 110 * s), "AETHVIRSE", _title);
            GUI.Label(new Rect(0, Screen.height * 0.14f + 100 * s, Screen.width, 40 * s), "UNSEAL THE CODEX", _sub);

            float w = 560 * s, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.45f, h = 66 * s, gap = 16 * s;
            if (_loadPage)
            {
                for (int slot = 0; slot <= SaveManager.ManualSlots; slot++)
                {
                    GUI.enabled = SaveManager.Info(slot) != null;
                    if (GUI.Button(new Rect(x, y, w, h), PauseMenu.SlotLabel(slot), _button)) SaveManager.Load(slot);
                    GUI.enabled = true;
                    y += h + gap;
                }
                if (GUI.Button(new Rect(x, y, w, h), "Back", _button)) _loadPage = false;
                return;
            }
            if (_latest >= 0)
            {
                if (GUI.Button(new Rect(x, y, w, h), "Continue", _button)) SaveManager.Load(_latest);
                y += h + gap;
            }
            if (GUI.Button(new Rect(x, y, w, h), "New Game", _button)) NewGame();
            y += h + gap;
            GUI.enabled = _latest >= 0;
            if (GUI.Button(new Rect(x, y, w, h), "Load", _button)) _loadPage = true;
            GUI.enabled = true;
            y += h + gap;
            if (!Application.isMobilePlatform && GUI.Button(new Rect(x, y, w, h), "Quit", _button)) Application.Quit();
        }
    }
}
