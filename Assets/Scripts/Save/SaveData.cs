using System;
using System.Collections.Generic;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Save
{
    /// <summary>
    /// Everything a save holds (JsonUtility DTO). <see cref="version"/> drives <see cref="Migrate"/>; later systems
    /// (inventory, quests, secrets, world gates …) add their own sections here.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAtUtc = "";
        public string scene = "";
        public float playtime;

        [Serializable]
        public sealed class PlayerSection
        {
            public string classId = "";
            public Progression progression;
            public List<string> skillIds = new List<string>();
            public List<int> skillLevels = new List<int>();
            public string[] slots = new string[PlayerStats.SlotCount];
            public Vector3 position;
            public bool hasPosition;
            public float health = 1f;
            public float mana = 1f;
        }

        [Serializable]
        public sealed class WorldSection
        {
            public WorldClock clock = new WorldClock();
            public WeatherState weather = new WeatherState();
            public WorldFlags flags = new WorldFlags();
            public int gold;
            public string region = "";
        }

        public PlayerSection player = new PlayerSection();
        public WorldSection world = new WorldSection();

        public string ToJson() => JsonUtility.ToJson(this);

        /// <summary>Parses and migrates; null if the text is not a save.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { return null; }
            if (data == null || data.version <= 0 || data.version > CurrentVersion) return null;
            data.Migrate();
            data.world.flags.Rebuild();
            return data;
        }

        /// <summary>Upgrades older saves step by step (none yet: version 1 is the first Unity save).</summary>
        void Migrate()
        {
            version = CurrentVersion;
        }
    }
}
