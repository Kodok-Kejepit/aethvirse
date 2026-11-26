using System.Linq;
using Aethvirse.Core;
using Aethvirse.World;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class AchievementTests
    {
        [Test]
        public void Reached_OnlyNewOnesAtTheirThreshold()
        {
            var f = new WorldFlags();
            Assert.That(Achievements.Reached("kills", 0, f.Has), Is.Empty);
            var first = Achievements.Reached("kills", 1, f.Has);
            Assert.That(first.Select(d => d.id), Is.EquivalentTo(new[] { "first_blood" }));
            f.SetFlag(Achievements.Flag("first_blood"));
            Assert.That(Achievements.Reached("kills", 150, f.Has).Select(d => d.id), Is.EquivalentTo(new[] { "hunter_100" }));
        }

        [Test]
        public void Definitions_AreUniqueAndUseKnownStats()
        {
            Assert.That(Achievements.All.Length, Is.GreaterThanOrEqualTo(30));
            Assert.That(Achievements.All.Select(d => d.id).Distinct().Count(), Is.EqualTo(Achievements.All.Length));
            foreach (var d in Achievements.All)
            {
                Assert.That(Items.StatKeys.All, Does.Contain(d.stat), d.id);
                Assert.That(d.need, Is.GreaterThan(0), d.id);
                Assert.That(d.title.Length, Is.GreaterThan(2), d.id);
            }
        }
    }
}
