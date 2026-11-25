using Aethvirse.Input;
using Aethvirse.Interaction;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>
    /// A lock of letter dials with a coded inscription. The key to the code is somewhere else in the region
    /// (a mural). Turn the dials (works with touch), try the word; the right word sets a flag and pays.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class CipherLock : MonoBehaviour
    {
        [SerializeField] string lockId = "";
        [SerializeField] string answer = "";
        [SerializeField] string inscription = "";
        [SerializeField] string rewardItem = "";

        public static string SolvedFlag(string id) => "cipher_" + id;

        static CipherLock _open;
        char[] _dials;
        CursorLockMode _cursorBefore;
        GUIStyle _title, _text, _button, _dial;
        string _status = "";

        public static bool IsOpen => _open != null;

        void Awake()
        {
            var use = GetComponent<Interactable>();
            use.Prompt = "Examine the lock";
            use.Used += _ => Open();
        }

        void Update()
        {
            GetComponent<Interactable>().Available = !GameSession.Instance.Flags.Has(SolvedFlag(lockId));
            if (_open == this && GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) Close();
        }

        void Open()
        {
            if (GameSession.Instance.Flags.Has(SolvedFlag(lockId))) return;
            _dials ??= new string('A', answer.Length).ToCharArray();
            _open = this;
            _status = "";
            _cursorBefore = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Close()
        {
            _open = null;
            if (_cursorBefore == CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }

        void Try()
        {
            if (new string(_dials) != answer) { _status = "The dials click back. Nothing."; return; }
            GameSession.Instance.Flags.SetFlag(SolvedFlag(lockId));
            var inv = Items.PlayerInventory.Current;
            if (inv != null && !string.IsNullOrEmpty(rewardItem) && inv.Database.Get(rewardItem) != null)
            {
                inv.Bag.Add(rewardItem);
                Codex.Notify($"The lock opens: {inv.Database.Get(rewardItem).displayName}", CodexTone.Reward);
            }
            else Codex.Notify("The lock opens.", CodexTone.Reward);
            Quests.QuestSystem.Instance?.AddAcademyPoints(20);
            Close();
        }

        void OnGUI()
        {
            GUI.depth = -10;
            if (_open != this) return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _button ??= new GUIStyle(GUI.skin.button);
            _dial ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _title.fontSize = Mathf.RoundToInt(34 * s);
            _text.fontSize = Mathf.RoundToInt(24 * s);
            _button.fontSize = Mathf.RoundToInt(28 * s);
            _dial.fontSize = Mathf.RoundToInt(48 * s);
            float w = 900 * s, h = 620 * s;
            var area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.color = new Color(0.03f, 0.05f, 0.1f, 0.95f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x, area.y + 20 * s, w, 60 * s), inscription, _title);
            GUI.Label(new Rect(area.x, area.y + 90 * s, w, 40 * s), "Letter dials. The words above are not the answer — they hide it.", _text);
            float dw = 110 * s, gap = 16 * s;
            float x0 = area.x + (w - (_dials.Length * dw + (_dials.Length - 1) * gap)) * 0.5f;
            for (int i = 0; i < _dials.Length; i++)
            {
                float x = x0 + i * (dw + gap);
                if (GUI.Button(new Rect(x, area.y + 160 * s, dw, 70 * s), "▲", _button)) _dials[i] = _dials[i] == 'Z' ? 'A' : (char)(_dials[i] + 1);
                GUI.Box(new Rect(x, area.y + 240 * s, dw, 110 * s), _dials[i].ToString(), _dial);
                if (GUI.Button(new Rect(x, area.y + 360 * s, dw, 70 * s), "▼", _button)) _dials[i] = _dials[i] == 'A' ? 'Z' : (char)(_dials[i] - 1);
            }
            GUI.Label(new Rect(area.x, area.y + 445 * s, w, 40 * s), _status, _text);
            if (GUI.Button(new Rect(area.x + w * 0.2f, area.y + h - 100 * s, w * 0.28f, 70 * s), "Try", _button)) Try();
            if (GUI.Button(new Rect(area.x + w * 0.52f, area.y + h - 100 * s, w * 0.28f, 70 * s), "Leave", _button)) Close();
        }
    }
}
