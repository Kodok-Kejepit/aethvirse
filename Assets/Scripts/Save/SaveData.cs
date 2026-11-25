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
        /// <summary>1 = first Unity save; 2 = + inventory; 3 = + quests, academy rank, Codex points; 4 = + world Gates; 5 = + job, shadows.</summary>
        public const int CurrentVersion = 6;

        /// <summary>0 in a fresh object, so JSON without a version (not a save) is refused; set on write.</summary>
        public int version;
        public string savedAtUtc = "";
        public string scene = "";
        public float playtime;

        [Serializable]
        public sealed class PlayerSection
        {
            public string classId = "";
            public string jobId = "";
            public Jobs.ShadowArmy shadows = new Jobs.ShadowArmy();
            public Progression progression;
            public List<string> skillIds = new List<string>();
            public List<int> skillLevels = new List<int>();
            /// <summary>Secret skills learned from books.</summary>
            public List<string> secretSkills = new List<string>();
            /// <summary>Socketed runes: skill id → rune item id (parallel lists).</summary>
            public List<string> runeSkills = new List<string>();
            public List<string> runeItems = new List<string>();
            public string[] slots = new string[PlayerStats.SlotCount];
            public Vector3 position;
            public bool hasPosition;
            public float health = 1f;
            public float mana = 1f;
            /// <summary>JsonUtility never writes null objects, so this says whether <see cref="inventory"/> is real.</summary>
            public bool hasInventory;
            public Items.Inventory inventory = new Items.Inventory();
        }

        [Serializable]
        public sealed class WorldSection
        {
            public WorldClock clock = new WorldClock();
            public WeatherState weather = new WeatherState();
            public WorldFlags flags = new WorldFlags();
            public int gold;
            public string region = "";
            public int academyPoints;
            public int codexPoints;
            public bool penaltyPending;
            public Quests.QuestLog quests = new Quests.QuestLog();
            public Gates.WorldGates gates = new Gates.WorldGates();
            public Secrets.SecretState secrets = new Secrets.SecretState();
        }

        public PlayerSection player = new PlayerSection();
        public WorldSection world = new WorldSection();

        public string ToJson()
        {
            version = CurrentVersion;
            return JsonUtility.ToJson(this);
        }

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

        /// <summary>Upgrades older saves step by step.</summary>
        void Migrate()
        {
            // v1 → v2: no inventory yet; the player keeps the starter kit.
            if (version < 2) player.hasInventory = false;
            // v2 → v3: quests / academy fields default to empty (JsonUtility already did that).
            version = CurrentVersion;
        }
    }
}
