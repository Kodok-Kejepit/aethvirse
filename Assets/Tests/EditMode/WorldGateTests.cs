using System.Collections.Generic;
using System.Linq;
using Aethvirse.Enemies;
using Aethvirse.Gates;
using Aethvirse.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class WorldGateTests
    {
        [Test]
        public void Dungeon_RoomCountGrowsWithRank()
        {
            for (int rank = 0; rank <= 5; rank++)
            {
                DungeonLayout d = DungeonLayout.Generate(1000 + rank, rank, false);
                Assert.That(d.rooms.Count, Is.EqualTo(DungeonLayout.RoomCount(rank)), $"rank {rank} ({d.shape})");
                Assert.That(d.rooms[0].kind, Is.EqualTo(RoomKind.Start));
                Assert.That(d.rooms[d.rooms.Count - 1].kind, Is.EqualTo(RoomKind.Boss));
            }
        }

        [Test]
        public void Dungeon_EveryRoomIsReachableAndNoCellIsShared()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                DungeonLayout d = DungeonLayout.Generate(seed, seed % 6, seed % 3 == 0);
                Assert.That(d.rooms.Select(r => r.cell).Distinct().Count(), Is.EqualTo(d.rooms.Count), $"seed {seed} {d.shape}: overlapping rooms");
                var reached = new HashSet<int> { 0 };
                bool grew = true;
                while (grew)
                {
                    grew = false;
                    foreach (DungeonLayout.Corridor c in d.corridors)
                    {
                        if (reached.Contains(c.from) && reached.Add(c.to)) grew = true;
                        if (reached.Contains(c.to) && reached.Add(c.from)) grew = true;
                    }
                }
                Assert.That(reached.Count, Is.EqualTo(d.rooms.Count), $"seed {seed} {d.shape}: unreachable rooms");
            }
        }

        [Test]
        public void Dungeon_SameSeedSameMap_SecretOnlyWhenAsked()
        {
            DungeonLayout a = DungeonLayout.Generate(42, 3, true), b = DungeonLayout.Generate(42, 3, true);
            Assert.That(a.shape, Is.EqualTo(b.shape));
            Assert.That(a.rooms.Select(r => r.cell), Is.EqualTo(b.rooms.Select(r => r.cell)));
            Assert.That(a.rooms.Count(r => r.kind == RoomKind.Secret), Is.EqualTo(1));
            Assert.That(a.corridors.Count(c => c.hidden), Is.EqualTo(1));
            Assert.That(DungeonLayout.Generate(42, 3, false).rooms.Count(r => r.kind == RoomKind.Secret), Is.EqualTo(0));
        }

        [Test]
        public void WorldGates_RollOnScheduleUpToTwoAndBreakAfterADay()
        {
            var g = new WorldGates();
            var rng = new System.Random(3);
            Assert.That(g.Roll("r1", 4, 0, 0f, rng), Is.Null, "first call only starts the clock");
            float t = 0f;
            for (int i = 0; i < 60; i++) { t += WorldGates.RollEveryMinutes; g.Roll("r1", 4, 0, t, rng); }
            Assert.That(g.In("r1").Count(), Is.EqualTo(WorldGates.MaxPerRegion));
            Assert.That(g.Roll("r1", 4, 0, t + 1f, rng), Is.Null, "not due yet");
            List<OpenGate> broke = g.CheckBreaks(t + WorldGates.BreakAfterMinutes * 2f);
            Assert.That(broke.Count, Is.EqualTo(2));
            g.Close(broke[0].uid);
            Assert.That(g.In("r1").Count(), Is.EqualTo(1));
        }

        [Test]
        public void Atlas_RegionsInOrderWithEnemiesThatExist()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/Resources/EnemyCatalog.asset");
            Assert.That(atlas, Is.Not.Null);
            List<RegionData> regions = atlas.AllRegions().ToList();
            Assert.That(regions.Count(r => !r.hidden), Is.EqualTo(7), "regions I–VII");
            Assert.That(regions.Count(r => r.hidden), Is.EqualTo(1), "[ ??? ]");
            for (int i = 1; i < regions.Count; i++) Assert.That(regions[i].levelMin, Is.EqualTo(regions[i - 1].levelMax), "levels continue");
            foreach (RegionData r in regions)
                foreach (string e in r.enemies) Assert.That(catalog.Get(e), Is.Not.Null, $"{r.id}: {e}");
            Assert.That(atlas.galaxies.Count, Is.GreaterThanOrEqualTo(2), "room for the planets / galaxies arc");
        }
    }
}
