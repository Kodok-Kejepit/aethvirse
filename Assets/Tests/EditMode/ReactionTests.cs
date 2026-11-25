using Aethvirse.Combat;
using Aethvirse.Enemies;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class ReactionTests
    {
        [TestCase(ElementType.Fire, ElementType.Ice, ReactionType.Melt)]
        [TestCase(ElementType.Ice, ElementType.Fire, ReactionType.Melt)]
        [TestCase(ElementType.Fire, ElementType.Wind, ReactionType.Firestorm)]
        [TestCase(ElementType.Earth, ElementType.Fire, ReactionType.Magma)]
        [TestCase(ElementType.Lightning, ElementType.Fire, ReactionType.Overload)]
        [TestCase(ElementType.Ice, ElementType.Wind, ReactionType.Blizzard)]
        [TestCase(ElementType.Ice, ElementType.Earth, ReactionType.Permafrost)]
        [TestCase(ElementType.Ice, ElementType.Lightning, ReactionType.Superconduct)]
        [TestCase(ElementType.Wind, ElementType.Earth, ReactionType.Sandstorm)]
        [TestCase(ElementType.Wind, ElementType.Lightning, ReactionType.Thunderstorm)]
        [TestCase(ElementType.Earth, ElementType.Lightning, ReactionType.Magnetize)]
        [TestCase(ElementType.Shadow, ElementType.Light, ReactionType.Eclipse)]
        [TestCase(ElementType.Shadow, ElementType.Fire, ReactionType.Corrode)]
        [TestCase(ElementType.Ice, ElementType.Light, ReactionType.Purge)]
        [TestCase(ElementType.Fire, ElementType.Void, ReactionType.Erase)]
        [TestCase(ElementType.Fire, ElementType.Fire, ReactionType.None)]
        [TestCase(ElementType.Physical, ElementType.Fire, ReactionType.None)]
        public void Table(ElementType aura, ElementType hit, ReactionType expected) =>
            Assert.That(Reactions.Resolve(aura, hit, false, false), Is.EqualTo(expected));

        [Test]
        public void HeavyPhysical_ShattersFrozen()
        {
            Assert.That(Reactions.Resolve(ElementType.Physical, ElementType.Physical, true, true), Is.EqualTo(ReactionType.Shatter));
            Assert.That(Reactions.Resolve(ElementType.Physical, ElementType.Physical, true, false), Is.EqualTo(ReactionType.None));
            Assert.That(Reactions.Multiplier(ReactionType.Shatter), Is.EqualTo(2.5f));
            Assert.That(Reactions.Multiplier(ReactionType.Melt), Is.EqualTo(2f));
        }

        [Test]
        public void ThreeChills_Freeze_AndFreezeDisables()
        {
            var s = new StatusSet();
            s.Apply(StatusType.Chill, 4f);
            s.Apply(StatusType.Chill, 4f);
            Assert.That(s.Disabled, Is.False);
            Assert.That(s.SpeedFactor, Is.LessThan(1f));
            s.Apply(StatusType.Chill, 4f);
            Assert.That(s.Has(StatusType.Freeze), Is.True);
            Assert.That(s.Has(StatusType.Chill), Is.False);
            Assert.That(s.Disabled, Is.True);
            s.Tick(StatusSet.FreezeTime + 0.1f, 100);
            Assert.That(s.Disabled, Is.False);
        }

        [Test]
        public void Burn_DealsDamageOverTime_AndStacksToThree()
        {
            var s = new StatusSet();
            for (int i = 0; i < 5; i++) s.Apply(StatusType.Burn, 3f);
            Assert.That(s.Stacks(StatusType.Burn), Is.EqualTo(3));
            Assert.That(s.Tick(0.25f, 10000), Is.EqualTo(0), "ticks twice a second");
            Assert.That(s.Tick(0.25f, 10000), Is.EqualTo(180));
        }

        [Test]
        public void Exposed_And_Corrode_RaiseDamageTaken()
        {
            var s = new StatusSet();
            Assert.That(s.DamageTakenFactor, Is.EqualTo(1f));
            s.Apply(StatusType.Exposed, 6f);
            s.Apply(StatusType.Corrode, 6f);
            Assert.That(s.DamageTakenFactor, Is.EqualTo(1.2f * 1.35f).Within(0.001f));
        }

        [Test]
        public void EliteRoll_GivesOneOrTwoDifferentAffixes()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                var a = EliteAffixes.Roll(rng);
                Assert.That(a.Count, Is.InRange(1, 2));
                if (a.Count == 2) Assert.That(a[0], Is.Not.EqualTo(a[1]));
            }
            Assert.That(EliteAffixes.Prefix(new[] { EliteAffix.Berserk, EliteAffix.Shielded }), Is.EqualTo("Berserk Shielded "));
        }
    }
}
