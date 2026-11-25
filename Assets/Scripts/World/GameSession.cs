using System;
using UnityEngine;

namespace Aethvirse.World
{
    /// <summary>
    /// Runtime world state that lives across scenes (Godot GameState): clock, weather, flags, gold,
    /// play time, current region. Created on demand and kept with DontDestroyOnLoad.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        static GameSession _instance;

        public static GameSession Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("GameSession");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<GameSession>();
                return _instance;
            }
        }

        public static bool Exists => _instance != null;

        public WorldClock Clock { get; private set; } = new WorldClock();
        public WeatherState Weather { get; private set; } = new WeatherState();
        WorldFlags _flags;
        public WorldFlags Flags
        {
            get => _flags ??= Hook(new WorldFlags());
            private set => _flags = Hook(value);
        }
        /// <summary>Forwarded from whichever flag set is current (it is replaced on load / new game).</summary>
        public event Action<string> FlagSet;

        WorldFlags Hook(WorldFlags f)
        {
            f.FlagSet += x => FlagSet?.Invoke(x);
            return f;
        }

        /// <summary>The class chosen at New Game (ClassData.id).</summary>
        public string ClassId { get; set; } = "vanguard";
        public int AcademyPoints { get; set; }
        /// <summary>Dynamic Gates in every region (saved).</summary>
        public Aethvirse.Gates.WorldGates Gates { get; set; } = new Aethvirse.Gates.WorldGates();
        /// <summary>Fog of war per region (saved).</summary>
        public MapMemory Map { get; set; } = new MapMemory();
        public int CodexPoints { get; set; }
        /// <summary>A missed daily quest waits to be paid for in the Penalty Zone.</summary>
        public bool PenaltyPending { get; set; }
        /// <summary>Where SceneTravel.Return goes (Penalty Zone, Gates, interiors).</summary>
        public string ReturnScene { get; set; } = "";
        public Vector3 ReturnPosition { get; set; }
        public WeatherTable WeatherTable { get; set; } = new WeatherTable();
        public int Gold { get; private set; }
        public float Playtime { get; private set; }
        public string Region { get; set; } = "";
        /// <summary>The clock runs only while a player is in the world (not on the title screen, not paused).</summary>
        public bool WorldRunning { get; set; }

        public event Action<int> HourChanged;
        public event Action<int> DayChanged;
        public event Action<WeatherKind> WeatherChanged;
        public event Action<int> GoldChanged;

        float _autosaveTimer;

        void Update()
        {
            if (!WorldRunning) return;
            float dt = Time.deltaTime;
            Playtime += dt;
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= Save.SaveManager.AutosaveInterval)
            {
                _autosaveTimer = 0f;
                Save.SaveManager.Save(Save.SaveManager.AutosaveSlot);
            }
            int hour = Clock.Hour, day = Clock.Day;
            Clock.Advance(dt);
            if (Clock.Hour != hour) HourChanged?.Invoke(Clock.Hour);
            if (Clock.Day != day) DayChanged?.Invoke(Clock.Day);
            if (Weather.Update(Clock.totalMinutes, WeatherTable)) WeatherChanged?.Invoke(Weather.current);
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            Gold = Mathf.Max(0, Gold + amount);
            GoldChanged?.Invoke(Gold);
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0 || amount > Gold) return false;
            AddGold(-amount);
            return true;
        }

        public void SetWeather(WeatherKind kind, bool lockIt)
        {
            Weather.Force(kind, Clock.totalMinutes, WeatherTable.maxDuration, lockIt);
            WeatherChanged?.Invoke(kind);
        }

        public void SetTime(int hh, int mm)
        {
            Clock.SetTimeOfDay(hh, mm);
            HourChanged?.Invoke(Clock.Hour);
        }

        /// <summary>New game: fresh clock, weather, flags, gold.</summary>
        public void ResetForNewGame()
        {
            Clock = new WorldClock();
            Weather = new WeatherState();
            Flags = new WorldFlags();
            Map = new MapMemory();
            Gold = 0;
            Playtime = 0f;
            Region = "";
            AcademyPoints = 0;
            CodexPoints = 0;
            Gates = new Aethvirse.Gates.WorldGates();
            PenaltyPending = false;
            ReturnScene = "";
        }

        public void Restore(WorldClock clock, WeatherState weather, WorldFlags flags, int gold, float playtime, string region)
        {
            Clock = clock ?? new WorldClock();
            Weather = weather ?? new WeatherState();
            Flags = flags ?? new WorldFlags();
            Flags.Rebuild();
            Gold = gold;
            Playtime = playtime;
            Region = region ?? "";
        }
    }
}
