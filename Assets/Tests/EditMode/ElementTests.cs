using Aethvirse.Combat;
using NUnit.Framework;
using E = Aethvirse.Combat.ElementType;

namespace Aethvirse.Tests
{
    public sealed class ElementTests
    {
        [TestCase(E.Fire, E.Ice), TestCase(E.Ice, E.Wind), TestCase(E.Wind, E.Earth), TestCase(E.Earth, E.Lightning), TestCase(E.Lightning, E.Fire)]
        public void NaturalCycle_Strong(E a, E d) => Assert.That(Element.Multiplier(a, d), Is.EqualTo(1.5f));

        [TestCase(E.Ice, E.Fire), TestCase(E.Fire, E.Lightning), TestCase(E.Lightning, E.Earth)]
        public void NaturalCycle_Weak(E a, E d) => Assert.That(Element.Multiplier(a, d), Is.EqualTo(0.75f));

        [TestCase(E.Fire, E.Wind), TestCase(E.Fire, E.Earth)]
        public void UnrelatedNatural_Neutral(E a, E d) => Assert.That(Element.Multiplier(a, d), Is.EqualTo(1f));

        [Test]
        public void SameElement_ResistsExceptPhysical()
        {
            Assert.That(Element.Multiplier(E.Fire, E.Fire), Is.EqualTo(0.75f));
            Assert.That(Element.Multiplier(E.Shadow, E.Shadow), Is.EqualTo(0.75f));
            Assert.That(Element.Multiplier(E.Physical, E.Physical), Is.EqualTo(1f));
        }

        [Test]
        public void ShadowLight_BothStrong()
        {
            Assert.That(Element.Multiplier(E.Shadow, E.Light), Is.EqualTo(1.5f));
            Assert.That(Element.Multiplier(E.Light, E.Shadow), Is.EqualTo(1.5f));
        }

        [Test]
        public void Physical_Neutral()
        {
            Assert.That(Element.Multiplier(E.Physical, E.Fire), Is.EqualTo(1f));
            Assert.That(Element.Multiplier(E.Fire, E.Physical), Is.EqualTo(1f));
        }

        [Test]
        public void Void()
        {
            Assert.That(Element.Multiplier(E.Void, E.Fire), Is.EqualTo(1.25f));
            Assert.That(Element.Multiplier(E.Void, E.Physical), Is.EqualTo(1.25f));
            Assert.That(Element.Multiplier(E.Void, E.Void), Is.EqualTo(0.5f));
            Assert.That(Element.Multiplier(E.Fire, E.Void), Is.EqualTo(1f));
        }
    }
}
