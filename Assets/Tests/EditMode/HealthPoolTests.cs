using Aethvirse.Combat;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class HealthPoolTests
    {
        static HealthPool Make(int max = 100, bool immortal = false)
        {
            var h = new HealthPool { maxHealth = max, immortal = immortal, immortalRefillDelay = 3f };
            h.Reset();
            return h;
        }

        [Test]
        public void Damage_ReducesAndKills()
        {
            HealthPool h = Make();
            bool died = false;
            h.Died += () => died = true;
            Assert.That(h.TakeDamage(new DamageInfo { amount = 40 }), Is.True);
            Assert.That(h.Current, Is.EqualTo(60));
            h.TakeDamage(new DamageInfo { amount = 100 });
            Assert.That(h.Current, Is.EqualTo(0));
            Assert.That(died, Is.True);
            Assert.That(h.TakeDamage(new DamageInfo { amount = 5 }), Is.False, "dead takes no more hits");
        }

        [Test]
        public void Invulnerable_ReportsDodge()
        {
            HealthPool h = Make();
            bool dodged = false;
            h.Dodged += _ => dodged = true;
            h.Invulnerable = true;
            Assert.That(h.TakeDamage(new DamageInfo { amount = 40 }), Is.False);
            Assert.That(h.Current, Is.EqualTo(100));
            Assert.That(dodged, Is.True);
        }

        [Test]
        public void Immortal_StaysAtOneAndRefills()
        {
            HealthPool h = Make(immortal: true);
            h.TakeDamage(new DamageInfo { amount = 999 });
            Assert.That(h.Current, Is.EqualTo(1));
            Assert.That(h.IsDead, Is.False);
            h.Tick(2f);
            Assert.That(h.Current, Is.EqualTo(1));
            h.Tick(1.1f);
            Assert.That(h.Current, Is.EqualTo(100));
        }

        [Test]
        public void Heal_ClampsToMax()
        {
            HealthPool h = Make();
            h.TakeDamage(new DamageInfo { amount = 30 });
            h.Heal(50);
            Assert.That(h.Current, Is.EqualTo(100));
        }
    }
}
