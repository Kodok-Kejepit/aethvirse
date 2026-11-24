using Aethvirse.Endgame;
using Aethvirse.World;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class EndgameTests
    {
        [TestCase(0, 0, 'A'), TestCase(25, 5, 'B'), TestCase(25, 30, 'C'), TestCase(0, 31, 'C')]
        public void Endings_Pick(int shadows, int secrets, char ending) => Assert.That(Endings.Pick(shadows, secrets), Is.EqualTo(ending));

        [Test]
        public void HollowGate_OnlyAtThreeWithAllKeys()
        {
            var f = new WorldFlags();
            foreach (string k in Endings.Keys) f.SetFlag(k);
            Assert.That(HollowGate.Open(f, 3), Is.True);
            Assert.That(HollowGate.Open(f, 4), Is.False);
            f.Clear(Endings.Keys[1]);
            Assert.That(HollowGate.Open(f, 3), Is.False);
        }
    }
}
