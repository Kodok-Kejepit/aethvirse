using Aethvirse.Core;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class MusicTests
    {
        [Test]
        public void Triads_FollowTheScale()
        {
            Assert.That(Music.Triad(45, true, 0), Is.EqualTo(new[] { 45, 48, 52 }), "A minor");
            Assert.That(Music.Triad(48, false, 0), Is.EqualTo(new[] { 48, 52, 55 }), "C major");
            Assert.That(Music.Triad(48, false, 4), Is.EqualTo(new[] { 55, 59, 62 }), "G major (V of C)");
        }

        [Test]
        public void EveryRegionHasATheme_WithFourChords()
        {
            foreach (string id in new[] { "r1_ashfall", "r2_verdant", "r7_throne", "s1_glasswood", "k3_last_city", "z1_seat", "Academy" })
                Assert.That(Music.Theme(id).chords.Length, Is.EqualTo(4), id);
            Assert.That(Music.Theme("r1_ashfall").minor, Is.True);
            Assert.That(Music.Theme("r2_verdant").minor, Is.False);
        }
    }
}
