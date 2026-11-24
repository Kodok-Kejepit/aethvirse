using System.Collections.Generic;
using Aethvirse.World;
using NUnit.Framework;

namespace Aethvirse.Tests
{
    public sealed class WorldTests
    {
        [Test]
        public void Clock_StartsDayOneAtEight()
        {
            var c = new WorldClock();
            Assert.That(c.Day, Is.EqualTo(1));
            Assert.That(c.Format(), Is.EqualTo("08:00"));
            Assert.That(c.IsNight, Is.False);
        }

        [Test]
        public void Clock_OneRealSecondIsOneGameMinute()
        {
            var c = new WorldClock();
            c.Advance(90f);
            Assert.That(c.Format(), Is.EqualTo("09:30"));
            c.Advance(16f * 60f);
            Assert.That(c.Day, Is.EqualTo(2));
            Assert.That(c.Format(), Is.EqualTo("01:30"));
            Assert.That(c.IsNight, Is.True);
        }

        [Test]
        public void Clock_SetTimeGoesForward()
        {
            var c = new WorldClock();
            c.SetTimeOfDay(6, 0);
            Assert.That(c.Day, Is.EqualTo(2), "06:00 is earlier than 08:00, so it is tomorrow");
            c.SetTimeOfDay(22, 15);
            Assert.That(c.Day, Is.EqualTo(2));
            Assert.That(c.Format(), Is.EqualTo("22:15"));
        }

        [Test]
        public void Clock_IsBetweenWrapsMidnight()
        {
            var c = new WorldClock();
            c.SetTimeOfDay(23, 0);
            Assert.That(c.IsBetween(22, 4), Is.True);
            Assert.That(c.IsBetween(8, 20), Is.False);
        }

        [Test]
        public void Weather_ChangesOnlyWhenDue()
        {
            var table = new WeatherTable { entries = new List<WeatherTable.Entry> { new WeatherTable.Entry { kind = WeatherKind.Rain, weight = 1f } } };
            var w = new WeatherState { changeAt = 100f };
            Assert.That(w.Update(50f, table), Is.False);
            Assert.That(w.Update(100f, table), Is.True);
            Assert.That(w.current, Is.EqualTo(WeatherKind.Rain));
            Assert.That(w.changeAt, Is.GreaterThanOrEqualTo(100f + table.minDuration));
        }

        [Test]
        public void Weather_LockedNeverChanges()
        {
            var table = new WeatherTable { entries = new List<WeatherTable.Entry> { new WeatherTable.Entry { kind = WeatherKind.Storm, weight = 1f } } };
            var w = new WeatherState();
            w.Force(WeatherKind.Fog, 0f, 10f, lockIt: true);
            Assert.That(w.Update(1000f, table), Is.False);
            Assert.That(w.current, Is.EqualTo(WeatherKind.Fog));
        }

        [Test]
        public void Weather_FitsTheRegion()
        {
            var desert = new WeatherTable { entries = new List<WeatherTable.Entry> { new WeatherTable.Entry { kind = WeatherKind.Sandstorm, weight = 1f } } };
            var w = new WeatherState { current = WeatherKind.Snow };
            Assert.That(w.FitTo(desert, 0f), Is.True);
            Assert.That(w.current, Is.EqualTo(WeatherKind.Clear));
        }

        [Test]
        public void Flags_SetCountAndRebuild()
        {
            var f = new WorldFlags();
            string seen = null;
            f.FlagSet += x => seen = x;
            f.SetFlag("met_horne");
            f.SetFlag("met_horne");
            Assert.That(f.flags.Count, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo("met_horne"));
            Assert.That(f.Add("kills_ash_hound", 3), Is.EqualTo(3));
            Assert.That(f.Add("kills_ash_hound"), Is.EqualTo(4));
            var copy = UnityEngine.JsonUtility.FromJson<WorldFlags>(UnityEngine.JsonUtility.ToJson(f));
            copy.Rebuild();
            Assert.That(copy.Has("met_horne"), Is.True);
            Assert.That(copy.Counter("kills_ash_hound"), Is.EqualTo(4));
        }
    }
}
