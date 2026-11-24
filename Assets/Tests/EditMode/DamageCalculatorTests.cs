using Aethvirse.Combat;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class DamageCalculatorTests
    {
        [Test]
        public void Formula_MatchesSpec() =>
            // (100 × 1.5 − 20 × 0.5) × 1.5 × 1.5 × 1 × 1 = 140 × 2.25 = 315
            Assert.That(DamageCalculator.Compute(100f, 1.5f, 20f, 1.5f, 1.5f, 1f, 1f), Is.EqualTo(315));

        [Test]
        public void MinimumDamage_IsOne() =>
            Assert.That(DamageCalculator.Compute(1f, 1f, 999f, 1f, 1f, 1f, 1f), Is.EqualTo(1));

        [Test]
        public void Variance_StaysInRange()
        {
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++)
                Assert.That(DamageCalculator.RollVariance(rng), Is.InRange(0.95f, 1.05f));
        }

        [Test]
        public void Resolve_AppliesElementAndForcedCrit()
        {
            var attacker = new CombatStats { attack = 100f, critChance = 0f, critDamage = 2f };
            var defender = new CombatStats { defense = 0f, element = ElementType.Ice };
            DamageInfo info = DamageCalculator.Resolve(attacker, defender, 1f, ElementType.Fire, new System.Random(1), forceCrit: true);
            Assert.That(info.isCrit, Is.True);
            // 100 × 2 (crit) × 1.5 (fire vs ice) = 300 ± 5%
            Assert.That(info.amount, Is.InRange(285, 315));
        }

        [Test]
        public void CritRoll_RespectsChance()
        {
            var rng = new System.Random(3);
            Assert.That(DamageCalculator.RollCrit(rng, 0f), Is.False);
            Assert.That(DamageCalculator.RollCrit(rng, 1f), Is.True);
        }
    }
}
