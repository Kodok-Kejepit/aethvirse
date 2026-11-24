using Aethvirse.Stats;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    /// <summary>Numbers checked against aethvirse-dev/docs/AETHVIRSE_Balancing.xlsx (same as the Godot tests).</summary>
    public sealed class ProgressionTests
    {
        StatTuning _t;
        ClassData _warden;

        [SetUp]
        public void SetUp()
        {
            _t = ScriptableObject.CreateInstance<StatTuning>();
            _warden = TestClasses.Warden();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_t);
            Object.DestroyImmediate(_warden);
        }

        [Test]
        public void XpCurve_MatchesFormula()
        {
            Assert.That(Progression.XpNeeded(1, _t), Is.EqualTo(100));
            Assert.That(Progression.XpNeeded(2, _t), Is.EqualTo(Mathf.RoundToInt(100f * Mathf.Pow(2f, 1.8f))));
            Assert.That(Progression.XpNeeded(10, _t), Is.EqualTo(Mathf.RoundToInt(100f * Mathf.Pow(10f, 1.8f))));
            Assert.That(Progression.XpNeeded(100, _t), Is.GreaterThan(0), "100 is not the cap");
            Assert.That(Progression.XpNeeded(1000, _t), Is.EqualTo(0), "cap at 1000");
        }

        [Test]
        public void LevelUp_GrantsPoints()
        {
            var p = new Progression(_t);
            Assert.That(p.AddXp(100), Is.EqualTo(1));
            Assert.That(p.level, Is.EqualTo(2));
            Assert.That(p.freePoints, Is.EqualTo(5));
            Assert.That(p.skillPoints, Is.EqualTo(1));
        }

        [Test]
        public void MultiLevelUp_KeepsLeftover()
        {
            var p = new Progression(_t);
            p.AddXp(Progression.XpNeeded(1, _t) + Progression.XpNeeded(2, _t) + 7);
            Assert.That(p.level, Is.EqualTo(3));
            Assert.That(p.xp, Is.EqualTo(7));
        }

        [Test]
        public void Allocate_Points()
        {
            var p = new Progression(_t);
            p.AddXp(100);
            Assert.That(p.Allocate(Stat.STR, 3), Is.True);
            Assert.That(p.freePoints, Is.EqualTo(2));
            Assert.That(p.Allocate(Stat.STR, 5), Is.False, "not enough points");
            Assert.That(p.PrimaryStats(_warden.baseStats, _warden.growth).strength, Is.EqualTo(12f + 2f + 3f), "base + growth + allocated");
        }

        [Test]
        public void WardenLevel1_DerivedMatchesSheet()
        {
            DerivedStats d = DerivedStats.Compute(1, _warden.baseStats, _t, 0f);
            Assert.That(d.maxHp, Is.EqualTo(330), "100 + 11×20 + 1×10");
            Assert.That(d.maxMp, Is.EqualTo(83), "50 + 3×10 + 1×3");
            Assert.That(d.attack, Is.EqualTo(27f).Within(0.001f), "12×2 + 6×0.5");
            Assert.That(d.defense, Is.EqualTo(14.6f).Within(0.001f), "11×1 + 12×0.3");
            Assert.That(d.critChance, Is.EqualTo(0.064f).Within(0.0001f));
            Assert.That(d.critDamage, Is.EqualTo(1.52f).Within(0.0001f));
            Assert.That(d.attackSpeed, Is.EqualTo(1.012f).Within(0.0001f));
        }

        [Test]
        public void EnemyScaling_Level1Normal_MatchesSheet()
        {
            EnemyScaling.Result r = EnemyScaling.Compute(1, EnemyTier.Normal, _warden, _t);
            Assert.That(r.maxHealth, Is.EqualTo(155));
            Assert.That(r.attack, Is.EqualTo(35f));
            Assert.That(r.defense, Is.EqualTo(2f));
            Assert.That(r.xp, Is.EqualTo(6));
        }

        [Test]
        public void EliteAndBoss_Multipliers()
        {
            EnemyScaling.Result n = EnemyScaling.Compute(10, EnemyTier.Normal, _warden, _t);
            EnemyScaling.Result e = EnemyScaling.Compute(10, EnemyTier.Elite, _warden, _t);
            EnemyScaling.Result b = EnemyScaling.Compute(10, EnemyTier.Boss, _warden, _t);
            Assert.That((float)e.maxHealth / n.maxHealth, Is.EqualTo(4f).Within(0.05f));
            Assert.That((float)b.maxHealth / n.maxHealth, Is.EqualTo(40f).Within(0.05f));
            Assert.That(b.xp, Is.GreaterThan(e.xp));
        }

        [Test]
        public void Serialization_RoundTrips()
        {
            var p = new Progression(_t);
            p.AddXp(500);
            p.Allocate(Stat.SEN, 2);
            var q = JsonUtility.FromJson<Progression>(JsonUtility.ToJson(p));
            Assert.That(q.level, Is.EqualTo(p.level));
            Assert.That(q.xp, Is.EqualTo(p.xp));
            Assert.That(q.freePoints, Is.EqualTo(p.freePoints));
            Assert.That(q.allocated.sense, Is.EqualTo(2f));
        }
    }

    /// <summary>Class numbers from the Balancing sheet, built in code so tests do not depend on assets.</summary>
    static class TestClasses
    {
        public static ClassData Warden()
        {
            var c = ScriptableObject.CreateInstance<ClassData>();
            c.id = "vanguard";
            c.baseStats = new StatBlock(12f, 6f, 3f, 11f, 4f);
            c.growth = new StatBlock(2f, 0.6f, 0.2f, 1.8f, 0.4f);
            c.recommendedSplit = new StatBlock(0.5f, 0.1f, 0f, 0.35f, 0.05f);
            return c;
        }
    }
}
