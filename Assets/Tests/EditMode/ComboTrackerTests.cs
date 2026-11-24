using Aethvirse.Combat;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class ComboTrackerTests
    {
        [TestCase(0, ComboRank.None), TestCase(1, ComboRank.Spark), TestCase(4, ComboRank.Spark), TestCase(5, ComboRank.Ember),
         TestCase(10, ComboRank.Flame), TestCase(20, ComboRank.Blaze), TestCase(35, ComboRank.Inferno), TestCase(60, ComboRank.Phoenix)]
        public void RankThresholds(int hits, ComboRank rank) => Assert.That(ComboTracker.RankForCount(hits), Is.EqualTo(rank));

        [Test]
        public void HitsWithinTimeout_Chain()
        {
            var t = new ComboTracker(3f);
            for (int i = 0; i < 6; i++) t.RegisterHit(i);
            Assert.That(t.Count, Is.EqualTo(6));
            Assert.That(t.Rank(5f), Is.EqualTo(ComboRank.Ember));
        }

        [Test]
        public void Timeout_ResetsCount()
        {
            var t = new ComboTracker(3f);
            t.RegisterHit(0f);
            t.RegisterHit(1f);
            t.RegisterHit(10f);
            Assert.That(t.Count, Is.EqualTo(1));
            Assert.That(t.Best, Is.EqualTo(2));
            Assert.That(t.Rank(20f), Is.EqualTo(ComboRank.None));
        }

        [Test]
        public void Heat_GrowsWithTier()
        {
            Assert.That(ComboTracker.Heat(ComboRank.None), Is.EqualTo(0f));
            Assert.That(ComboTracker.Heat(ComboRank.Flame), Is.GreaterThan(ComboTracker.Heat(ComboRank.Ember)));
            Assert.That(ComboTracker.Heat(ComboRank.Phoenix), Is.EqualTo(1f));
        }

        [Test]
        public void Break_ClearsCount()
        {
            var t = new ComboTracker(3f);
            t.RegisterHit(0f);
            t.Break();
            Assert.That(t.Count, Is.EqualTo(0));
        }
    }
}
