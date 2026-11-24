using Aethvirse.Skills;
using Aethvirse.Stats;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class ManaBuffSkillTests
    {
        [Test]
        public void Mana_PayRegenAndCap()
        {
            var m = new ManaPool { regenPerSecond = 10f };
            m.SetMax(50f, fill: true);
            Assert.That(m.TryPay(30f), Is.True);
            Assert.That(m.TryPay(30f), Is.False, "only 20 left");
            m.Tick(1f);
            Assert.That(m.Current, Is.EqualTo(30f));
            m.Tick(10f);
            Assert.That(m.Current, Is.EqualTo(50f));
        }

        [Test]
        public void Buffs_ReplaceAndExpire()
        {
            var b = new BuffSet();
            b.Add(BuffStat.Attack, 0.2f, 10f, now: 0f);
            b.Add(BuffStat.Attack, 0.3f, 10f, now: 1f);
            Assert.That(b.Total(BuffStat.Attack), Is.EqualTo(0.3f), "a new buff on the same stat replaces the old one");
            b.Add(BuffStat.Defense, 0.1f, 2f, now: 1f);
            Assert.That(b.Expire(5f), Is.True);
            Assert.That(b.Total(BuffStat.Defense), Is.EqualTo(0f));
            Assert.That(b.Total(BuffStat.Attack), Is.EqualTo(0.3f));
            Assert.That(b.Remaining(BuffStat.Attack, 5f), Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void Skill_ScalesWithLevel()
        {
            var s = ScriptableObject.CreateInstance<SkillData>();
            s.damageMultiplier = 2.2f;
            s.multiplierPerLevel = 0.15f;
            s.buffAmount = 0.2f;
            s.buffAmountPerLevel = 0.02f;
            Assert.That(s.MultiplierAt(1), Is.EqualTo(2.2f).Within(0.0001f));
            Assert.That(s.MultiplierAt(5), Is.EqualTo(2.8f).Within(0.0001f));
            Assert.That(s.BuffAt(3), Is.EqualTo(0.24f).Within(0.0001f));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void SkillCombo_MatchesOrderAndWindow()
        {
            var c = new SkillComboData { first = "iron_charge", second = "rending_slash", window = 2f };
            Assert.That(c.Matches("iron_charge", 1.5f, "rending_slash"), Is.True);
            Assert.That(c.Matches("iron_charge", 2.5f, "rending_slash"), Is.False, "too late");
            Assert.That(c.Matches("rending_slash", 1f, "iron_charge"), Is.False, "wrong order");
        }
    }
}
