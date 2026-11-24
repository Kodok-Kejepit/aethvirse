using UnityEngine;

namespace Aethvirse.Quests
{
    /// <summary>
    /// The Codex daily quest (§10): one training, one hunting and one gathering task, picked by day and scaled by
    /// level. Missing a day sends you to the Penalty Zone. Pure builder (unit tested).
    /// </summary>
    public static class DailyQuests
    {
        enum Group { Training, Hunting, Gathering }

        // group, type, text, target, base count, per level, cap, min level
        static readonly (Group g, ObjectiveType t, string text, string target, int count, float perLevel, int cap, int minLevel)[] Templates =
        {
            (Group.Training, ObjectiveType.Attack, "Attack {0} times", "", 40, 2f, 100, 1),
            (Group.Training, ObjectiveType.Dodge, "Dodge {0} times", "", 20, 1f, 50, 1),
            (Group.Training, ObjectiveType.Distance, "Run {0} m", "", 1500, 120f, 5000, 1),
            (Group.Training, ObjectiveType.Skill, "Use skills {0} times", "", 8, 0.5f, 30, 1),
            (Group.Hunting, ObjectiveType.Kill, "Defeat {0} monsters", "", 8, 1f, 30, 1),
            (Group.Hunting, ObjectiveType.ClearGate, "Clear {0} Gate", "", 1, 0f, 1, 3),
            (Group.Gathering, ObjectiveType.Pickup, "Pick up {0} things", "", 6, 0.5f, 20, 1),
            (Group.Gathering, ObjectiveType.Pickup, "Gather {0} herbs", "herb", 3, 0.2f, 8, 1),
            (Group.Gathering, ObjectiveType.Craft, "Craft {0} item", "", 1, 0.1f, 3, 1),
            (Group.Gathering, ObjectiveType.UseItem, "Use {0} items", "", 2, 0f, 2, 1),
        };

        public const string Prefix = "daily_";
        public const int CodexPoints = 10;

        public static string IdFor(int day) => Prefix + day;

        public static QuestData Build(int day, int level, int xpReward)
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.id = IdFor(day);
            q.title = "Daily Quest";
            q.description = "The Codex expects these done today. It does not say what happens if they are not.";
            q.kind = QuestKind.Daily;
            foreach (Group g in new[] { Group.Training, Group.Hunting, Group.Gathering }) q.objectives.Add(ObjectiveFor(g, day, level));
            q.rewards.Add(new Reward { type = RewardType.Xp, amount = xpReward });
            q.rewards.Add(new Reward { type = RewardType.CodexPoints, amount = CodexPoints });
            q.academyPoints = 5;
            return q;
        }

        static QuestObjective ObjectiveFor(Group group, int day, int level)
        {
            var options = new System.Collections.Generic.List<int>();
            for (int i = 0; i < Templates.Length; i++)
                if (Templates[i].g == group && level >= Templates[i].minLevel) options.Add(i);
            int pick = options[(int)((uint)(day * 7919 + (int)group * 104729) % (uint)options.Count)];
            var t = Templates[pick];
            int count = Mathf.Min(t.count + Mathf.FloorToInt(t.perLevel * (level - 1)), t.cap);
            return new QuestObjective { type = t.t, target = t.target, count = count, text = string.Format(t.text, count) };
        }

        /// <summary>A sudden task with a timer (§10 emergency quest).</summary>
        public static QuestData Emergency(int serial, int level)
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.id = "emergency_" + serial;
            q.title = "[ EMERGENCY ]";
            q.description = "Monsters are gathering nearby. Thin them out before they spread.";
            q.kind = QuestKind.Emergency;
            q.timeLimit = 180f;
            q.objectives.Add(new QuestObjective { type = ObjectiveType.Kill, count = 6, text = "Defeat 6 monsters" });
            q.rewards.Add(new Reward { type = RewardType.Gold, amount = 60 + 10 * level });
            q.academyPoints = 15;
            return q;
        }
    }
}
