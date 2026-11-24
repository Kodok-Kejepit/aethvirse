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
            }
            p.mana = Mana.Ratio;
        }

        /// <summary>Applies the player's part of a save (after the scene loaded).</summary>
        public void Restore(Save.SaveData.PlayerSection p)
        {
            if (p.progression != null)
            {
                Progression = p.progression;
                Progression.Tuning = tuning;
            }
            _skillLevels.Clear();
            for (int i = 0; i < p.skillIds.Count && i < p.skillLevels.Count; i++) _skillLevels[p.skillIds[i]] = p.skillLevels[i];
            for (int i = 0; i < SlotCount; i++) Slots[i] = p.slots != null && i < p.slots.Length ? p.slots[i] : null;
            if (inventory != null && p.hasInventory) inventory.Restore(p.inventory);
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
            slot >= 0 && slot < SlotCount && !string.IsNullOrEmpty(Slots[slot]) ? classData.FindSkill(Slots[slot]) : null;

        /// <summary>Recalculates everything from progression, passives and buffs.</summary>
        public void Recompute(bool refill)
        {
            int level = Progression.level;
            Primary = Progression.PrimaryStats(classData.baseStats, classData.growth);
            bool gear = inventory != null && inventory.Bag != null;
            if (gear) Primary = Primary.Plus(inventory.EquipmentStats());
            // Equipped weapon (none = bare hands; the starter kit equips the issued weapon).
            float weapon = gear ? inventory.WeaponAttack(level) : level * tuning.weaponAtkPerLevel;
            DerivedStats d = DerivedStats.Compute(level, Primary, tuning, weapon);
            if (gear)
            {
                d.defense += inventory.EquipmentDefense();
                d.critChance = Mathf.Min(tuning.critCap, d.critChance + inventory.EquipmentBonus(Items.StatKeys.Crit));
                Lifesteal = inventory.EquipmentBonus(Items.StatKeys.Lifesteal);
            }
            float atkBonus = _buffs.Total(BuffStat.Attack) + PassiveTotal(BuffStat.Attack);
            d.attack *= 1f + atkBonus;
            d.magicAttack *= 1f + atkBonus;
            d.defense *= 1f + _buffs.Total(BuffStat.Defense) + PassiveTotal(BuffStat.Defense);
            d.critChance = Mathf.Min(tuning.critCap, d.critChance + _buffs.Total(BuffStat.Crit) + PassiveTotal(BuffStat.Crit));
            d.attackSpeed += _buffs.Total(BuffStat.AttackSpeed) + PassiveTotal(BuffStat.AttackSpeed);
            d.moveSpeed += _buffs.Total(BuffStat.MoveSpeed) + PassiveTotal(BuffStat.MoveSpeed);
            Derived = d;

            CombatStats combatStats = d.ToCombatStats(classData.magicDamage);
            if (combat != null)
            {
                combat.SetStats(combatStats);
                combat.Lifesteal = Lifesteal;
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
            SkillData skill = classData.FindSkill(id);
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
            foreach (SkillData s in classData.skills)
                if (s.IsPassive && s.buffStat == stat && SkillLevel(s.id) > 0) total += s.BuffAt(SkillLevel(s.id));
            return total;
        }

        /// <summary>Learns newly unlocked skills at level 1 and puts actives on free slots.</summary>
        void EnsureSkillsUnlocked()
        {
            foreach (SkillData s in classData.skills)
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
            SkillData s = classData.FindSkill(id);
            if (s == null || s.IsPassive || SkillLevel(id) <= 0 || slot < 0 || slot >= SlotCount) return false;
            if ((slot == UltimateSlot) != s.isUltimate) return false;
            for (int i = 0; i < SlotCount; i++) if (Slots[i] == id) Slots[i] = null;
            Slots[slot] = id;
            Changed?.Invoke();
            return true;
        }
    }
}
