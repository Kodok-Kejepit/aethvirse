using System.IO;
using Aethvirse.Interaction;
using Aethvirse.Save;
using Aethvirse.Stats;
using Aethvirse.World;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class SaveTests
    {
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "aethvirse_save_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void SaveFile_RoundTripsAndKeepsBackup()
        {
            string path = Path.Combine(_dir, "slot1.asav");
            SaveFile.Write(path, "{\"a\":1}");
            SaveFile.Write(path, "{\"a\":2}");
            Assert.That(SaveFile.Read(path), Is.EqualTo("{\"a\":2}"));
            Assert.That(SaveFile.Read(path + ".bak"), Is.EqualTo("{\"a\":1}"));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void SaveFile_RejectsMissingForeignTruncatedAndCorrupted()
        {
            string path = Path.Combine(_dir, "slot1.asav");
            Assert.That(SaveFile.Read(path), Is.Null, "missing");
            File.WriteAllText(path, "not a save");
            Assert.That(SaveFile.Read(path), Is.Null, "foreign");
            SaveFile.Write(path, new string('x', 5000));
            byte[] bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, bytes[..(bytes.Length / 2)]);
            Assert.That(SaveFile.Read(path), Is.Null, "truncated");
            SaveFile.Write(path, "{\"hello\":\"world\"}");
            bytes = File.ReadAllBytes(path);
            bytes[20] ^= 0xFF;
            File.WriteAllBytes(path, bytes);
            Assert.That(SaveFile.Read(path), Is.Null, "corrupted checksum");
        }

        [Test]
        public void SaveData_RoundTripsThroughJson()
        {
            var tuning = ScriptableObject.CreateInstance<StatTuning>();
            var data = new SaveData { scene = "Playground", playtime = 123f };
            data.player.classId = "vanguard";
            data.player.progression = new Progression(tuning);
            data.player.progression.AddXp(500);
            data.player.skillIds.Add("iron_charge");
            data.player.skillLevels.Add(3);
            data.player.slots[0] = "iron_charge";
            data.player.position = new Vector3(1f, 2f, 3f);
            data.player.hasPosition = true;
            data.world.clock.Advance(600f);
            data.world.weather.current = WeatherKind.Fog;
            data.world.flags.SetFlag("academy_entered");
            data.world.gold = 42;

            SaveData back = SaveData.FromJson(data.ToJson());
            Assert.That(back, Is.Not.Null);
            Assert.That(back.player.progression.level, Is.EqualTo(data.player.progression.level));
            Assert.That(back.player.skillLevels[0], Is.EqualTo(3));
            Assert.That(back.player.slots[0], Is.EqualTo("iron_charge"));
            Assert.That(back.player.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(back.world.clock.Format(), Is.EqualTo(data.world.clock.Format()));
            Assert.That(back.world.weather.current, Is.EqualTo(WeatherKind.Fog));
            Assert.That(back.world.flags.Has("academy_entered"), Is.True);
            Assert.That(back.world.gold, Is.EqualTo(42));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void SaveData_RejectsGarbageAndFutureVersions()
        {
            Assert.That(SaveData.FromJson(null), Is.Null);
            Assert.That(SaveData.FromJson("{}"), Is.Null, "version 0");
            Assert.That(SaveData.FromJson("{\"version\":999}"), Is.Null, "from a newer game");
        }

        [Test]
        public void Interaction_PrefersInFrontAndInRange()
        {
            float ahead = InteractionDetector.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1.5f), 2f);
            float behind = InteractionDetector.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1.2f), 2f);
            float far = InteractionDetector.Score(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 3f), 2f);
            Assert.That(ahead, Is.LessThan(behind), "1.5 m ahead beats 1.2 m behind");
            Assert.That(float.IsPositiveInfinity(far), Is.True);
        }
    }
}
