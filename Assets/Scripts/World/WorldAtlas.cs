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

    /// <summary>A region (§7): level band, scene, weather, enemies, Gate spots, waystones, neighbours.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/World/Region", fileName = "Region")]
    public sealed class RegionData : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string blurb = "";
        public string scene = "";
        public int levelMin = 1;
        public int levelMax = 50;
        [Tooltip("Academy rank (0 = E … 5 = S) needed before the road opens (the Mystery Journal adds its own lock in P1-11).")]
        public int requiredRank;
        [Tooltip("Hidden regions never show on maps or road signs.")]
        public bool hidden;
        public Color palette = new Color(0.45f, 0.45f, 0.45f);
        public WeatherTable weather = new WeatherTable();
        public List<WaystoneInfo> waystones = new List<WaystoneInfo>();
        public List<Vector3> gateSpots = new List<Vector3>();
        [Tooltip("Enemy types that roam here (Enemies.EnemyData ids).")]
        public List<string> enemies = new List<string>();
        public List<string> neighbours = new List<string>();

        public int LevelAt(float t) => Mathf.RoundToInt(Mathf.Lerp(levelMin, levelMax, Mathf.Clamp01(t)));
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
