using System.Linq;
using Aethvirse.Endgame;
using Aethvirse.Enemies;
using Aethvirse.World;
using NUnit.Framework;
using UnityEditor;

namespace Aethvirse.Tests
{
    public sealed class WorldsTests
    {
        [Test]
        public void Atlas_HasThreeGalaxiesFourWorldsAndTheSeat()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            Assert.That(atlas.galaxies.Count, Is.EqualTo(3));
            Assert.That(atlas.galaxies.Sum(g => g.planets.Count), Is.EqualTo(4));
            Assert.That(atlas.Region("z1_seat"), Is.Not.Null);
            foreach (GalaxyInfo g in atlas.galaxies)
                foreach (PlanetInfo p in g.planets)
                {
                    Assert.That(p.regions.Count, Is.GreaterThan(0), p.id);
                    foreach (RegionData r in p.regions)
                        if (!r.hidden) Assert.That(System.IO.File.Exists($"Assets/Scenes/Regions/{r.scene}.unity"), Is.True, r.scene);
                }
        }

        [Test]
        public void LaterWorlds_StayClosedUntilTheirFlag()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            var flags = new WorldFlags();
            RegionData seren = atlas.Region("s1_glasswood");
            Assert.That(WorldAtlas.IsOpen(seren, 5, flags), Is.False);
            flags.SetFlag(Endings.LastSealFlag);
            Assert.That(WorldAtlas.IsOpen(seren, 5, flags), Is.True);
            Assert.That(WorldAtlas.IsOpen(atlas.Region("z1_seat"), 5, flags), Is.False);
            Assert.That(StarGate.Open(flags, "god_war_opened"), Is.False);
            Assert.That(StarGate.Open(flags, ""), Is.True);
        }

        [Test]
        public void EveryRegionEnemyExists_AndSomeShoot()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/Resources/EnemyCatalog.asset");
            foreach (RegionData r in atlas.AllRegions())
                foreach (string e in r.enemies) Assert.That(catalog.Get(e), Is.Not.Null, r.id + " → " + e);
            Assert.That(catalog.enemies.Count(e => e.IsRanged), Is.GreaterThanOrEqualTo(8));
            foreach (string id in new[] { "the_god", "vanguard", "the_conductor", "kethra_warden", "seraph_husk", "halo_warden" })
                Assert.That(catalog.Get(id), Is.Not.Null, id);
        }

        [TestCase(1f, 0), TestCase(0.7f, 1), TestCase(0.5f, 1), TestCase(0.4f, 2), TestCase(0.1f, 3)]
        public void BossPhases_ByHealth(float ratio, int phase) => Assert.That(BossPhases.For(ratio), Is.EqualTo(phase));
    }
}
