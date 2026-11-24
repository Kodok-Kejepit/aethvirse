using System;
using System.IO;
using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Save
{
    /// <summary>
    /// Save / load (§11): 3 manual slots + autosave (slot 0), versioned compressed JSON under
    /// persistentDataPath/saves. Loading switches to the saved scene and hands the data to the player
    /// when it spawns (<see cref="TakePending"/>).
    /// </summary>
    public static class SaveManager
    {
        public const int AutosaveSlot = 0;
        public const int ManualSlots = 3;
        public const float AutosaveInterval = 300f;

        static SaveData _pending;

        public static string Folder => Path.Combine(Application.persistentDataPath, "saves");
        public static string PathFor(int slot) => Path.Combine(Folder, slot == AutosaveSlot ? "autosave.asav" : $"slot{slot}.asav");

        public static event Action<int> Saved;

        /// <summary>Short info for menus (null = empty slot).</summary>
        public sealed class SlotInfo
        {
            public int slot;
            public string className;
            public int level;
            public float playtime;
            public DateTime savedAt;
            public string region;
        }

        public static SlotInfo Info(int slot)
        {
            SaveData d = SaveData.FromJson(SaveFile.Read(PathFor(slot)));
            if (d == null) return null;
            DateTime.TryParse(d.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime at);
            return new SlotInfo
            {
                slot = slot,
                className = d.player.classId,
                level = d.player.progression != null ? d.player.progression.level : 1,
                playtime = d.playtime,
                savedAt = at.ToLocalTime(),
                region = d.world.region,
            };
        }

        /// <summary>The most recently written slot, or -1.</summary>
        public static int LatestSlot()
        {
            int best = -1;
            DateTime bestTime = DateTime.MinValue;
            for (int s = 0; s <= ManualSlots; s++)
            {
                SlotInfo i = Info(s);
                if (i != null && i.savedAt > bestTime)
                {
                    best = s;
                    bestTime = i.savedAt;
                }
            }
            return best;
        }

        /// <summary>Writes the current game into a slot. False if there is no player to save.</summary>
        public static bool Save(int slot)
        {
            PlayerStats player = PlayerStats.Current;
            if (player == null) return false;
            var data = new SaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                scene = SceneManager.GetActiveScene().name,
            };
            GameSession s = GameSession.Instance;
            data.playtime = s.Playtime;
            data.world.clock = s.Clock;
            data.world.weather = s.Weather;
            data.world.flags = s.Flags;
            data.world.gold = s.Gold;
            data.world.region = s.Region;
            data.world.academyPoints = s.AcademyPoints;
            data.world.codexPoints = s.CodexPoints;
            data.world.penaltyPending = s.PenaltyPending;
            data.world.quests = Quests.QuestSystem.Instance.Log;
            data.world.gates = s.Gates;
            player.Capture(data.player);
            try
            {
                SaveFile.Write(PathFor(slot), data.ToJson());
            }
            catch (Exception e)
            {
                Debug.LogError($"Save to slot {slot} failed: {e.Message}");
                return false;
            }
            Saved?.Invoke(slot);
            return true;
        }

        /// <summary>Loads a slot: restores the world, then opens the saved scene; the player picks up the rest.</summary>
        public static bool Load(int slot)
        {
            SaveData data = SaveData.FromJson(SaveFile.Read(PathFor(slot)));
            if (data == null) return false;
            GameSession.Instance.Restore(data.world.clock, data.world.weather, data.world.flags, data.world.gold, data.playtime, data.world.region);
            GameSession.Instance.AcademyPoints = data.world.academyPoints;
            if (!string.IsNullOrEmpty(data.player.classId)) GameSession.Instance.ClassId = data.player.classId;
            GameSession.Instance.CodexPoints = data.world.codexPoints;
            GameSession.Instance.PenaltyPending = data.world.penaltyPending;
            GameSession.Instance.Gates = data.world.gates ?? new Gates.WorldGates();
            Quests.QuestSystem.Instance.Restore(data.world.quests);
            _pending = data;
            Combat.TimeFx.ResetAll();
            SceneManager.LoadScene(string.IsNullOrEmpty(data.scene) ? Core.BootLoader.FirstScene : data.scene);
            return true;
        }

        /// <summary>Scene travel hands the carried player state over this way.</summary>
        public static void SetPending(SaveData data) => _pending = data;

        /// <summary>The loaded data waiting for the player (once).</summary>
        public static SaveData TakePending()
        {
            SaveData d = _pending;
            _pending = null;
            return d;
        }

        public static void Delete(int slot)
        {
            string p = PathFor(slot);
            if (File.Exists(p)) File.Delete(p);
        }
    }
}
