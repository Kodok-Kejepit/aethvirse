using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.World
{
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
        [Tooltip("World flag also needed (the previous region's Mystery Journal page full).")]
        public string requiredFlag = "";
        public Color palette = new Color(0.45f, 0.45f, 0.45f);
        public WeatherTable weather = new WeatherTable();
        public List<WaystoneInfo> waystones = new List<WaystoneInfo>();
        public List<Vector3> gateSpots = new List<Vector3>();
        [Tooltip("Enemy types that roam here (Enemies.EnemyData ids).")]
        public List<string> enemies = new List<string>();
        public List<string> neighbours = new List<string>();

        public int LevelAt(float t) => Mathf.RoundToInt(Mathf.Lerp(levelMin, levelMax, Mathf.Clamp01(t)));
    }
}
