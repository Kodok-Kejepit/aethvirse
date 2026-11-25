using System.Collections.Generic;
using Aethvirse.Input;
using Aethvirse.Skills;
using Aethvirse.Stats;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Graybox status window (C / gamepad Select / touch "C"): spend stat points (+ on STR … SEN),
    /// level skills with skill points and put actives on slots 1–4. The Codex UI replaces it in M4.
    /// </summary>
    public sealed class StatusPanel : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;

        static StatusPanel _instance;
        bool _open;
        CursorLockMode _cursorBefore;
        Vector2 _scroll;
        GUIStyle _title, _text, _button;

        public static bool IsOpen => _instance != null && _instance._open;

        /// <summary>Touch button / other UI.</summary>
        public static void Toggle()
        {
            if (_instance != null) _instance.SetOpen(!_instance._open);
        }

        void Awake() => _instance = this;

        void Update()
        {
            if (GameInput.Instance.StatusPanel.WasPressedThisFrame()) SetOpen(!_open);
            if (_open && GameInput.Instance.ReleaseCursor.WasPressedThisFrame()) SetOpen(false);
        }

        void SetOpen(bool open)
        {
            if (open == _open) return;
            _open = open;
            // Clicking + buttons needs a free cursor; restore capture afterwards.
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

        /// <summary>Job (§8): current job, jobs on offer (trial Gate, hidden jobs at once), shadow army.</summary>
        void DrawJobs(float s)
        {
            GUILayout.Space(14 * s);
            GUILayout.Label("Job: " + (stats.CurrentJob != null ? $"{stats.CurrentJob.displayName} (tier {stats.CurrentJob.tier})" : Loc.T("none_yet")), _title);
            var reg = Jobs.JobRegistry.Instance;
            if (reg != null)
            {
                var options = reg.Options(stats.Class.id, stats.JobId, stats.Progression.level, World.GameSession.Instance.Flags.Has);
                if (options.Count == 0)
                {
                    int next = stats.CurrentJob == null ? 30 : 60;
                    GUILayout.Label($"{Loc.T("next_job")} {next}.", _text);
                }
                foreach (Jobs.JobData j in options)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{j.displayName} — {j.description}", _text, GUILayout.Width(700 * s));
                    string label = j.hidden ? "Accept" : "Take the trial";
                    if (GUILayout.Button(label, _button, GUILayout.Width(220 * s), GUILayout.Height(44 * s)))
                    {
                        if (j.hidden) stats.ChangeJob(j.id);
                        else
                        {
                            World.RegionData r = World.RegionController.Current;
                            string[] types = r != null && r.enemies.Count > 0 ? r.enemies.ToArray() : new[] { "ash_hound", "ember_husk" };
                            Gates.GateSession.BeginTrial(j.id, j.tier, stats.Progression.level, types);
                            SetOpen(false);
                            Save.SceneTravel.Go(World.GateSpot.GateScene, null, rememberReturn: true);
                            return;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (stats.Shadows.soldiers.Count > 0)
            {
                GUILayout.Label($"{Loc.T("shadow_army")} ({stats.Shadows.soldiers.Count})", _title);
                foreach (Jobs.ShadowArmy.Soldier sol in stats.Shadows.soldiers)
                    GUILayout.Label($"  {sol.enemyType}  Lv {sol.level}  · {Jobs.ShadowArmy.RankNames[sol.rank]} ({sol.xp} xp)", _text);
            }
        }

        /// <summary>Mystery Journal (§9.5): one page per region; found ✦, hinted ◇ (hint text), unknown ✕ — never answers.</summary>
        void DrawJournal(float s)
        {
            var db = Secrets.SecretDatabase.Instance;
            var atlas = World.WorldAtlas.Instance;
            if (db == null || atlas == null) return;
            var state = Secrets.SecretSystem.Instance.State;
            GUILayout.Space(14 * s);
            GUILayout.Label(Loc.T("journal"), _title);
            foreach (World.RegionData r in atlas.AllRegions())
            {
                var (found, total) = db.Progress(r.id, state);
                if (total == 0) continue;
                bool visited = World.GameSession.Instance.Flags.Has(World.Waystone.Flag(r.id + "_gate")) || found > 0;
                if (!visited) continue;
                GUILayout.Label($"{r.displayName} — {Loc.T("lines_revealed")} {found}/{total}", _text);
                var page = db.PageFor(r.id);
                if (page != null) GUILayout.Label("   " + page.synopsis, _text);
                foreach (Secrets.SecretData sec in db.InRegion(r.id))
                {
                    if (state.IsFound(sec.id)) { GUILayout.Label("   [*] " + sec.title, _text); continue; }
                    bool any = false;
                    for (int i = 0; i < sec.hints.Count; i++)
                        if (state.HintSeen(sec.id, i)) { GUILayout.Label("   [?] " + sec.hints[i], _text); any = true; }
                    if (!any) GUILayout.Label("   [ ] ???", _text);
                }
            }
        }

        void OnGUI()
        {
            if (!_open || stats == null) return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _text ??= new GUIStyle(GUI.skin.label);
            _button ??= new GUIStyle(GUI.skin.button);
            _title.fontSize = Mathf.RoundToInt(30 * s);
            _text.fontSize = Mathf.RoundToInt(22 * s);
            _button.fontSize = Mathf.RoundToInt(22 * s);

            float w = 980 * s, h = 860 * s;
            var area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.color = new Color(0.02f, 0.06f, 0.14f, 0.92f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(area.x + 24 * s, area.y + 16 * s, w - 48 * s, h - 32 * s));
            _scroll = GUILayout.BeginScrollView(_scroll);

            Progression p = stats.Progression;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{stats.Class.displayName}   Lv {p.level}", _title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Loc.T("close") + " (C)", _button, GUILayout.Width(170 * s), GUILayout.Height(48 * s))) SetOpen(false);
            GUILayout.EndHorizontal();
            GUILayout.Label($"XP {p.xp} / {p.XpToNext}      {Loc.T("stat_points")}: {p.freePoints}      {Loc.T("skill_points")}: {p.skillPoints}", _text);
            GUILayout.Space(10 * s);

            DerivedStats d = stats.Derived;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(w * 0.45f));
            foreach (Stat stat in StatBlock.All)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{stat}  {Mathf.RoundToInt(stats.Primary[stat])}", _text, GUILayout.Width(220 * s));
                GUI.enabled = p.freePoints > 0;
                if (GUILayout.Button("+", _button, GUILayout.Width(64 * s), GUILayout.Height(44 * s))) stats.Allocate(stat);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label($"HP {d.maxHp}    MP {d.maxMp}", _text);
            GUILayout.Label($"ATK {d.attack:0}    DEF {d.defense:0}", _text);
            GUILayout.Label($"CRIT {d.critChance * 100f:0.0}%  ×{d.critDamage:0.00}", _text);
            GUILayout.Label($"ASPD {d.attackSpeed:0.00}    MSPD {d.moveSpeed:0.00}    CDR {d.cooldownReduction * 100f:0}%", _text);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            DrawJobs(s);
            DrawJournal(s);

            GUILayout.Space(14 * s);
            GUILayout.Label(Loc.T("skills"), _title);
            foreach (SkillData skill in stats.AllSkills())
            {
                int level = stats.SkillLevel(skill.id);
                GUILayout.BeginHorizontal();
                string state = level > 0 ? $"Lv {level}" : $"unlocks at Lv {skill.unlockLevel}";
                string tag = skill.IsPassive ? "passive" : skill.isUltimate ? "ULT" : $"{stats.MpCostWithRune(skill)} MP · {skill.cooldown:0}s";
                GUI.color = level > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                GUILayout.Label($"{skill.displayName}  ({tag})  {state}", _text, GUILayout.Width(560 * s));
                GUI.color = Color.white;
                GUI.enabled = level > 0 && level < skill.maxLevel && p.skillPoints > 0;
                if (GUILayout.Button("+", _button, GUILayout.Width(56 * s), GUILayout.Height(40 * s))) stats.LevelUpSkill(skill.id);
                GUI.enabled = level > 0 && !skill.IsPassive && !skill.isUltimate;
                for (int slot = 0; slot < PlayerStats.UltimateSlot; slot++)
                {
                    bool here = stats.Slots[slot] == skill.id;
                    if (GUILayout.Toggle(here, (slot + 1).ToString(), _button, GUILayout.Width(44 * s), GUILayout.Height(40 * s)) && !here)
                        stats.AssignSlot(skill.id, slot);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Label("   " + skill.description, _text);
                if (level > 0) DrawRune(stats, skill, s);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>Rune socket row: shows the rune and cycles through fitting runes in the bag (or takes it out).</summary>
        void DrawRune(PlayerStats stats, SkillData skill, float s)
        {
            var inv = Items.PlayerInventory.Current;
            var db = SecretSkillDatabase.Instance;
            if (inv == null || db == null) return;
            var fitting = new List<string>();
            foreach (Items.ItemStack st in inv.Bag.stacks)
            {
                Items.ItemData item = inv.Database.Get(st.itemId);
                if (item == null || item.kind != Items.ItemKind.Rune || fitting.Contains(item.id)) continue;
                SkillRuneData r = db.Rune(item.runeId);
                if (r != null && r.Fits(skill)) fitting.Add(item.id);
            }
            SkillRuneData on = stats.RuneFor(skill);
            if (on == null && fitting.Count == 0) return;
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.6f, 0.85f, 1f);
            GUILayout.Label(on != null ? $"   {Loc.T("rune")}: {on.displayName} — {on.description}" : $"   {Loc.T("rune")}: —", _text, GUILayout.Width(760 * s));
            GUI.color = Color.white;
            if (fitting.Count > 0 && GUILayout.Button(Loc.T("socket"), _button, GUILayout.Width(150 * s), GUILayout.Height(38 * s)))
            {
                // Next fitting rune after the current one.
                string current = stats.RuneItemOn(skill.id);
                int k = fitting.IndexOf(current);
                stats.SocketRune(skill.id, fitting[(k + 1) % fitting.Count]);
            }
            if (on != null && GUILayout.Button(Loc.T("unsocket"), _button, GUILayout.Width(150 * s), GUILayout.Height(38 * s))) stats.SocketRune(skill.id, "");
            GUILayout.EndHorizontal();
        }
    }
}
