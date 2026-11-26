using Aethvirse.Interaction;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Items
{
    /// <summary>
    /// A chest in the world (§9.3): opens once a day for loot — unless it is a mimic, which bares its teeth and
    /// attacks. A beaten mimic drops loot like a boss. Whether it is a mimic is decided by the day, not on opening.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public sealed class Chest : MonoBehaviour
    {
        public const float MimicChance = 0.2f;

        [SerializeField] string chestId = "";
        [SerializeField] int level = 1;

        Interactable _use;

        string Flag => $"chest_{chestId}_day{GameSession.Instance.Clock.Day}";

        /// <summary>Same answer all day for the same chest (pure; tested).</summary>
        public static bool IsMimic(string id, int day) => new System.Random(id.GetHashCode() ^ (day * 7919)).NextDouble() < MimicChance;

        void Awake()
        {
            _use = GetComponent<Interactable>();
            _use.Prompt = "Open the chest";
            _use.Used += _ => Open();
        }

        void Update()
        {
            bool opened = GameSession.Instance.Flags.Has(Flag);
            _use.Available = !opened;
            transform.localScale = opened ? new Vector3(1f, 0.5f, 1f) : Vector3.one;
        }

        void Open()
        {
            GameSession.Instance.Flags.SetFlag(Flag);
            Core.Achievements.Count("chests", 1);
            var inv = PlayerInventory.Current;
            if (IsMimic(chestId, GameSession.Instance.Clock.Day) && Enemies.EnemyCatalog.Instance != null)
            {
                Enemies.Enemy m = Enemies.EnemyCatalog.Instance.Spawn("mimic", transform.position, level, 20f, true);
                if (m != null) { m.RespawnAtHome = false; m.MakeBoss(3f, 1.4f, 1f); }
                Codex.Notify("The chest has teeth.", CodexTone.Anomaly);
                return;
            }
            if (inv == null) return;
            var rng = new System.Random();
            int gold = 20 + level * 4 + rng.Next(level * 2 + 10);
            inv.AddGold(gold);
            foreach (ItemStack drop in ItemActions.RollDrops(level, Stats.EnemyTier.Elite, inv.Database, inv.Rules, rng))
                if (!inv.Bag.AddStack(drop)) WorldPickup.Spawn(transform.position + Vector3.up, drop);
            Codex.Notify($"Chest opened. +{gold} gold", CodexTone.Reward);
        }
    }
}
