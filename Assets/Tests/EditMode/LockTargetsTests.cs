using Aethvirse.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class LockTargetsTests
    {
        [Test]
        public void Score_PrefersTargetsInView()
        {
            float ahead = LockTargets.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 6f), 8f);
            float side = LockTargets.Score(Vector3.zero, Vector3.forward, new Vector3(6f, 0f, 0f), 8f);
            Assert.That(ahead, Is.LessThan(side));
        }

        [Test]
        public void Score_PrefersCloserAtSameAngle()
        {
            float near = LockTargets.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 3f), 8f);
            float far = LockTargets.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 9f), 8f);
            Assert.That(near, Is.LessThan(far));
        }

        [Test]
        public void Score_IgnoresHeight() =>
            Assert.That(LockTargets.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 5f, 4f), 8f), Is.EqualTo(4f).Within(0.001f));
    }
}
