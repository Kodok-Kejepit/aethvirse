using System;
using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Player;
using Aethvirse.Skills;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>
    /// Player stats (port of the Godot StatsComponent): progression + class + passives + buffs →
    /// DerivedStats → health, mana, combat numbers and movement. Owns XP intake, stat points,
    /// skill levels and the skill bar slots.
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        public const int SlotCount = 5;
        public const int UltimateSlot = 4;

        [SerializeField] ClassData classData;
        [SerializeField] StatTuning tuning;
        [SerializeField] PlayerController player;
        [SerializeField] PlayerCombat combat;
        [SerializeField] Damageable damageable;
        [SerializeField] Items.PlayerInventory inventory;

        readonly Dictionary<string, int> _skillLevels = new Dictionary<string, int>();
        readonly BuffSet _buffs = new BuffSet();

        public ClassData Class => classData;
        public StatTuning Tuning => tuning;
        public Progression Progression { get; private set; }
        public StatBlock Primary { get; private set; }
        public DerivedStats Derived { get; private set; }
        public ManaPool Mana { get; } = new ManaPool();
        public string[] Slots { get; } = new string[SlotCount];
        public BuffSet Buffs => _buffs;
        /// <summary>Bound shadow soldiers (shadow job line; saved).</summary>
        public global::Aethvirse.Jobs.ShadowArmy Shadows { get; private set; } = new global::Aethvirse.Jobs.ShadowArmy();
        /// <summary>Current job id ("" = still the base class) and its lineage.</summary>
        public string JobId { get; private set; } = "";
        public List<global::Aethvirse.Jobs.JobData> Jobs { get; private set; } = new List<global::Aethvirse.Jobs.JobData>();
        public global::Aethvirse.Jobs.JobData CurrentJob => Jobs.Count > 0 ? Jobs[Jobs.Count - 1] : null;
        public event Action<global::Aethvirse.Jobs.JobData> JobChanged;

        /// <summary>Class skills followed by the skills of every job taken so far.</summary>
        public List<SkillData> AllSkills()
        {
            var list = new List<SkillData>(classData.skills);
            foreach (global::Aethvirse.Jobs.JobData j in Jobs) list.AddRange(j.skills);
            var db = SecretSkillDatabase.Instance;
            if (db != null) foreach (string id in SecretSkills) { SkillData s = db.Skill(id); if (s != null) list.Add(s); }
            return list;
        }

        /// <summary>Secret skills learned from skill books (any class).</summary>
        public List<string> SecretSkills { get; private set; } = new List<string>();
        /// <summary>skill id → rune item id.</summary>
        readonly Dictionary<string, string> _runes = new Dictionary<string, string>();
        public event Action RunesChanged;

        /// <summary>Learns a secret skill from a book (level 1). False if already known or unknown.</summary>
        public bool LearnSecretSkill(string id)
        {
            var db = SecretSkillDatabase.Instance;
            SkillData s = db != null ? db.Skill(id) : null;
            if (s == null || SecretSkills.Contains(id)) return false;
            SecretSkills.Add(id);
            _skillLevels[id] = 1;
            SkillLearned?.Invoke(s);
            Recompute(refill: false);
            return true;
        }

        public string RuneItemOn(string skillId) => _runes.TryGetValue(skillId, out string r) ? r : "";

        /// <summary>The rune socketed in a skill (null = none).</summary>
        public SkillRuneData RuneFor(SkillData skill)
        {
            if (skill == null || !_runes.TryGetValue(skill.id, out string itemId) || inventory == null) return null;
            Items.ItemData item = inventory.Database.Get(itemId);
            var db = SecretSkillDatabase.Instance;
            return item != null && db != null ? db.Rune(item.runeId) : null;
        }

        /// <summary>Sockets a rune item from the bag (the old one goes back to the bag). Empty id = take it out.</summary>
        public bool SocketRune(string skillId, string runeItemId)
        {
            if (inventory == null) return false;
            string old = RuneItemOn(skillId);
            if (!string.IsNullOrEmpty(runeItemId))
            {
                if (!inventory.Bag.Remove(runeItemId, 1)) return false;
                _runes[skillId] = runeItemId;
            }
            else _runes.Remove(skillId);
            if (!string.IsNullOrEmpty(old)) inventory.Bag.Add(old, 1);
            RunesChanged?.Invoke();
            return true;
        }

        public int MpCostWithRune(SkillData s) => Mathf.Max(0, Mathf.RoundToInt(MpCost(s) * RuneMath.MpFactor(RuneFor(s))));

        public SkillData FindSkill(string id) => AllSkills().Find(s => s != null && s.id == id);

        /// <summary>MP cost after the job's multiplier (summoners are mana-hungry).</summary>
        public int MpCost(SkillData s) => Mathf.RoundToInt(s.mpCost * (CurrentJob != null ? CurrentJob.mpCostMultiplier : 1f));

        /// <summary>Job Change (§8): only a job on offer right now. Learns its skills and refills.</summary>
        public bool ChangeJob(string jobId)
        {
            var reg = global::Aethvirse.Jobs.JobRegistry.Instance;
            if (reg == null) return false;
            var offered = reg.Options(classData.id, JobId, Progression.level, World.GameSession.Instance.Flags.Has);
            global::Aethvirse.Jobs.JobData target = offered.Find(j => j.id == jobId);
            if (target == null) return false;
            SetJob(target.id);
            World.GameSession.Instance.Flags.SetFlag("job_" + target.id);
            JobChanged?.Invoke(target);
            return true;
        }

        void SetJob(string jobId)
        {
            JobId = jobId ?? "";
            var reg = global::Aethvirse.Jobs.JobRegistry.Instance;
            Jobs = reg != null ? reg.Lineage(JobId) : new List<global::Aethvirse.Jobs.JobData>();
            EnsureSkillsUnlocked();
            Recompute(refill: true);
        }
        /// <summary>Share of damage dealt healed back (gear affixes).</summary>
        public float Lifesteal { get; private set; }

        /// <summary>Anything shown in the HUD / status panel changed.</summary>
        public event Action Changed;
        public event Action<int> LeveledUp;
        public event Action<SkillData> SkillLearned;
        /// <summary>XP actually gained (after the combo-heat bonus).</summary>
        public event Action<int> XpGained;

        static float Now => Time.time;

        /// <summary>The player's stats in the loaded scene (saves, cheats, UI).</summary>
        public static PlayerStats Current { get; private set; }

        void Awake()
        {
            Current = this;
            UseClass(World.GameSession.Instance.ClassId);
            Progression = new Progression(tuning);
            EnsureSkillsUnlocked();
            Recompute(refill: true);
        }

        void Start()
        {
            // A loaded save waits for the player to exist.
            Save.SaveData pending = Save.SaveManager.TakePending();
            if (pending != null) Restore(pending.player);
            World.GameSession.Instance.WorldRunning = true;
            Quests.QuestSystem.Instance.OnWorldReady();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (World.GameSession.Exists) World.GameSession.Instance.WorldRunning = false;
        }

        /// <summary>Writes the player's part of a save.</summary>
        public void Capture(Save.SaveData.PlayerSection p)
        {
            p.classId = classData.id;
            p.progression = Progression;
            p.skillIds.Clear();
            p.skillLevels.Clear();
            foreach (var kv in _skillLevels)
            {
                p.skillIds.Add(kv.Key);
                p.skillLevels.Add(kv.Value);
            }
            p.slots = (string[])Slots.Clone();
            p.secretSkills = new List<string>(SecretSkills);
            p.runeSkills = new List<string>(_runes.Keys);
            p.runeItems = new List<string>(_runes.Values);
            p.jobId = JobId;
            p.shadows = Shadows;
            if (player != null)
            {
                p.position = player.transform.position;
                p.hasPosition = true;
            }
            p.health = damageable != null ? damageable.Health.Ratio : 1f;
            if (inventory != null && inventory.Bag != null)
            {
                p.inventory = inventory.Bag;
                p.hasInventory = true;
                p.stash = inventory.Stash;
            }
            p.mana = Mana.Ratio;
        }

        /// <summary>Switches to a class by id (New Game choice, saves); keeps the scene default if unknown.</summary>
        void UseClass(string id)
        {
            ClassData c = ClassRegistry.Instance != null ? ClassRegistry.Instance.Get(id) : null;
            if (c != null) classData = c;
            if (combat != null) combat.SetClass(classData);
        }

        /// <summary>Applies the player's part of a save (after the scene loaded).</summary>
        public void Restore(Save.SaveData.PlayerSection p)
        {
            if (!string.IsNullOrEmpty(p.classId) && p.classId != classData.id)
            {
                World.GameSession.Instance.ClassId = p.classId;
                UseClass(p.classId);
            }
            if (p.progression != null)
            {
                Progression = p.progression;
                Progression.Tuning = tuning;
            }
            _skillLevels.Clear();
            for (int i = 0; i < p.skillIds.Count && i < p.skillLevels.Count; i++) _skillLevels[p.skillIds[i]] = p.skillLevels[i];
            JobId = p.jobId ?? "";
            SecretSkills = p.secretSkills != null ? new List<string>(p.secretSkills) : new List<string>();
            _runes.Clear();
            if (p.runeSkills != null && p.runeItems != null)
                for (int i = 0; i < p.runeSkills.Count && i < p.runeItems.Count; i++) _runes[p.runeSkills[i]] = p.runeItems[i];
            Shadows = p.shadows ?? new global::Aethvirse.Jobs.ShadowArmy();
            var reg = global::Aethvirse.Jobs.JobRegistry.Instance;
            Jobs = reg != null ? reg.Lineage(JobId) : new List<global::Aethvirse.Jobs.JobData>();
            for (int i = 0; i < SlotCount; i++) Slots[i] = p.slots != null && i < p.slots.Length ? p.slots[i] : null;
            if (inventory != null && p.hasInventory) inventory.Restore(p.inventory);
            if (inventory != null) inventory.RestoreStash(p.stash);
            EnsureSkillsUnlocked();
            Recompute(refill: true);
            if (damageable != null && p.health < 1f)
                damageable.Health.SetCurrent(Mathf.Max(1, Mathf.RoundToInt(damageable.Health.maxHealth * p.health)));
            Mana.SetRatio(p.mana);
            if (player != null && p.hasPosition) player.Teleport(p.position);
        }

        void OnEnable()
        {
            Enemies.EnemyEvents.Killed += OnEnemyKilled;
            if (inventory != null) inventory.EquipmentChanged += OnEquipmentChanged;
        }

        void OnDisable()
        {
            Enemies.EnemyEvents.Killed -= OnEnemyKilled;
            if (inventory != null) inventory.EquipmentChanged -= OnEquipmentChanged;
        }

        void OnEquipmentChanged() => Recompute(refill: false);

        void Update()
        {
            Mana.Tick(Time.deltaTime);
            if (_buffs.Expire(Now)) Recompute(refill: false);
        }

        void OnEnemyKilled(int xp) => AddXp(xp);

        public int SkillLevel(string id) => _skillLevels.TryGetValue(id, out int level) ? level : 0;

        public SkillData SlotSkill(int slot) =>
            slot >= 0 && slot < SlotCount && !string.IsNullOrEmpty(Slots[slot]) ? FindSkill(Slots[slot]) : null;

        /// <summary>Recalculates everything from progression, passives and buffs.</summary>
        public void Recompute(bool refill)
        {
            int level = Progression.level;
            Primary = Progression.PrimaryStats(classData.baseStats, classData.growth);
            bool gear = inventory != null && inventory.Bag != null;
            if (gear) Primary = Primary.Plus(inventory.EquipmentStats());
            // Equipped weapon (none = bare hands; the starter kit equips the issued weapon).
            float weapon = gear ? inventory.WeaponAttack(level) : level * tuning.weaponAtkPerLevel;
            foreach (global::Aethvirse.Jobs.JobData j in Jobs) Primary = Primary.Plus(j.statBonus);
            DerivedStats d = DerivedStats.Compute(level, Primary, tuning, weapon);
            float jobAtk = 0f, jobDef = 0f, jobHp = 0f;
            foreach (global::Aethvirse.Jobs.JobData j in Jobs) { jobAtk += j.attackBonus; jobDef += j.defenseBonus; jobHp += j.hpBonus; }
            d.maxHp = Mathf.RoundToInt(d.maxHp * (1f + jobHp));
            if (gear)
            {
                d.defense += inventory.EquipmentDefense();
                d.critChance = Mathf.Min(tuning.critCap, d.critChance + inventory.EquipmentBonus(Items.StatKeys.Crit));
                Lifesteal = inventory.EquipmentBonus(Items.StatKeys.Lifesteal);
                // Percent bonuses from sets, gems and enchants.
                d.attack *= 1f + inventory.EquipmentBonus(Items.StatKeys.AtkPct);
                d.magicAttack *= 1f + inventory.EquipmentBonus(Items.StatKeys.AtkPct);
                d.defense *= 1f + inventory.EquipmentBonus(Items.StatKeys.DefPct);
                d.maxHp = Mathf.RoundToInt(d.maxHp * (1f + inventory.EquipmentBonus(Items.StatKeys.HpPct)));
                d.cooldownReduction = Mathf.Min(0.5f, d.cooldownReduction + inventory.EquipmentBonus(Items.StatKeys.Cdr));
                d.attackSpeed += inventory.EquipmentBonus(Items.StatKeys.Aspd);
                d.moveSpeed += inventory.EquipmentBonus(Items.StatKeys.Mspd);
                d.critDamage += inventory.EquipmentBonus(Items.StatKeys.CritDmg);
            }
            float atkBonus = _buffs.Total(BuffStat.Attack) + PassiveTotal(BuffStat.Attack) + jobAtk;
            d.attack *= 1f + atkBonus;
            d.magicAttack *= 1f + atkBonus;
            d.defense *= 1f + _buffs.Total(BuffStat.Defense) + PassiveTotal(BuffStat.Defense) + jobDef;
            d.critChance = Mathf.Min(tuning.critCap, d.critChance + _buffs.Total(BuffStat.Crit) + PassiveTotal(BuffStat.Crit));
            d.attackSpeed += _buffs.Total(BuffStat.AttackSpeed) + PassiveTotal(BuffStat.AttackSpeed);
            d.moveSpeed += _buffs.Total(BuffStat.MoveSpeed) + PassiveTotal(BuffStat.MoveSpeed);
            Derived = d;

            CombatStats combatStats = d.ToCombatStats(classData.magicDamage);
            if (combat != null)
            {
                combat.SetStats(combatStats);
                combat.Lifesteal = Lifesteal;
                combat.WeaponElement = gear ? inventory.WeaponElement() : ElementType.Physical;
            }
            if (damageable != null)
            {
                damageable.SetStats(combatStats);
                damageable.Health.SetMax(d.maxHp, refill);
            }
            Mana.regenPerSecond = d.mpRegen;
            Mana.SetMax(d.maxMp, refill);
            if (player != null)
            {
                player.SpeedMultiplier = d.moveSpeed;
                player.AttackSpeed = d.attackSpeed;
            }
            Changed?.Invoke();
        }

        /// <summary>XP with the combo-heat bonus (a hotter combo when the enemy falls gives more).</summary>
        public void AddXp(int amount)
        {
            ComboRank heat = combat != null ? combat.Tracker.Rank(Time.unscaledTime) : ComboRank.None;
            int total = Mathf.RoundToInt(amount * ComboTracker.Bonus(heat));
            int gained = Progression.AddXp(total);
            XpGained?.Invoke(total);
            if (gained > 0)
            {
                EnsureSkillsUnlocked();
                Recompute(refill: true);
                LeveledUp?.Invoke(Progression.level);
                Core.Sfx.LevelUp();
            }
            else Changed?.Invoke();
        }

        public bool Allocate(Stat stat)
        {
            if (!Progression.Allocate(stat)) return false;
            Recompute(refill: false);
            return true;
        }

        public bool LevelUpSkill(string id)
        {
            SkillData skill = FindSkill(id);
            int current = SkillLevel(id);
            if (skill == null || current <= 0 || current >= skill.maxLevel || Progression.skillPoints <= 0) return false;
            Progression.skillPoints--;
            _skillLevels[id] = current + 1;
            Recompute(refill: false);
            return true;
        }

        public void AddBuff(BuffStat stat, float amount, float duration)
        {
            _buffs.Add(stat, amount, duration, Now);
            Recompute(refill: false);
        }

        /// <summary>Permanent bonus from learned passive skills.</summary>
        public float PassiveTotal(BuffStat stat)
        {
            float total = 0f;
            foreach (SkillData s in AllSkills())
                if (s.IsPassive && s.buffStat == stat && SkillLevel(s.id) > 0) total += s.BuffAt(SkillLevel(s.id));
            return total;
        }

        /// <summary>Learns newly unlocked skills at level 1 and puts actives on free slots.</summary>
        void EnsureSkillsUnlocked()
        {
            foreach (SkillData s in AllSkills())
            {
                if (Progression.level < s.unlockLevel || SkillLevel(s.id) > 0) continue;
                _skillLevels[s.id] = 1;
                AutoSlot(s);
                SkillLearned?.Invoke(s);
            }
        }

        void AutoSlot(SkillData s)
        {
            if (s.IsPassive) return;
            if (s.isUltimate)
            {
                if (string.IsNullOrEmpty(Slots[UltimateSlot])) Slots[UltimateSlot] = s.id;
                return;
            }
            for (int i = 0; i < UltimateSlot; i++)
            {
                if (!string.IsNullOrEmpty(Slots[i])) continue;
                Slots[i] = s.id;
                return;
            }
        }

        /// <summary>Puts an active skill into slot 0–3; the ultimate slot only takes ultimates.</summary>
        public bool AssignSlot(string id, int slot)
        {
            SkillData s = FindSkill(id);
            if (s == null || s.IsPassive || SkillLevel(id) <= 0 || slot < 0 || slot >= SlotCount) return false;
            if ((slot == UltimateSlot) != s.isUltimate) return false;
            for (int i = 0; i < SlotCount; i++) if (Slots[i] == id) Slots[i] = null;
            Slots[slot] = id;
            Changed?.Invoke();
            return true;
        }
    }
}
