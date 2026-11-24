using Aethvirse.Combat;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class AttackTokensTests
    {
        [SetUp] public void SetUp() { AttackTokens.Clear(); AttackTokens.MaxTokens = 2; }
        [TearDown] public void TearDown() => AttackTokens.Clear();

        [Test]
        public void OnlyMaxAttackersAtOnce()
        {
            object a = new object(), b = new object(), c = new object();
            Assert.That(AttackTokens.Request(a), Is.True);
            Assert.That(AttackTokens.Request(b), Is.True);
            Assert.That(AttackTokens.Request(c), Is.False, "third attacker waits");
            AttackTokens.Release(a);
            Assert.That(AttackTokens.Request(c), Is.True);
        }

        [Test]
        public void HolderAsksTwice_KeepsOneToken()
        {
            object a = new object();
            AttackTokens.Request(a);
            Assert.That(AttackTokens.Request(a), Is.True);
            Assert.That(AttackTokens.ActiveCount, Is.EqualTo(1));
        }
    }
}
