using System.Collections.Generic;
using Aethvirse.Items;
using Aethvirse.Player;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Core
{
    /// <summary>
    /// Achievements and titles (§8 title system): counters kept in the saved world flags, each achievement unlocks a
    /// title; one title can be worn for a small passive bonus. Definitions are data (pure, tested).
    /// </summary>
    public sealed class Achievements : MonoBehaviour
    {
        public struct Def
        {
            public string id, name, desc, counter, title, stat;
            public int need;
            public float value;
        }

        public static readonly Def[] All =
        {
            D("first_blood", "First Blood", "Defeat your first monster.", "kills", 1, "Fresh Blade", StatKeys.AtkPct, 0.01f),
            D("hunter_100", "Hunter", "Defeat 100 monsters.", "kills", 100, "Hunter", StatKeys.AtkPct, 0.02f),
            D("hunter_1000", "Exterminator", "Defeat 1,000 monsters.", "kills", 1000, "Exterminator", StatKeys.AtkPct, 0.04f),
            D("hunter_10000", "Calamity", "Defeat 10,000 monsters.", "kills", 10000, "Walking Calamity", StatKeys.AtkPct, 0.07f),
            D("boss_1", "Giant Slayer", "Defeat a world guardian.", "bosses", 1, "Giant Slayer", StatKeys.CritDmg, 0.1f),
            D("boss_4", "Godkiller", "Defeat every guardian and the God.", "bosses", 4, "Godkiller", StatKeys.AtkPct, 0.08f),
            D("mimic_1", "That Chest Bit Me", "Defeat a mimic.", "mimics", 1, "Suspicious of Chests", StatKeys.DefPct, 0.02f),
            D("chests_50", "Treasure Seeker", "Open 50 chests.", "chests", 50, "Treasure Seeker", StatKeys.Sen, 5f),
            D("reaction_1", "Chemistry", "Trigger an elemental reaction.", "reactions", 1, "Alchemist of Ruin", StatKeys.AtkPct, 0.01f),
            D("reaction_500", "Elemental Scholar", "Trigger 500 reactions.", "reactions", 500, "Elemental Scholar", StatKeys.Cdr, 0.04f),
            D("dodge_50", "Untouchable", "Make 50 perfect dodges.", "perfect_dodges", 50, "Untouchable", StatKeys.Mspd, 0.04f),
            D("dodge_500", "Ghost", "Make 500 perfect dodges.", "perfect_dodges", 500, "Ghost", StatKeys.Aspd, 0.05f),
            D("combo_50", "Ember Heart", "Reach a 50-hit combo.", "combo_best", 50, "Ember Heart", StatKeys.Aspd, 0.02f),
            D("combo_200", "Phoenix", "Reach a 200-hit combo.", "combo_best", 200, "Phoenix Rising", StatKeys.CritDmg, 0.2f),
            D("skills_500", "Spellslinger", "Use skills 500 times.", "skills", 500, "Spellslinger", StatKeys.Cdr, 0.03f),
            D("secret_1", "Something Is There", "Find your first secret.", "secrets", 1, "Curious", StatKeys.Sen, 2f),
            D("secret_30", "Page Turner", "Find 30 secrets.", "secrets", 30, "Page Turner", StatKeys.Sen, 6f),
            D("secret_126", "The Whole Book", "Find every secret of every world.", "secrets", 126, "Reader of the Codex", StatKeys.AtkPct, 0.1f),
            D("cipher_1", "Codebreaker", "Open a cipher lock.", "ciphers", 1, "Codebreaker", StatKeys.Int, 4f),
            D("cipher_14", "Cryptographer", "Open every cipher lock.", "ciphers", 14, "Cryptographer", StatKeys.Cdr, 0.06f),
            D("sealed_1", "[ ??? ]", "Complete a sealed mission.", "sealed", 1, "Unannounced", StatKeys.Crit, 0.01f),
            D("sealed_14", "Every Seal", "Complete every sealed mission.", "sealed", 14, "The One Who Read Everything", StatKeys.CritDmg, 0.35f),
            D("duel_1", "Rivalry", "Win a duel in the academy ring.", "duels", 1, "Rival", StatKeys.AtkPct, 0.02f),
            D("duel_4", "Your Own Name", "Defeat the fourth name on the board.", "duels", 4, "Beyond Yourself", StatKeys.HpPct, 0.08f),
            D("gate_10", "Gate Breaker", "Clear 10 Gates.", "gates", 10, "Gate Breaker", StatKeys.AtkPct, 0.03f),
            D("gate_100", "Gate Sovereign", "Clear 100 Gates.", "gates", 100, "Gate Sovereign", StatKeys.DefPct, 0.06f),
            D("level_50", "Rising", "Reach level 50.", "level", 50, "Rising Vanguard", StatKeys.HpPct, 0.03f),
            D("level_100", "Centurion", "Reach level 100.", "level", 100, "Centurion", StatKeys.HpPct, 0.05f),
            D("level_300", "Beyond the Map", "Reach level 300.", "level", 300, "Unbound", StatKeys.AtkPct, 0.06f),
            D("job_1", "A New Name", "Change jobs.", "jobs", 1, "Reborn", StatKeys.Vit, 4f),
            D("regions_7", "Cartographer", "Walk every region of Aethvirse.", "regions", 7, "Cartographer", StatKeys.Mspd, 0.05f),
            D("regions_14", "Star Walker", "Walk every region of every world.", "regions", 14, "Star Walker", StatKeys.Mspd, 0.08f),
            D("rich", "Hoard", "Hold 1,000,000 gold.", "gold_max", 1000000, "Gold Dragon", StatKeys.Lifesteal, 0.01f),
        };

        static Def D(string id, string name, string desc, string counter, int need, string title, string stat, float value) =>
            new Def { id = id, name = name, desc = desc, counter = counter, need = need, title = title, stat = stat, value = value };

        public static string Flag(string id) => "ach_" + id;
        const string EquippedCounter = "title_equipped";

        /// <summary>Which achievements a counter value newly reaches (pure; tested).</summary>
        public static List<Def> Reached(string counter, int value, System.Func<string, bool> has)
        {
            var list = new List<Def>();
            foreach (Def d in All) if (d.counter == counter && value >= d.need && !has(Flag(d.id))) list.Add(d);
            return list;
        }

        static readonly string[] BossIds = { "the_unwritten", "the_conductor", "kethra_warden", "the_god" };
        static Achievements _instance;
        float _poll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("Achievements");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Achievements>();
        }

        void OnEnable()
        {
            GameEvents.EnemyKilled += OnKill;
            GameEvents.SkillUsed += _ => Count("skills", 1);
            GameEvents.GateCleared += _ => Count("gates", 1);
            GameEvents.AreaEntered += OnArea;
            Combat.Damageable.AnyReaction += _ => Count("reactions", 1);
        }

        void Start()
        {
            GameSession.Instance.FlagSet += OnFlag;
            if (Quests.QuestSystem.Instance != null)
                Quests.QuestSystem.Instance.Completed += q => { if (q.kind == Quests.QuestKind.Sealed) Count("sealed", 1); };
        }

        void OnKill(string id)
        {
            Count("kills", 1);
            if (id == "mimic") Count("mimics", 1);
            if (System.Array.IndexOf(BossIds, id) >= 0) Count("bosses", 1);
        }

        void OnArea(string id)
        {
            if (WorldAtlas.Instance == null || WorldAtlas.Instance.Region(id) == null) return;
            WorldFlags f = GameSession.Instance.Flags;
            if (f.Has("visited_" + id)) return;
            f.SetFlag("visited_" + id);
            Count("regions", 1);
        }

        void OnFlag(string flag)
        {
            if (flag.StartsWith("secret_")) Count("secrets", 1);
            else if (flag.StartsWith("cipher_")) Count("ciphers", 1);
            else if (flag.StartsWith("duel_") && flag.EndsWith("_won")) Count("duels", 1);
            else if (flag.StartsWith("job_")) Count("jobs", 1);
        }

        public static void Count(string counter, int amount)
        {
            if (!GameSession.Exists) return;
            WorldFlags f = GameSession.Instance.Flags;
            Check(counter, f.Add(counter, amount));
        }

        /// <summary>Keeps a "best so far" counter (combo, level, gold).</summary>
        static void Best(string counter, int value)
        {
            WorldFlags f = GameSession.Instance.Flags;
            int now = f.Counter(counter);
            if (value <= now) return;
            f.Add(counter, value - now);
            Check(counter, value);
        }

        static void Check(string counter, int value)
        {
            WorldFlags f = GameSession.Instance.Flags;
            foreach (Def d in Reached(counter, value, f.Has))
            {
                f.SetFlag(Flag(d.id));
                Codex.Notify($"[ ACHIEVEMENT ]  {d.name}\nTitle unlocked: {d.title}", CodexTone.Reward);
            }
        }

        void Update()
        {
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f || !GameSession.Exists) return;
            _poll = 1f;
            PlayerStats ps = PlayerStats.Current;
            if (ps == null) return;
            Best("level", ps.Progression.level);
            Best("gold_max", GameSession.Instance.Gold);
            var combat = ps.GetComponent<PlayerCombat>();
            if (combat != null) Best("combo_best", combat.Tracker.Best);
        }

        // --- Titles -----------------------------------------------------------------------------------

        public static bool Unlocked(Def d) => GameSession.Exists && GameSession.Instance.Flags.Has(Flag(d.id));

        /// <summary>Index into <see cref="All"/> of the worn title, or -1.</summary>
        public static int Equipped => GameSession.Exists ? GameSession.Instance.Flags.Counter(EquippedCounter) - 1 : -1;

        public static void Equip(int index)
        {
            WorldFlags f = GameSession.Instance.Flags;
            f.Add(EquippedCounter, index + 1 - f.Counter(EquippedCounter));
            PlayerStats.Current?.Recompute(refill: false);
        }

        /// <summary>The worn title's bonus to a stat key (0 if none).</summary>
        public static float TitleBonus(string stat)
        {
            int i = Equipped;
            return i >= 0 && i < All.Length && All[i].stat == stat && Unlocked(All[i]) ? All[i].value : 0f;
        }

        public static string TitleName => Equipped >= 0 && Equipped < All.Length && Unlocked(All[Equipped]) ? All[Equipped].title : "";
    }
}
