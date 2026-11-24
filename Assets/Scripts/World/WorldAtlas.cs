using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>A fast-travel point inside a region (unlocked by walking past it).</summary>
    [Serializable]
    public sealed class WaystoneInfo
    {
        public string id = "";
        public string displayName = "";
        public Vector3 position;
    }

    /// <summary>A world (planet) — Aethvirse is the first; after the last seal, others open (STORY §6.5).</summary>
    [Serializable]
    public sealed class PlanetInfo
    {
        public string id = "";
        public string displayName = "";
        public List<RegionData> regions = new List<RegionData>();
        [Tooltip("World flag that opens travel to this planet.")]
        public string unlockFlag = "";
    }

    [Serializable]
    public sealed class GalaxyInfo
    {
        public string id = "";
        public string displayName = "";
        public List<PlanetInfo> planets = new List<PlanetInfo>();
        public string unlockFlag = "";
    }

    /// <summary>Galaxy → planet → region (Resources/WorldAtlas).</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/World/Atlas", fileName = "WorldAtlas")]
    public sealed class WorldAtlas : ScriptableObject
    {
        public List<GalaxyInfo> galaxies = new List<GalaxyInfo>();

        static WorldAtlas _loaded;
        public static WorldAtlas Instance => _loaded != null ? _loaded : _loaded = Resources.Load<WorldAtlas>("WorldAtlas");

        public IEnumerable<RegionData> AllRegions()
        {
            foreach (GalaxyInfo g in galaxies)
                foreach (PlanetInfo p in g.planets)
                    foreach (RegionData r in p.regions)
                        if (r != null) yield return r;
        }

        public RegionData Region(string id)
        {
            foreach (RegionData r in AllRegions()) if (r.id == id) return r;
            return null;
        }

        public RegionData RegionForScene(string scene)
        {
            foreach (RegionData r in AllRegions()) if (r.scene == scene) return r;
            return null;
        }

        /// <summary>Road open: rank reached and (later) the previous journal page full.</summary>
        public static bool IsOpen(RegionData r, int academyRank, WorldFlags flags) =>
            r != null && academyRank >= r.requiredRank && (!r.hidden || flags.Has($"region_{r.id}_found"));
    }
}
