using System.Linq;
using Aethvirse.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Aethvirse.Tests
{
    public sealed class StreamingTests
    {
        [Test]
        public void RegionsOfAWorld_SitSideBySide_AndNeighboursShareAWorld()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            foreach (GalaxyInfo g in atlas.galaxies)
                foreach (PlanetInfo p in g.planets)
                {
                    var visible = p.regions.Where(r => !r.hidden).ToList();
                    for (int i = 0; i < visible.Count; i++)
                    {
                        Assert.That(visible[i].origin, Is.EqualTo(new Vector3(0f, 0f, i * WorldStreamer.Spacing)), visible[i].id);
                        foreach (string n in visible[i].neighbours)
                            Assert.That(p.regions.Any(r => r.id == n), Is.True, visible[i].id + " → " + n + " must be in the same world");
                    }
                }
        }

        [Test]
        public void DistanceTo_IsZeroInside_AndGrowsOutside()
        {
            var r = ScriptableObject.CreateInstance<RegionData>();
            r.origin = new Vector3(0f, 0f, 300f);
            Assert.That(WorldStreamer.DistanceTo(r, new Vector3(0f, 0f, 300f)), Is.EqualTo(0f));
            Assert.That(WorldStreamer.DistanceTo(r, new Vector3(0f, 0f, 160f)), Is.EqualTo(0f));
            Assert.That(WorldStreamer.DistanceTo(r, new Vector3(0f, 0f, 100f)), Is.EqualTo(50f).Within(1e-3));
        }

        [Test]
        public void RegionScenes_SaveTheRigInactive_WithAnActivator()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>("Assets/Resources/WorldAtlas.asset");
            var scenes = atlas.AllRegions().Where(r => !r.hidden && !string.IsNullOrEmpty(r.scene)).Select(r => r.scene).ToList();
            foreach (string sc in scenes.Take(3))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene($"Assets/Scenes/Regions/{sc}.unity");
                var roots = scene.GetRootGameObjects();
                var rig = roots.Where(x => x.GetComponent<SceneRig>() != null).ToList();
                Assert.That(rig.Count, Is.GreaterThan(3), sc);
                Assert.That(rig.All(x => !x.activeSelf), Is.True, sc + ": the rig must be saved inactive");
                Assert.That(roots.Any(x => x.GetComponent<RigActivator>() != null), Is.True, sc);
            }
        }
    }
}
