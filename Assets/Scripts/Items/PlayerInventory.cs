using System;
using System.Collections.Generic;
using Aethvirse.Combat;
using Aethvirse.Input;
using Aethvirse.Stats;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>
    /// The player's bag, equipment and wallet (port of the Godot PlayerItems): feeds equipment into stats,
    /// lifesteal, curses (HP drain), evolving weapons (kills), quick potion (H / D-pad down), starter kit.
    /// </summary>
    [DefaultExecutionOrder(-50)] // the bag must exist before PlayerStats computes equipment
    public sealed class PlayerInventory : MonoBehaviour, ItemActions.IWallet
    {
        [SerializeField] ItemDatabase database;
        [SerializeField] LootRules rules;
        [SerializeField] List<Recipe> recipes = new List<Recipe>();
        [SerializeField] List<ShopEntry> quartermasterStock = new List<ShopEntry>();
        [SerializeField] string starterWeapon = "issued_greatsword";

        public static PlayerInventory Current { get; private set; }

        public Inventory Bag { get; private set; }
        public ItemDatabase Database => database;
        public LootRules Rules => rules;
        public IReadOnlyList<Recipe> Recipes => recipes;
        public IReadOnlyList<ShopEntry> QuartermasterStock => quartermasterStock;
        public readonly System.Random Rng = new System.Random();

        public int Gold => GameSession.Instance.Gold;
        public void AddGold(int amount) => GameSession.Instance.AddGold(amount);
        public bool SpendGold(int amount) => GameSession.Instance.SpendGold(amount);

        /// <summary>Equipment changed: stats must recompute.</summary>
        public event Action EquipmentChanged;

        float _curseTimer;
        PlayerStats _stats;
        Damageable _health;

        void Awake()
        {
            Current = this;
            Bag = new Inventory(database);
            GiveStarterKit();
            _stats = GetComponent<PlayerStats>();
            _health = GetComponent<Damageable>();
        }

        void OnEnable() => Enemies.EnemyEvents.KilledEnemy += OnKill;
        void OnDisable() => Enemies.EnemyEvents.KilledEnemy -= OnKill;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void GiveStarterKit()
        {
            string weaponId = Stats.ClassRegistry.StarterWeapon(GameSession.Instance.ClassId);
            if (database.Get(weaponId) != null) starterWeapon = weaponId;
            Bag.Add(starterWeapon);
            ItemStack weapon = Bag.stacks.Find(s => s.itemId == starterWeapon);
            if (weapon != null) Bag.Equip(weapon);
            Bag.Add("potion_small", 3);
        }

        /// <summary>Replaces the bag with a loaded one.</summary>
        public void Restore(Inventory loaded)
        {
            if (loaded == null) return;
            Bag = loaded;
            Bag.Db = database;
            EquipmentChanged?.Invoke();
        }

        void Update()
        {
            if (GameInput.Instance.QuickPotion.WasPressedThisFrame()) QuickPotion();
            TickCurses(Time.deltaTime);
        }

        // --- Stats -------------------------------------------------------------------------------

        public float WeaponAttack(int level)
        {
            ItemStack s = Bag.GetEquipped(EquipSlot.Weapon);
            ItemData item = s != null ? database.Get(s.itemId) : null;
            return item != null ? ItemStats.Attack(s, item, level, rules) : 0f;
        }

        public float EquipmentDefense()
        {
            float total = 0f;
            foreach (Inventory.Equipped e in Bag.equipped)
            {
                ItemData item = database.Get(e.stack.itemId);
                if (item != null) total += ItemStats.Defense(e.stack, item, rules);
            }
            return total;
        }

        public float EquipmentBonus(string stat)
        {
            float total = 0f;
            foreach (Inventory.Equipped e in Bag.equipped)
            {
                ItemData item = database.Get(e.stack.itemId);
                if (item != null && e.stack.identified) total += ItemStats.Bonus(e.stack, item, stat, rules);
            }
            return total;
        }

        public StatBlock EquipmentStats() => new StatBlock(EquipmentBonus(StatKeys.Str), EquipmentBonus(StatKeys.Agi),
            EquipmentBonus(StatKeys.Int), EquipmentBonus(StatKeys.Vit), EquipmentBonus(StatKeys.Sen));

        // --- Actions -------------------------------------------------------------------------------

        public bool Equip(ItemStack s)
        {
            if (!Bag.Equip(s)) return false;
            ItemData item = database.Get(s.itemId);
            if (item != null && item.cursed) Codex.Notify($"{Bag.DisplayName(s)} binds itself to you. It will not come off.", CodexTone.Anomaly);
            EquipmentChanged?.Invoke();
            return true;
        }

        public bool Unequip(EquipSlot slot)
        {
            ItemData item = Bag.GetEquippedItem(slot);
            if (!Bag.Unequip(slot))
            {
                if (item != null && item.cursed) Codex.Notify("It will not come off.", CodexTone.Warning);
                return false;
            }
            EquipmentChanged?.Invoke();
            return true;
        }

        /// <summary>Potions, food, scrolls, mystery boxes.</summary>
        public bool Use(ItemStack s)
        {
            ItemData item = database.Get(s.itemId);
            if (item == null || !item.IsUsable) return false;
            if (item.opensMysteryBox)
            {
                int level = _stats != null ? _stats.Progression.level : 1;
                var (steps, gear, key) = ItemActions.OpenMysteryBox(Bag, s, level, rules, Rng);
                string tiers = string.Join(" → ", Array.ConvertAll(steps, t => ((Rarity)Mathf.Min(t, 6)).ToString()));
                Codex.Notify($"Mystery Box: {tiers}" + (gear != null ? $"\n{Bag.DisplayName(gear)}" : "") + (key ? " + Gate Key" : ""), CodexTone.Reward);
                return true;
            }
            if (item.kind == ItemKind.SkillBook && !string.IsNullOrEmpty(item.teachesSkill))
            {
                if (_stats == null || !_stats.LearnSecretSkill(item.teachesSkill))
                {
                    Codex.Notify("The pages are blank to you. You already know this.", CodexTone.Info);
                    return false;
                }
                Bag.TakeOne(s);
                var learned = Skills.SecretSkillDatabase.Instance.Skill(item.teachesSkill);
                Codex.Notify($"[ SECRET SKILL ]  {learned.displayName}\n{learned.description}", CodexTone.Anomaly);
                Core.GameEvents.RaiseItemUsed(item.id);
                return true;
            }
            if (item.identifiesItems)
            {
                ItemStack target = Bag.stacks.Find(x => !x.identified);
                if (target == null) { Codex.Notify("Nothing to identify.", CodexTone.Info); return false; }
                target.identified = true;
                target.identifiedBy = "scroll";
                Bag.TakeOne(s);
                Codex.Notify($"Identified: {Bag.DisplayName(target)}", CodexTone.Reward);
                return true;
            }
            if (item.healAmount > 0 && _health != null)
            {
                if (_health.Health.Current >= _health.Health.maxHealth) return false;
                _health.Health.Heal(item.healAmount);
            }
            if (item.manaAmount > 0 && _stats != null) _stats.Mana.Restore(item.manaAmount);
            Bag.TakeOne(s);
            Core.GameEvents.RaiseItemUsed(item.id);
            return true;
        }

        public bool QuickPotion()
        {
            ItemStack best = null;
            int bestHeal = int.MaxValue;
            foreach (ItemStack s in Bag.stacks)
            {
                ItemData item = database.Get(s.itemId);
                if (item == null || item.kind != ItemKind.Consumable || item.healAmount <= 0) continue;
                if (item.healAmount < bestHeal) { best = s; bestHeal = item.healAmount; }
            }
            return best != null && Use(best);
        }

        /// <summary>Loot reaching the player (pickups): gear and items into the bag, gold into the wallet.</summary>
        public bool Give(ItemStack s)
        {
            if (s.itemId == "gold") { AddGold(s.count); return true; }
            bool ok = s.IsUniqueInstance ? Bag.AddStack(s) : Bag.Add(s.itemId, s.count) == 0;
            if (!ok) Codex.Notify("Bag full.", CodexTone.Warning);
            else Core.GameEvents.RaisePickedUp(s.itemId, s.count);
            return ok;
        }

        // --- Curses and evolving weapons ----------------------------------------------------------

        void TickCurses(float dt)
        {
            if (_health == null || _health.IsDead) return;
            _curseTimer += dt;
            foreach (Inventory.Equipped e in Bag.equipped)
            {
                ItemData item = database.Get(e.stack.itemId);
                if (item == null || !item.cursed || item.curseHpFraction <= 0f || _curseTimer < item.curseInterval) continue;
                int drain = Mathf.Min(Mathf.Max(1, Mathf.RoundToInt(_health.Health.maxHealth * item.curseHpFraction)), _health.Health.Current - 1);
                if (drain > 0) _health.Health.SetCurrent(_health.Health.Current - drain);
                _curseTimer = 0f;
            }
        }

        void OnKill(Enemies.EnemyData _)
        {
            ItemStack weapon = Bag.GetEquipped(EquipSlot.Weapon);
            ItemData item = weapon != null ? database.Get(weapon.itemId) : null;
            if (item == null || item.evolveKills <= 0) return;
            weapon.kills++;
            if (weapon.kills < item.evolveKills || database.Get(item.evolvesInto) == null) return;
            string before = Bag.DisplayName(weapon);
            weapon.itemId = item.evolvesInto;
            weapon.kills = 0;
            Codex.Notify($"{before} has changed. It is now {Bag.DisplayName(weapon)}.", CodexTone.Anomaly);
            EquipmentChanged?.Invoke();
        }
    }
}
