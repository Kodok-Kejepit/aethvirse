using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Graybox HUD: HP / MP / stamina, level + XP, skill bar with cooldowns, lock-on target name and
    /// callouts (perfect dodge, level up, skill combo, XP).
    /// The combo counter is the <see cref="ComboFlameHud"/>. The real Codex-style HUD comes in M4.
    /// </summary>
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] PlayerCombat combat;
        [SerializeField] LockOnTargeter lockOn;
        [SerializeField] Damageable health;
        [SerializeField] PlayerStats stats;
        [SerializeField] SkillCaster caster;
        [SerializeField] Interaction.InteractionDetector interaction;

        static readonly string[] SlotKeys = { "1", "2", "3", "4", "T" };
        string _bigText;
        Color _bigColor;
        float _bigTime;
        int _xpShown;
        float _xpTime;
        int _failSlot = -1;
        float _failTime;

        const float CalloutTime = 1.2f;
        float _callout;
        GUIStyle _small;
        GUIStyle _center;

        void OnEnable()
        {
            if (combat != null) combat.PerfectDodge += OnPerfectDodge;
            if (stats != null)
            {
                stats.LeveledUp += OnLevelUp;
                stats.XpGained += OnXp;
            }
            if (caster != null)
            {
                caster.ComboTriggered += OnSkillCombo;
                caster.CastFailed += OnCastFailed;
            }
        }

        void OnDisable()
        {
            if (combat != null) combat.PerfectDodge -= OnPerfectDodge;
            if (stats != null)
            {
                stats.LeveledUp -= OnLevelUp;
                stats.XpGained -= OnXp;
            }
            if (caster != null)
            {
                caster.ComboTriggered -= OnSkillCombo;
                caster.CastFailed -= OnCastFailed;
            }
        }

        void OnLevelUp(int level) => Big($"LEVEL UP!  Lv {level}", new Color(1f, 0.9f, 0.4f));
        void OnSkillCombo(SkillComboData combo) => Big(combo.displayName.ToUpperInvariant() + "!", new Color(1f, 0.55f, 0.2f));

        void OnXp(int amount)
        {
            _xpShown = _xpTime > 0f ? _xpShown + amount : amount;
            _xpTime = 1.5f;
        }

        void OnCastFailed(int slot, CastResult result)
        {
            if (result == CastResult.EmptySlot) return;
            _failSlot = slot;
            _failTime = 0.4f;
        }

        void Big(string text, Color color)
        {
            _bigText = text;
            _bigColor = color;
            _bigTime = 1.6f;
        }

        void OnPerfectDodge() => _callout = CalloutTime;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _callout = Mathf.Max(_callout - dt, 0f);
            _bigTime = Mathf.Max(_bigTime - dt, 0f);
            _xpTime = Mathf.Max(_xpTime - dt, 0f);
            _failTime = Mathf.Max(_failTime - dt, 0f);
        }

        void OnGUI()
        {
            float s = Screen.height / 1080f;
            _small ??= new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } };
            _center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small.fontSize = Mathf.RoundToInt(20 * s);

            // Bottom left: level + XP, HP, MP, stamina.
            float x = 24 * s, w = 420 * s, y = Screen.height - 150 * s;
            if (stats != null)
            {
                Progression p = stats.Progression;
                int need = p.XpToNext;
                Bar(new Rect(x, y, w, 18 * s), need > 0 ? (float)p.xp / need : 1f, new Color(0.45f, 0.75f, 1f),
                    $"Lv {p.level}   XP {p.xp} / {need}" + (p.freePoints > 0 || p.skillPoints > 0 ? "   [C] points!" : ""));
            }
            if (health != null)
                Bar(new Rect(x, y + 26 * s, w, 26 * s), health.Health.Ratio, new Color(0.85f, 0.2f, 0.25f), $"HP {health.Health.Current} / {health.Health.maxHealth}");
            if (stats != null)
                Bar(new Rect(x, y + 58 * s, w * 0.85f, 18 * s), stats.Mana.Ratio, new Color(0.3f, 0.45f, 1f), $"MP {Mathf.FloorToInt(stats.Mana.Current)} / {Mathf.RoundToInt(stats.Mana.maxMana)}");
            if (player != null)
                Bar(new Rect(x, y + 82 * s, w * 0.7f, 12 * s), player.Stamina.Ratio,
                    player.Stamina.Exhausted ? new Color(0.6f, 0.5f, 0.2f) : new Color(0.95f, 0.8f, 0.25f), null);

            if (stats != null && caster != null && !Aethvirse.Input.Touch.TouchControls.IsActive) DrawSkillBar(s);

            // Top right: day, time, weather.
            World.GameSession session = World.GameSession.Instance;
            _small.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(Screen.width - 420 * s, 12 * s, 400 * s, 28 * s),
                $"Day {session.Clock.Day}  ·  {session.Clock.Format()}  ·  {session.Weather.current}", _small);
            _small.alignment = TextAnchor.MiddleLeft;

            DrawQuestTracker(s);

            // Interaction prompt above the skill bar.
            if (interaction != null && interaction.Current != null)
            {
                _center.fontSize = Mathf.RoundToInt(26 * s);
                _center.normal.textColor = Color.white;
                string key = Aethvirse.Input.Touch.TouchControls.IsActive ? "" : "[E] ";
                GUI.Label(new Rect(0, Screen.height - 150 * s, Screen.width, 40 * s), key + interaction.Current.Prompt, _center);
            }

            if (_bigTime > 0f)
            {
                _center.fontSize = Mathf.RoundToInt(54 * s);
                Color c = _bigColor;
                c.a = Mathf.Clamp01(_bigTime / 0.4f);
                _center.normal.textColor = c;
                GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 80 * s), _bigText, _center);
            }
            if (_xpTime > 0f)
            {
                _small.normal.textColor = new Color(0.6f, 0.85f, 1f, Mathf.Clamp01(_xpTime / 0.4f));
                GUI.Label(new Rect(x + w + 12 * s, y - 4 * s, 200 * s, 26 * s), $"+{_xpShown} XP", _small);
                _small.normal.textColor = Color.white;
            }

            // Top centre: lock-on target.
            ILockable target = lockOn != null ? lockOn.Target : null;
            if (target != null && !(target is Object o && o == null))
            {
                _center.fontSize = Mathf.RoundToInt(24 * s);
                _center.normal.textColor = new Color(1f, 0.45f, 0.45f);
                GUI.Label(new Rect(0, 20 * s, Screen.width, 36 * s), "> " + target.DisplayName + " <", _center);
            }

            if (_callout > 0f)
            {
                _center.fontSize = Mathf.RoundToInt(48 * s);
                _center.normal.textColor = new Color(0.55f, 0.9f, 1f, Mathf.Clamp01(_callout / 0.3f));
                GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 70 * s), Loc.T("perfect_dodge"), _center);
            }
        }

        /// <summary>Bottom centre: 4 skills + ultimate with key, name initials, cooldown sweep and MP tint.</summary>
        void DrawSkillBar(float s)
        {
            float size = 64 * s, gap = 10 * s;
            float total = PlayerStats.SlotCount * size + (PlayerStats.SlotCount - 1) * gap + 14 * s;
            float x0 = (Screen.width - total) * 0.5f, y = Screen.height - size - 24 * s;
            for (int i = 0; i < PlayerStats.SlotCount; i++)
            {
                float x = x0 + i * (size + gap) + (i == PlayerStats.UltimateSlot ? 14 * s : 0f);
                var r = new Rect(x, y, size, size);
                SkillData skill = stats.SlotSkill(i);
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                if (skill != null)
                {
                    bool affordable = stats.Mana.CanPay(skill.mpCost);
                    Color c = skill.iconColor;
                    if (!affordable) c = Color.Lerp(c, Color.gray, 0.7f);
                    GUI.color = c;
                    GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), Texture2D.whiteTexture);
                    float cd = caster.CooldownRatio(skill);
                    if (cd > 0f)
                    {
                        GUI.color = new Color(0f, 0f, 0f, 0.65f);
                        GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, (r.height - 6) * cd), Texture2D.whiteTexture);
                    }
                    if (_failTime > 0f && _failSlot == i)
                    {
                        GUI.color = new Color(1f, 0.1f, 0.1f, 0.5f);
                        GUI.DrawTexture(r, Texture2D.whiteTexture);
                    }
                    GUI.color = Color.white;
                    _center.fontSize = Mathf.RoundToInt(20 * s);
                    _center.normal.textColor = new Color(0.05f, 0.05f, 0.1f);
                    GUI.Label(r, Initials(skill.displayName), _center);
                    if (cd > 0f)
                    {
                        _center.normal.textColor = Color.white;
                        GUI.Label(new Rect(r.x, r.y + r.height * 0.45f, r.width, r.height * 0.55f), Mathf.CeilToInt(caster.CooldownRemaining(skill)).ToString(), _center);
                    }
                }
                GUI.color = Color.white;
                _small.fontSize = Mathf.RoundToInt(15 * s);
                GUI.Label(new Rect(r.x + 4, r.y + 1, 30 * s, 20 * s), SlotKeys[i], _small);
                _small.fontSize = Mathf.RoundToInt(20 * s);
            }
        }

        /// <summary>Right side under the clock: up to 3 active quests with their current objective and progress.</summary>
        void DrawQuestTracker(float s)
        {
            Quests.QuestSystem qs = Quests.QuestSystem.Instance;
            float y = 48 * s, w = 460 * s, x = Screen.width - w - 20 * s;
            int shown = 0;
            _small.alignment = TextAnchor.UpperLeft;
            foreach (Quests.QuestData q in qs.ActiveQuests())
            {
                if (shown++ >= 3) break;
                Quests.QuestObjective o = qs.Log.Current(q, out int index);
                string progress = o != null && o.count > 1 ? $"  {qs.Log.Progress(q.id, index)}/{o.count}" : "";
                QuestLog_Timer(q, qs, out string timer);
                GUI.color = new Color(0f, 0f, 0f, 0.45f);
                GUI.DrawTexture(new Rect(x, y, w, 58 * s), Texture2D.whiteTexture);
                GUI.color = Color.white;
                _small.normal.textColor = q.kind == Quests.QuestKind.Story ? new Color(1f, 0.85f, 0.4f) : q.kind == Quests.QuestKind.Daily ? new Color(0.5f, 0.85f, 1f) : q.kind == Quests.QuestKind.Emergency ? new Color(1f, 0.45f, 0.4f) : new Color(0.6f, 1f, 0.7f);
                GUI.Label(new Rect(x + 8 * s, y + 2 * s, w, 26 * s), q.title + timer, _small);
                _small.normal.textColor = Color.white;
                GUI.Label(new Rect(x + 8 * s, y + 28 * s, w, 26 * s), "· " + (o != null ? o.text : "…") + progress, _small);
                y += 64 * s;
            }
            _small.alignment = TextAnchor.MiddleLeft;
        }

        static void QuestLog_Timer(Quests.QuestData q, Quests.QuestSystem qs, out string timer)
        {
            timer = "";
            if (q.timeLimit <= 0f) return;
            Quests.QuestLog.State st = qs.Log.Get(q.id);
            if (st == null) return;
            float left = Mathf.Max(0f, q.timeLimit - (Time.time - st.startedAt));
            timer = $"  {Mathf.FloorToInt(left / 60f)}:{Mathf.FloorToInt(left) % 60:00}";
        }

        static string Initials(string name)
        {
            string[] words = name.Replace("'", "").Split(' ');
            return words.Length > 1 ? $"{words[0][0]}{words[1][0]}" : name.Substring(0, Mathf.Min(2, name.Length));
        }

        void Bar(Rect r, float ratio, Color fill, string label)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(ratio), r.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (label != null) GUI.Label(new Rect(r.x + 8, r.y, r.width, r.height), label, _small);
        }

    }
}
