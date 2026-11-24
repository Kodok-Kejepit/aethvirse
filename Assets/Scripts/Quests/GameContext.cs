using Aethvirse.Items;
using Aethvirse.Stats;
using Aethvirse.World;

namespace Aethvirse.Quests
{
    /// <summary>Conditions read the live game through this (session, player stats, bag, quests).</summary>
    public sealed class GameContext : IConditionContext
    {
        public static readonly GameContext Instance = new GameContext();

        static GameSession S => GameSession.Instance;

        public bool HasFlag(string flag) => S.Flags.Has(flag);
        public string Weather => S.Weather.current.ToString();
        public int Hour => S.Clock.Hour;
        public bool IsNight => S.Clock.IsNight;
        public bool HasItem(string itemId, int count) => PlayerInventory.Current != null && PlayerInventory.Current.Bag.Has(itemId, count);
        public int Counter(string key) => S.Flags.Counter(key);
        public int Level => PlayerStats.Current != null ? PlayerStats.Current.Progression.level : 1;

        public float Stat(string key)
        {
            PlayerStats p = PlayerStats.Current;
            if (p == null) return 0f;
            return key switch
            {
                "str" => p.Primary.strength, "agi" => p.Primary.agility, "int" => p.Primary.intelligence,
                "vit" => p.Primary.vitality, "sen" => p.Primary.sense, _ => 0f,
            };
        }

        public int AcademyRank => Quests.AcademyRank.For(S.AcademyPoints);
        public bool QuestDone(string id) => QuestSystem.Instance.Log.IsCompleted(id);
        public bool QuestActive(string id) => QuestSystem.Instance.Log.IsActive(id);
    }
}
