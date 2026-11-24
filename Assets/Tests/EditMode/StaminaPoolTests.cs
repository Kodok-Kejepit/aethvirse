using Aethvirse.Player;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class StaminaPoolTests
    {
        StaminaPool _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new StaminaPool { maxStamina = 100f, regenPerSecond = 28f, regenDelay = 0.8f, exhaustedRecoverRatio = 0.3f };
            _pool.Reset();
        }

        [Test]
        public void StartsFull() => Assert.That(_pool.Current, Is.EqualTo(100f));

        [Test]
        public void SpendClampsAtZeroAndExhausts()
        {
            Assert.That(_pool.TrySpend(150f), Is.True, "allowed while any stamina is left");
            Assert.That(_pool.Current, Is.EqualTo(0f));
            Assert.That(_pool.Exhausted, Is.True);
            Assert.That(_pool.TrySpend(1f), Is.False);
        }

        [Test]
        public void RegenWaitsForDelay()
        {
            _pool.TrySpend(50f);
            _pool.Tick(0.5f);
            Assert.That(_pool.Current, Is.EqualTo(50f), "still in the regen delay");
            _pool.Tick(0.3f);
            _pool.Tick(1f);
            Assert.That(_pool.Current, Is.EqualTo(78f).Within(0.01f));
        }

        [Test]
        public void ExhaustionLiftsAtRecoverRatio()
        {
            _pool.TrySpend(100f);
            _pool.Tick(0.8f);
            _pool.Tick(1f);
            Assert.That(_pool.Exhausted, Is.True, "28 < 30");
            _pool.Tick(0.1f);
            Assert.That(_pool.Exhausted, Is.False);
            Assert.That(_pool.CanSpend, Is.True);
        }

        [Test]
        public void RegenStopsAtMax()
        {
            _pool.TrySpend(10f);
            _pool.Tick(0.8f);
            _pool.Tick(10f);
            Assert.That(_pool.Current, Is.EqualTo(100f));
        }

        [Test]
        public void DrainReportsWhenEmpty()
        {
            Assert.That(_pool.Drain(50f, 1f), Is.True);
            Assert.That(_pool.Drain(50f, 1f), Is.False);
        }

        [Test]
        public void ChangedEventFires()
        {
            float seen = -1f;
            _pool.Changed += (current, _) => seen = current;
            _pool.TrySpend(20f);
            Assert.That(seen, Is.EqualTo(80f));
        }
    }
}
