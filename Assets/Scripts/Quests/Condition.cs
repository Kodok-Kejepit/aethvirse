using System;
using System.Collections.Generic;

namespace Aethvirse.Quests
{
    public enum ConditionType { Flag, Weather, HourRange, IsNight, HasItem, CounterAtLeast, LevelAtLeast, StatAtLeast, AcademyRankAtLeast, QuestDone, QuestActive }

    /// <summary>What a condition can ask about the game (implemented by <see cref="GameContext"/>; tests use fakes).</summary>
    public interface IConditionContext
    {
        bool HasFlag(string flag);
        string Weather { get; }
        int Hour { get; }
        bool IsNight { get; }
        bool HasItem(string itemId, int count);
        int Counter(string key);
        int Level { get; }
        float Stat(string key);
        int AcademyRank { get; }
        bool QuestDone(string id);
        bool QuestActive(string id);
    }

    /// <summary>One data-driven condition (§9.1 ConditionData): quests, dialog lines and secrets use lists of these.</summary>
    [Serializable]
    public sealed class Condition
    {
        public ConditionType type = ConditionType.Flag;
        public string key = "";
        public List<string> values = new List<string>();
        public float value;
        public float value2;
        public bool negate;

        public bool Evaluate(IConditionContext ctx)
        {
            bool r = type switch
            {
                ConditionType.Flag => ctx.HasFlag(key),
                ConditionType.Weather => values.Contains(ctx.Weather),
                ConditionType.HourRange => value <= value2 ? ctx.Hour >= value && ctx.Hour < value2 : ctx.Hour >= value || ctx.Hour < value2,
                ConditionType.IsNight => ctx.IsNight,
                ConditionType.HasItem => values.Exists(id => ctx.HasItem(id, Math.Max(1, (int)value))) || (!string.IsNullOrEmpty(key) && ctx.HasItem(key, Math.Max(1, (int)value))),
                ConditionType.CounterAtLeast => ctx.Counter(key) >= value,
                ConditionType.LevelAtLeast => ctx.Level >= (int)value,
                ConditionType.StatAtLeast => ctx.Stat(key) >= value,
                ConditionType.AcademyRankAtLeast => ctx.AcademyRank >= (int)value,
                ConditionType.QuestDone => ctx.QuestDone(key),
                ConditionType.QuestActive => ctx.QuestActive(key),
                _ => false,
            };
            return negate ? !r : r;
        }

        public static bool All(List<Condition> list, IConditionContext ctx)
        {
            if (list == null) return true;
            foreach (Condition c in list) if (!c.Evaluate(ctx)) return false;
            return true;
        }

        public static Condition Make(ConditionType t, string key = "", float value = 0f, bool negate = false) =>
            new Condition { type = t, key = key, value = value, negate = negate };
    }
}
