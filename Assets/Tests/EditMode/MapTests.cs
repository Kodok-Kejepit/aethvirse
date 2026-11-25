using Aethvirse.World;
using NUnit.Framework;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class MapTests
    {
        [Test]
        public void Reveal_MarksACircle_AndSurvivesASave()
        {
            var m = new MapMemory();
            Assert.That(m.IsRevealed("r1", 26, 26), Is.False);
            int n = m.Reveal("r1", Vector3.zero, 28f);
            Assert.That(n, Is.GreaterThan(50));
            Assert.That(m.Reveal("r1", Vector3.zero, 28f), Is.EqualTo(0), "already seen");
            Vector2Int c = MapMemory.Cell(Vector3.zero);
            Assert.That(m.IsRevealed("r1", c.x, c.y), Is.True);
            Assert.That(m.IsRevealed("r2", c.x, c.y), Is.False, "per region");
            var back = JsonUtility.FromJson<MapMemory>(JsonUtility.ToJson(m));
            Assert.That(back.IsRevealed("r1", c.x, c.y), Is.True);
            Assert.That(back.Explored("r1"), Is.EqualTo(m.Explored("r1")).Within(1e-6));
        }

        [Test]
        public void Cell_ClampsToTheGrid()
        {
            Assert.That(MapMemory.Cell(new Vector3(-999f, 0f, 999f)), Is.EqualTo(new Vector2Int(0, MapMemory.Grid - 1)));
        }
    }
}
