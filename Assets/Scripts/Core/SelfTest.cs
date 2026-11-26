using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aethvirse.Combat;
using Aethvirse.Items;
using Aethvirse.Player;
using Aethvirse.Skills;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Core
{
    /// <summary>
    /// Development sweep: <c>-selftest &lt;folder&gt;</c> plays through every system in a real build — all four classes
    /// with every skill, every job line (summons, shadows), every secret skill, reactions, every item (equip, use, gems,
    /// enchants, reforge, runes, mystery boxes), save → load, every scene (regions, Gate, [ ??? ], penalty zone), the
    /// duel ring, a cipher lock, chests — and records every error or exception with its stack plus screenshots.
    /// The goal is a run with zero errors.
    /// </summary>
    public sealed class SelfTest : MonoBehaviour
    {
        string _dir;
        readonly StringBuilder _log = new StringBuilder();
        readonly Dictionary<string, int> _errors = new Dictionary<string, int>();
        readonly List<string> _errorOrder = new List<string>();
        string _step = "boot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-selftest");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("SelfTest");
            DontDestroyOnLoad(go);
            var t = go.AddComponent<SelfTest>();
            t._dir = args[i + 1];
            Directory.CreateDirectory(t._dir);
            Application.logMessageReceived += t.OnLog;
            t.StartCoroutine(t.Run());
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string key = message + "\n" + stack;
            if (_errors.TryGetValue(key, out int n)) { _errors[key] = n + 1; return; }
            _errors[key] = 1;
            _errorOrder.Add($"[{_step}] {type}: {message}\n{stack}");
        }

        void Note(string line)
        {
            _log.AppendLine($"[{Time.realtimeSinceStartup:0.0}s] {_step}: {line}");
            Flush();
        }

        void Flush()
        {
            File.WriteAllText(Path.Combine(_dir, "log.txt"), _log.ToString());
            var e = new StringBuilder();
            e.AppendLine($"{_errorOrder.Count} distinct errors");
            foreach (string s in _errorOrder) e.AppendLine(s).AppendLine("----");
            File.WriteAllText(Path.Combine(_dir, "errors.txt"), e.ToString());
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
        }

        static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        PlayerStats Stats => PlayerStats.Current;
        SkillCaster Caster => Stats != null ? Stats.GetComponent<SkillCaster>() : null;

        void Immortal()
        {
            var d = Stats != null ? Stats.GetComponent<Damageable>() : null;
            if (d != null) d.Health.immortal = true;
        }

        IEnumerator NewGame(string classId)
        {
            GameSession.Instance.ClassId = classId;
            UI.TitleMenu.NewGame();
            yield return Wait(4f);
            Immortal();
            GameSession.Instance.AcademyPoints = 100000;
        }

        /// <summary>Casts every active skill the hero has (in all slots), waiting for each to finish.</summary>
        IEnumerator CastAll(string label)
        {
            if (Stats == null || Caster == null) { Note("no player"); yield break; }
            int cast = 0, failed = 0;
            foreach (SkillData s in new List<SkillData>(Stats.AllSkills()))
            {
                if (s == null || s.IsPassive || Stats.SkillLevel(s.id) <= 0) continue;
                int slot = s.isUltimate ? PlayerStats.UltimateSlot : 0;
                if (!Stats.AssignSlot(s.id, slot)) { failed++; Note($"cannot slot {s.id}"); continue; }
                Caster.ResetCooldowns();
                Stats.Recompute(refill: true);
                CastResult r = Caster.TryCastSlot(slot);
                if (r == CastResult.Ok) cast++; else { failed++; Note($"{s.id}: {r}"); }
                yield return Wait(s.IsDamaging ? 1.1f : 0.3f);
            }
            Note($"{label}: cast {cast}, failed {failed}");
        }

        void SpawnTargets()
        {
            Transform p = PlayerTarget.Current;
            if (p == null || Enemies.EnemyCatalog.Instance == null) return;
            for (int i = 0; i < 3; i++)
            {
                Enemies.Enemy e = Enemies.EnemyCatalog.Instance.Spawn("ash_hound", p.position + p.forward * (3f + i) + Vector3.right * (i - 1) * 2f, 5, 0f, false);
                if (e != null) e.RespawnAtHome = false;
            }
        }

        static bool Only(string part)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-only");
            return i < 0 || i + 1 >= args.Length || args[i + 1] == part;
        }

        /// <summary>Where the camera and the player are, and what the scene holds (for blank-screen bugs).</summary>
        void Probe(string label)
        {
            Camera cam = Camera.main;
            Transform p = PlayerTarget.Current;
            int renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            Note($"{label}: camera {(cam != null ? cam.transform.position.ToString("0.0") + " fwd " + cam.transform.forward.ToString("0.00") + " near " + cam.nearClipPlane + " far " + cam.farClipPlane + " enabled " + cam.enabled : "none")} | player {(p != null ? p.position.ToString("0.0") : "none")} | renderers {renderers} | fog {RenderSettings.fog} {RenderSettings.fogDensity:0.000}");
        }

        IEnumerator Run()
        {
            yield return Wait(4f);
            if (!Only("all"))
            {
                yield return NewGame("vanguard");
                _step = "gate_only";
                Gates.GateSession.BeginTrial("guardian", 1, 60, new[] { "ash_hound", "ember_husk" });
                Save.SceneTravel.Go("Gate", null, rememberReturn: true);
                for (int k = 0; k < 4; k++)
                {
                    yield return Wait(1.5f);
                    Probe($"t{k}");
                    yield return Shot($"gate_{k}");
                }
                _log.AppendLine("done");
                Flush();
                Application.Quit();
                yield break;
            }
            _step = "title";
            yield return Shot("00_title");

            string[] classes = { "vanguard", "phantom", "arcanist", "ranger" };
            string[][] lines =
            {
                new[] { "guardian", "bastion" }, new[] { "shadowblade", "nightreaper" },
                new[] { "elementalist", "archmage" }, new[] { "sharpshooter", "deadeye" },
            };
            for (int c = 0; c < classes.Length; c++)
            {
                _step = "class_" + classes[c];
                yield return NewGame(classes[c]);
                Note($"scene {SceneManager.GetActiveScene().name}, class {Stats?.Class?.id}");
                Stats.AddXp(50_000_000);
                Stats.Recompute(refill: true);
                Note($"level {Stats.Progression.level}");
                SpawnTargets();
                yield return CastAll("class skills");
                yield return Shot($"01_{classes[c]}_skills");
                foreach (string job in lines[c])
                {
                    _step = $"job_{job}";
                    Note($"change job: {Stats.ChangeJob(job)}");
                    yield return CastAll("job skills");
                }
            }

            _step = "summoner_shadow";
            yield return NewGame("vanguard");
            Stats.AddXp(50_000_000);
            foreach (string f in new[] { "sealed_necromancer", "codex_umbra_read", "echo_king_answered" }) GameSession.Instance.Flags.SetFlag(f);
            foreach (string job in new[] { "summoner", "necromancer", "shadow_sovereign" })
            {
                Note($"change job {job}: {Stats.ChangeJob(job)}");
                SpawnTargets();
                yield return Wait(0.5f);
                yield return CastAll(job);
            }
            yield return Shot("02_shadows");

            _step = "secret_skills";
            var db = SecretSkillDatabase.Instance;
            foreach (SkillData s in db.skills) Stats.LearnSecretSkill(s.id);
            SpawnTargets();
            yield return CastAll("secret skills");
            yield return Shot("03_secret_skills");

            _step = "reactions";
            {
                var target = Enemies.EnemyCatalog.Instance.Spawn("ash_hound", PlayerTarget.Current.position + PlayerTarget.Current.forward * 4f, 5, 0f, false);
                var d = target.GetComponent<Damageable>();
                d.Health.immortal = true;
                ElementType[] seq = { ElementType.Fire, ElementType.Ice, ElementType.Wind, ElementType.Fire, ElementType.Lightning, ElementType.Ice, ElementType.Earth, ElementType.Shadow, ElementType.Light, ElementType.Void, ElementType.Ice, ElementType.Ice, ElementType.Ice };
                foreach (ElementType e in seq)
                {
                    d.Receive(new DamageInfo { amount = 10, element = e, hitPosition = d.transform.position });
                    yield return Wait(0.1f);
                }
                d.Receive(new DamageInfo { amount = 10, element = ElementType.Physical, isHeavy = true });
                Note($"status disabled {d.Status.Disabled}");
            }

            _step = "items";
            {
                PlayerInventory inv = PlayerInventory.Current;
                inv.AddGold(10_000_000);
                int equipped = 0, used = 0;
                foreach (ItemData item in new List<ItemData>(inv.Database.items))
                {
                    if (item == null) continue;
                    inv.Bag.stacks.Clear();
                    inv.Bag.Add(item.id, 3);
                    ItemStack st = inv.Bag.stacks.Count > 0 ? inv.Bag.stacks[0] : null;
                    if (st == null) continue;
                    if (item.IsEquipment && inv.Equip(st)) equipped++;
                    else if (item.IsUsable && inv.Use(st)) used++;
                }
                Note($"equipped {equipped}, used {used} of {inv.Database.items.Count}");
                // Generated gear: gems, enchants, reforge.
                inv.Bag.stacks.Clear();
                var rng = new System.Random(1);
                ItemStack gear = new EquipmentGenerator(inv.Database, inv.Rules, rng).Generate(200, 5, EquipSlot.Weapon);
                inv.Bag.AddStack(gear);
                inv.Bag.Add("gem_ruby_4", 3);
                inv.Bag.Add("scroll_kindling", 1);
                inv.Bag.Add("scrap_metal", 10);
                Note($"socket {ItemActions.SocketGem(inv.Bag, gear, "gem_ruby_4")}, enchant {ItemActions.Enchant(inv.Bag, gear, inv.Database.Get(gear.itemId), "scroll_kindling")}, reforge {ItemActions.Reforge(inv.Bag, gear, inv.Rules, inv, rng)}, equip {inv.Equip(gear)}");
                inv.Bag.Add("rune_hydra", 1);
                SkillData proj = Stats.AllSkills().Find(s => s.kind == SkillKind.Projectile && Stats.SkillLevel(s.id) > 0);
                if (proj != null) Note($"rune socket {Stats.SocketRune(proj.id, "rune_hydra")} on {proj.id}");
                for (int i = 0; i < 5; i++) { inv.Bag.Add("mystery_box", 1); inv.Use(inv.Bag.stacks.Find(x => x.itemId == "mystery_box")); }
                Stats.Recompute(refill: true);
                yield return CastAll("with gear + rune");
                UI.InventoryPanel.Toggle();
                yield return Wait(0.5f);
                yield return Shot("04_bag");
                UI.InventoryPanel.Toggle();
            }

            _step = "save_load";
            Note($"save {Save.SaveManager.Save(1)}");
            yield return Wait(0.5f);
            Note($"load {Save.SaveManager.Load(1)}");
            yield return Wait(5f);
            Immortal();
            Note($"after load: scene {SceneManager.GetActiveScene().name}, level {Stats?.Progression.level}, job {Stats?.JobId}, secret skills {Stats?.SecretSkills.Count}");

            _step = "scenes";
            var atlas = WorldAtlas.Instance;
            var scenes = new List<string>();
            foreach (RegionData r in atlas.AllRegions()) if (!string.IsNullOrEmpty(r.scene)) scenes.Add(r.scene);
            scenes.Add(Endgame.HollowGate.UnknownScene);
            foreach (string sc in scenes)
            {
                _step = "scene_" + sc;
                Save.SceneTravel.Go(sc, null);
                yield return Wait(4f);
                Immortal();
                Note($"in {SceneManager.GetActiveScene().name}, region {RegionController.Current?.id}, players {FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length}");
                yield return Shot("05_" + sc);
            }

            _step = "gate";
            Gates.GateSession.BeginTrial("guardian", 1, 60, new[] { "ash_hound", "ember_husk" });
            Save.SceneTravel.Go("Gate", null, rememberReturn: true);
            yield return Wait(5f);
            Immortal();
            Note($"in {SceneManager.GetActiveScene().name}, enemies {FindObjectsByType<Enemies.Enemy>(FindObjectsSortMode.None).Length}");
            Probe("gate");
            yield return Shot("06_gate");
            Gates.GateSession.End();

            _step = "penalty";
            Save.SceneTravel.Go("PenaltyZone", null);
            yield return Wait(4f);
            yield return Shot("07_penalty");

            _step = "academy_props";
            Save.SceneTravel.Go("Academy", null);
            yield return Wait(4f);
            Immortal();
            foreach (var ring in FindObjectsByType<Interaction.DuelRing>(FindObjectsSortMode.None))
            {
                ring.GetComponent<Interaction.Interactable>().Use(PlayerTarget.Current.gameObject);
                yield return Wait(6f);
                yield return Shot("08_duel");
            }

            _step = "ashfall_props";
            Save.SceneTravel.Go("R1_Ashfall", null);
            yield return Wait(4f);
            Immortal();
            foreach (var ch in FindObjectsByType<Chest>(FindObjectsSortMode.None)) ch.GetComponent<Interaction.Interactable>().Use(PlayerTarget.Current.gameObject);
            var lk = FindFirstObjectByType<Secrets.CipherLock>();
            if (lk != null)
            {
                lk.GetComponent<Interaction.Interactable>().Use(PlayerTarget.Current.gameObject);
                yield return Wait(0.5f);
                yield return Shot("09_cipher");
            }

            _step = "done";
            Note($"finished with {_errorOrder.Count} distinct errors");
            _log.AppendLine("done");
            Flush();
            Application.Quit();
        }
    }
}
