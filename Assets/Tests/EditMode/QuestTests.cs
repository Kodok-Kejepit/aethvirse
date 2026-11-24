using System.Collections.Generic;
using Aethvirse.Quests;
using Aethvirse.Save;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class QuestTests
    {
        sealed class Ctx : IConditionContext
        {
            public HashSet<string> Flags = new HashSet<string>();
            public bool HasFlag(string f) => Flags.Contains(f);
            public string Weather { get; set; } = "Clear";
            public int Hour { get; set; } = 12;
            public bool IsNight => Hour >= 20 || Hour < 5;
            public Dictionary<string, int> Items = new Dictionary<string, int>();
            public bool HasItem(string id, int count) => Items.TryGetValue(id, out int n) && n >= count;
            public int Counter(string key) => 0;
            public int Level { get; set; } = 1;
            public float Stat(string key) => 10f;
            public int AcademyRank { get; set; }
            public HashSet<string> Done = new HashSet<string>();
            public bool QuestDone(string id) => Done.Contains(id);
            public bool QuestActive(string id) => false;
        }

        static QuestData Make()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.id = "q";
            q.objectives = new List<QuestObjective>
            {
                new QuestObjective { type = ObjectiveType.Talk, target = "horne", stage = 0 },
                new QuestObjective { type = ObjectiveType.Kill, target = "ash_hound", count = 3, stage = 1 },
                new QuestObjective { type = ObjectiveType.Pickup, target = "herb", count = 2, stage = 1, optional = true },
                new QuestObjective { type = ObjectiveType.Talk, target = "horne", stage = 2 },
            };
            return q;
        }

        [Test]
        public void Stages_RunInOrderAndOptionalDoesNotBlock()
        {
            QuestData q = Make();
            var log = new QuestLog();
            Assert.That(log.Start(q, 0f), Is.True);
            Assert.That(log.Start(q, 0f), Is.False, "already active");
            Assert.That(log.Notify(q, ObjectiveType.Kill, "ash_hound", 1), Is.False, "kills do not count in stage 0");
            log.Notify(q, ObjectiveType.Talk, "horne", 1);
            Assert.That(log.AdvanceIfReady(q), Is.False);
            Assert.That(log.Get("q").stage, Is.EqualTo(1));
            log.Notify(q, ObjectiveType.Kill, "other_monster", 1);
            Assert.That(log.Progress("q", 1), Is.EqualTo(0), "wrong target");
            log.Notify(q, ObjectiveType.Kill, "ash_hound", 5);
            Assert.That(log.Progress("q", 1), Is.EqualTo(3), "capped at the count");
            Assert.That(log.AdvanceIfReady(q), Is.False);
            Assert.That(log.Get("q").stage, Is.EqualTo(2), "the optional herb objective did not block");
            log.Notify(q, ObjectiveType.Talk, "horne", 1);
            Assert.That(log.AdvanceIfReady(q), Is.True);
            log.Complete("q");
            Assert.That(log.IsCompleted("q"), Is.True);
            Assert.That(log.Start(q, 0f), Is.False, "completed quests do not restart");
            Object.DestroyImmediate(q);
        }

        [Test]
        public void Tracker_PrefersRequiredObjectives()
        {
            QuestData q = Make();
            var log = new QuestLog();
            log.Start(q, 0f);
            log.Notify(q, ObjectiveType.Talk, "horne", 1);
            log.AdvanceIfReady(q);
            Assert.That(log.Current(q, out _).type, Is.EqualTo(ObjectiveType.Kill));
            Object.DestroyImmediate(q);
        }

        [Test]
        public void Conditions_EvaluateAndNegate()
        {
            var ctx = new Ctx();
            ctx.Flags.Add("met_horne");
            ctx.Items["herb"] = 3;
            Assert.That(Condition.Make(ConditionType.Flag, "met_horne").Evaluate(ctx), Is.True);
            Assert.That(Condition.Make(ConditionType.Flag, "met_horne", negate: true).Evaluate(ctx), Is.False);
            Assert.That(new Condition { type = ConditionType.HasItem, key = "herb", value = 3 }.Evaluate(ctx), Is.True);
            Assert.That(new Condition { type = ConditionType.HasItem, key = "herb", value = 4 }.Evaluate(ctx), Is.False);
            ctx.Hour = 23;
            Assert.That(new Condition { type = ConditionType.HourRange, value = 22, value2 = 4 }.Evaluate(ctx), Is.True, "wraps midnight");
            Assert.That(Condition.Make(ConditionType.IsNight).Evaluate(ctx), Is.True);
            Assert.That(Condition.All(null, ctx), Is.True);
        }

        [Test]
        public void Daily_HasThreeTasksStableForTheDay()
        {
            QuestData a = DailyQuests.Build(5, 10, 100);
            QuestData b = DailyQuests.Build(5, 10, 100);
            Assert.That(a.objectives.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(a.objectives[i].type, Is.EqualTo(b.objectives[i].type));
                Assert.That(a.objectives[i].count, Is.GreaterThan(0));
            }
            Assert.That(a.id, Is.EqualTo("daily_5"));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void Daily_ScalesButStaysCapped()
        {
            for (int day = 1; day < 40; day++)
            {
                QuestData q = DailyQuests.Build(day, 300, 100);
                foreach (QuestObjective o in q.objectives) Assert.That(o.count, Is.LessThanOrEqualTo(5000));
                Object.DestroyImmediate(q);
            }
        }

        [TestCase(0, 0), TestCase(99, 0), TestCase(100, 1), TestCase(300, 2), TestCase(700, 3), TestCase(1500, 4), TestCase(99999, 5)]
        public void AcademyRank_Thresholds(int points, int rank) => Assert.That(AcademyRank.For(points), Is.EqualTo(rank));

        [Test]
        public void QuestLog_SurvivesASave()
        {
            QuestData q = Make();
            var data = new SaveData();
            data.world.quests.Start(q, 0f);
            data.world.quests.Notify(q, ObjectiveType.Talk, "horne", 1);
            data.world.quests.completed.Add("q_cinder_seat");
            data.world.academyPoints = 120;
            SaveData back = SaveData.FromJson(data.ToJson());
            Assert.That(back.world.quests.IsActive("q"), Is.True);
            Assert.That(back.world.quests.Progress("q", 0), Is.EqualTo(1));
            Assert.That(back.world.quests.IsCompleted("q_cinder_seat"), Is.True);
            Assert.That(back.world.academyPoints, Is.EqualTo(120));
            Object.DestroyImmediate(q);
        }
    }
}
