using System.Collections.Generic;
using Aethvirse.Core;
using Aethvirse.Input;
using Aethvirse.Items;
using Aethvirse.Quests;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Dialog
{
    /// <summary>
    /// Runs a conversation: lines with a typing effect (tap / E / click to advance), then choices; applies effects.
    /// Graybox IMGUI at the bottom of the screen; portraits and styling come in Phase 2.
    /// </summary>
    public sealed class DialogBox : MonoBehaviour
    {
        const float CharsPerSecond = 45f;

        static DialogBox _instance;
        NpcData _npc;
        readonly List<string> _lines = new List<string>();
        int _line;
        float _shown;
        List<DialogChoice> _choices;
        GUIStyle _name, _text, _button;
        CursorLockMode _cursorBefore;
        bool _advance;

        public static bool IsOpen => _instance != null && _instance._npc != null;

        public static void Begin(NpcData npc)
        {
            if (_instance == null)
            {
                var go = new GameObject("DialogBox");
                _instance = go.AddComponent<DialogBox>();
            }
            _instance.Open(npc);
        }

        void Open(NpcData npc)
        {
            DialogEntry e = npc.Pick(GameContext.Instance);
            if (e == null) return;
            _npc = npc;
            _lines.Clear();
            _lines.AddRange(e.lines);
            if (e.dailyVariants.Count > 0) _lines.Add(e.dailyVariants[GameSession.Instance.Clock.Day % e.dailyVariants.Count]);
            _line = 0;
            _shown = 0f;
            _choices = e.choices.FindAll(c => Condition.All(c.conditions, GameContext.Instance));
            Apply(e.onShow);
            GameEvents.RaiseTalked(npc.id);
            _cursorBefore = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Close()
        {
            _npc = null;
            if (_cursorBefore == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update()
        {
            if (_npc == null) return;
            _shown += Time.unscaledDeltaTime * CharsPerSecond;
            if (GameInput.Instance.Interact.WasPressedThisFrame() || GameInput.Instance.Jump.WasPressedThisFrame()) _advance = true;
            if (GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) Close();
        }

        void Advance()
        {
            if (_line >= _lines.Count) return;
            if (_shown < _lines[_line].Length) { _shown = _lines[_line].Length; return; }
            _line++;
            _shown = 0f;
            if (_line >= _lines.Count && _choices.Count == 0) Close();
        }

        void Choose(DialogChoice c)
        {
            if (c.goldCost > 0 && !GameSession.Instance.SpendGold(c.goldCost))
            {
                Codex.Notify("Not enough gold.", CodexTone.Warning);
                return;
            }
            Apply(c.effects);
            if (c.reply.Count > 0)
            {
                _lines.Clear();
                _lines.AddRange(c.reply);
                _line = 0;
                _shown = 0f;
                _choices = new List<DialogChoice>();
            }
            else Close();
        }

        static void Apply(List<DialogEffect> effects)
        {
            GameSession s = GameSession.Instance;
            PlayerInventory inv = PlayerInventory.Current;
            foreach (DialogEffect e in effects)
            {
                switch (e.type)
                {
                    case DialogEffectType.GiveItem: inv?.Bag.Add(e.key, e.amount); Codex.Notify($"Received {e.amount}× {inv?.Database.Get(e.key)?.displayName ?? e.key}", CodexTone.Reward); break;
                    case DialogEffectType.TakeItem: inv?.Bag.Remove(e.key, e.amount); break;
                    case DialogEffectType.GiveGold: s.AddGold(e.amount); break;
                    case DialogEffectType.TakeGold: s.SpendGold(e.amount); break;
                    case DialogEffectType.SetFlag: s.Flags.SetFlag(e.key); break;
                    case DialogEffectType.StartQuest: QuestSystem.Instance.Start(e.key); break;
                    case DialogEffectType.AcademyPoints: QuestSystem.Instance.AddAcademyPoints(e.amount); break;
                    case DialogEffectType.CodexMessage: Codex.Notify(e.text, CodexTone.Anomaly); break;
                    case DialogEffectType.OpenShop: InventoryPanel.OpenShop(); break;
                }
            }
        }

        void OnGUI()
        {
            if (_npc == null) return;
            float s = Screen.height / 1080f;
            _name ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _text ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            _button ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            _name.fontSize = Mathf.RoundToInt(28 * s);
            _text.fontSize = Mathf.RoundToInt(26 * s);
            _button.fontSize = Mathf.RoundToInt(24 * s);
            _name.normal.textColor = new Color(0.6f, 0.85f, 1f);

            var box = new Rect(Screen.width * 0.15f, Screen.height - 330 * s, Screen.width * 0.7f, 300 * s);
            GUI.color = new Color(0.02f, 0.06f, 0.14f, 0.92f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(box.x + 24 * s, box.y + 12 * s, box.width, 40 * s), _npc.displayName, _name);
            if (_line < _lines.Count)
            {
                string full = _lines[_line];
                string part = full.Substring(0, Mathf.Min(full.Length, Mathf.FloorToInt(_shown)));
                GUI.Label(new Rect(box.x + 24 * s, box.y + 60 * s, box.width - 48 * s, box.height - 80 * s), part, _text);
                // The whole box is a "next" button (touch / mouse).
                if (GUI.Button(box, GUIContent.none, GUIStyle.none) || _advance) Advance();
            }
            else
            {
                float y = box.y + 60 * s;
                foreach (DialogChoice c in new List<DialogChoice>(_choices))
                {
                    string label = c.goldCost > 0 ? $"{c.text}  ({c.goldCost}g)" : c.text;
                    if (GUI.Button(new Rect(box.x + 24 * s, y, box.width - 48 * s, 52 * s), label, _button)) Choose(c);
                    y += 60 * s;
                }
            }
            _advance = false;
        }
    }
}
